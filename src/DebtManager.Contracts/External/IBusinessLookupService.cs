namespace DebtManager.Contracts.External;

public interface IBusinessLookupService
{
    Task<bool> ValidateAcnAsync(string acn, CancellationToken ct = default);
    Task<BusinessInfo?> LookupByAbnAsync(string abn, CancellationToken ct = default);
}

public class BusinessInfo
{
    public string Abn { get; set; } = string.Empty;
    public string? Acn { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? BusinessType { get; set; }
    public string? Status { get; set; }
}
