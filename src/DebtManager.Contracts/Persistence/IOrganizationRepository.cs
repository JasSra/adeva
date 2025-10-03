using DebtManager.Domain.Organizations;

namespace DebtManager.Contracts.Persistence;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<Organization?> GetByAbnAsync(string abn, CancellationToken ct = default);
}
