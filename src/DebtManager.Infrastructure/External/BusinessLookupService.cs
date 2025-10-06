using System.Text.RegularExpressions;
using DebtManager.Contracts.External;

namespace DebtManager.Infrastructure.External;

public class BusinessLookupService : IBusinessLookupService
{
    private readonly IAbrValidator _abr;

    public BusinessLookupService(IAbrValidator abr)
    {
        _abr = abr;
    }

    public async Task<bool> ValidateAcnAsync(string acn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(acn)) return false;
        var digits = Regex.Replace(acn, "[^0-9]", "");
        if (digits.Length != 9) return false;

        // ACN checksum validation (ASIC):
        // Multiply first 8 digits by weights 8..1, sum, divide by 10. Check digit = (10 - (sum % 10)) % 10
        int sum = 0;
        for (int i = 0; i < 8; i++)
        {
            int d = digits[i] - '0';
            int weight = 8 - i;
            sum += d * weight;
        }
        int check = (10 - (sum % 10)) % 10;
        int provided = digits[8] - '0';
        return check == provided;
    }

    public async Task<BusinessInfo?> LookupByAbnAsync(string abn, CancellationToken ct = default)
    {
        var result = await _abr.ValidateAbnAsync(abn, ct);
        if (!result.IsValid)
        {
            return null;
        }

        return new BusinessInfo
        {
            Abn = result.Abn ?? abn,
            Name = result.BusinessName ?? result.LegalName ?? string.Empty,
            LegalName = result.LegalName,
            TradingName = result.TradingName,
            BusinessType = result.EntityType,
            Status = result.ErrorMessage == null ? "Active" : null,
            Acn = result.Acn
        };
    }
}
