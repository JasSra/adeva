using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task AddAsync(Transaction entity, CancellationToken ct = default)
    {
        await db.Transactions.AddAsync(entity, ct);
    }

    public Task<Transaction?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Transactions.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Transaction?> GetByProviderReferenceAsync(string providerRef, CancellationToken ct = default)
    {
        return db.Transactions
            .Include(x => x.Debt)
            .Include(x => x.PaymentPlan)
            .Include(x => x.PaymentInstallment)
            .FirstOrDefaultAsync(x => x.ProviderRef == providerRef, ct);
    }

    public async Task<IReadOnlyList<Transaction>> GetByDebtAsync(Guid debtId, CancellationToken ct = default)
    {
        return await db.Transactions
            .Where(x => x.DebtId == debtId)
            .OrderByDescending(x => x.ProcessedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Transaction>> GetUnsettledAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        return await db.Transactions
            .Where(x => x.Status == TransactionStatus.Pending && x.ProcessedAtUtc <= asOfUtc)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
