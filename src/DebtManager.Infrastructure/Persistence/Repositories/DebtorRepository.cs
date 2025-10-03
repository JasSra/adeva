using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Debtors;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class DebtorRepository(AppDbContext db) : IDebtorRepository
{
    public async Task AddAsync(Debtor entity, CancellationToken ct = default)
    {
        await db.Debtors.AddAsync(entity, ct);
    }

    public Task<Debtor?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Debtors.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Debtor?> GetByReferenceAsync(string referenceId, CancellationToken ct = default)
    {
        return db.Debtors.FirstOrDefaultAsync(x => x.ReferenceId == referenceId, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
