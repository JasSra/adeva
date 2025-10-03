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
                return new BrandingTheme(org.Name, org.PrimaryColorHex);
            }
        }

        // Fallback
        return new BrandingTheme("Default Org", "#0ea5e9");
    }
}

internal sealed class NullOrganizationRepository : IOrganizationRepository
{
    public Task AddAsync(DebtManager.Domain.Organizations.Organization entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task<DebtManager.Domain.Organizations.Organization?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    public Task<DebtManager.Domain.Organizations.Organization?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default) => Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    public Task<DebtManager.Domain.Organizations.Organization?> GetByAbnAsync(string abn, CancellationToken ct = default) => Task.FromResult<DebtManager.Domain.Organizations.Organization?>(null);
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public record BrandingTheme(string Name, string PrimaryColorHex);
