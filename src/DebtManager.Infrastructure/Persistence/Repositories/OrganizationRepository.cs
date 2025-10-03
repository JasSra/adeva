using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class OrganizationRepository(AppDbContext db) : IOrganizationRepository
{
    public async Task AddAsync(Organization entity, CancellationToken ct = default)
    {
        await db.Organizations.AddAsync(entity, ct);
    }

    public Task<Organization?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Organizations.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Organization?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        return db.Organizations.FirstOrDefaultAsync(x => x.Subdomain == subdomain, ct);
    }

    public Task<Organization?> GetByAbnAsync(string abn, CancellationToken ct = default)
    {
        return db.Organizations.FirstOrDefaultAsync(x => x.Abn == abn, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
