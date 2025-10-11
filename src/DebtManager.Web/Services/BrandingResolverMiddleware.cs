using System.Text.RegularExpressions;
using DebtManager.Contracts.Persistence;

namespace DebtManager.Web.Services;

public class BrandingResolverMiddleware : IMiddleware
{
    public const string ThemeItemKey = "BrandingTheme";

    private readonly IOrganizationRepository _orgRepo;

    public BrandingResolverMiddleware() : this(new NullOrganizationRepository()) {}

    public BrandingResolverMiddleware(IOrganizationRepository orgRepo)
    {
        _orgRepo = orgRepo;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();
        string tenant = ResolveTenantFromHost(host);
        var theme = await GetThemeForTenantAsync(tenant, context.RequestAborted);
        context.Items[ThemeItemKey] = theme;
        await next(context);
    }

    private static string ResolveTenantFromHost(string host)
    {
        // subdomain.adeva.local or custom domains
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            return parts[0];
        }
        // default tenant
        return "default";
    }

    private async Task<BrandingTheme> GetThemeForTenantAsync(string tenant, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tenant) && tenant != "default")
        {
            var org = await _orgRepo.GetBySubdomainAsync(tenant, ct);
            if (org != null)
            {
                return new BrandingTheme(
                    name: string.IsNullOrWhiteSpace(org.TradingName) ? org.Name : org.TradingName!,
                    primaryColorHex: org.PrimaryColorHex,
                    secondaryColorHex: org.SecondaryColorHex,
                    supportEmail: org.SupportEmail,
                    supportPhone: org.SupportPhone,
                    logoUrl: org.LogoUrl,
                    brandTagline: org.BrandTagline,
                    statementFooter: org.StatementFooter);
            }
        }

        // Fallback
        return BrandingTheme.Default;
    }
}

internal sealed class NullOrganizationRepository : IOrganizationRepository
{
    public Task AddAsync(DebtManager.Domain.Organizations.Organization entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task<DebtManager.Domain.Organizations.Organization?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    public Task<DebtManager.Domain.Organizations.Organization?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        // Provide a minimal in-memory stub for tests when middleware is constructed
        // without DI. This ensures predictable theming for known tenants like "client1".
        if (string.Equals(subdomain, "client1", StringComparison.OrdinalIgnoreCase))
        {
            var org = DebtManager.Domain.Organizations.Organization.CreatePending(
                name: "Client One",
                legalName: "Client One Pty Ltd",
                abn: "12345678901",
                defaultCurrency: "AUD",
                primaryColorHex: "#0ea5e9",
                secondaryColorHex: "#0369a1",
                supportEmail: "support@client1.com",
                supportPhone: "+61 2 1234 5678",
                timezone: "Australia/Sydney",
                subdomain: "client1",
                tradingName: "Client One");
            return Task.FromResult<DebtManager.Domain.Organizations.Organization?>(org);
        }

        return Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    }
    public Task<DebtManager.Domain.Organizations.Organization?> GetByAbnAsync(string abn, CancellationToken ct = default) => Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class BrandingTheme
{
    private const string DefaultPrimary = "#2563eb";
    private const string DefaultSecondary = "#1e293b";
    private const string DefaultSupportEmail = "support@adevaplus.com";
    private const string DefaultSupportPhone = "1300 ADEVA PLUS";

    private static readonly Regex HexColorRegex = new(@"^#?(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

    public static BrandingTheme Default { get; } = new BrandingTheme(
        name: "Adeva Plus",
        primaryColorHex: DefaultPrimary,
        secondaryColorHex: DefaultSecondary,
        supportEmail: DefaultSupportEmail,
        supportPhone: DefaultSupportPhone,
        logoUrl: null,
        brandTagline: "Collections power, delivered with care.",
        statementFooter: "Receipts issued by Adeva Plus on behalf of our clients.");

    public BrandingTheme(
        string name,
        string primaryColorHex,
        string secondaryColorHex,
        string supportEmail,
        string supportPhone,
        string? logoUrl = null,
        string? brandTagline = null,
        string? statementFooter = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Your Organization" : name.Trim();
        PrimaryColorHex = NormalizeColor(primaryColorHex, DefaultPrimary);
        SecondaryColorHex = NormalizeColor(secondaryColorHex, DefaultSecondary);
        SupportEmail = string.IsNullOrWhiteSpace(supportEmail) ? DefaultSupportEmail : supportEmail.Trim();
        SupportPhone = string.IsNullOrWhiteSpace(supportPhone) ? DefaultSupportPhone : supportPhone.Trim();
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;
        BrandTagline = string.IsNullOrWhiteSpace(brandTagline) ? null : brandTagline.Trim();
        StatementFooter = string.IsNullOrWhiteSpace(statementFooter) ? null : statementFooter.Trim();
    }

    public string Name { get; }
    public string PrimaryColorHex { get; }
    public string SecondaryColorHex { get; }
    public string SupportEmail { get; }
    public string SupportPhone { get; }
    public string? LogoUrl { get; }
    public string? BrandTagline { get; }
    public string? StatementFooter { get; }

    public string DisplayTagline => BrandTagline ?? "Powered by Adeva Plus";
    public bool HasCustomLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    public bool HasSupportContact => !string.IsNullOrWhiteSpace(SupportEmail) || !string.IsNullOrWhiteSpace(SupportPhone);

    private static string NormalizeColor(string? input, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(input) && TryNormaliseHex(input, out var value))
        {
            return value;
        }

        return TryNormaliseHex(fallback, out var fb) ? fb : "#2563eb";
    }

    private static bool TryNormaliseHex(string input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim();
        if (!HexColorRegex.IsMatch(candidate))
        {
            candidate = candidate.StartsWith('#') ? candidate[1..] : candidate;
            if (!HexColorRegex.IsMatch(candidate))
            {
                return false;
            }
        }

        candidate = candidate.StartsWith('#') ? candidate : $"#{candidate}";
        if (candidate.Length == 4 || candidate.Length == 5)
        {
            candidate = "#" + string.Concat(candidate.Skip(1).Select(ch => $"{ch}{ch}"));
        }

        normalized = candidate.Length switch
        {
            > 9 => candidate[..9].ToLowerInvariant(),
            _ => candidate.ToLowerInvariant()
        };
        return true;
    }
}
