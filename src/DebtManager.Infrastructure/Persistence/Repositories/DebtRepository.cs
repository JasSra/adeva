using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class DebtRepository(AppDbContext db) : IDebtRepository
{
    public async Task AddAsync(Debt entity, CancellationToken ct = default)
    {
        await db.Debts.AddAsync(entity, ct);
    }

    public Task<Debt?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Debts.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Debt?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return db.Debts
            .Include(d => d.Debtor)
            .Include(d => d.Organization)
            .Include(d => d.PaymentPlans).ThenInclude(p => p.Installments)
            .Include(d => d.PaymentPlans).ThenInclude(p => p.Transactions)
            .Include(d => d.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<Debt>> GetOpenByDebtorAsync(Guid debtorId, CancellationToken ct = default)
    {
        return await db.Debts
            .Where(x => x.DebtorId == debtorId && x.Status != DebtStatus.Settled && x.Status != DebtStatus.WrittenOff && x.Status != DebtStatus.Closed)
            .Include(x => x.PaymentPlans)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Debt>> GetPendingReconciliationAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        return await db.Debts
            .Where(x => x.Status == DebtStatus.Active && x.NextActionAtUtc != null && x.NextActionAtUtc <= asOfUtc)
            .Include(x => x.Transactions.Where(t => t.Status != TransactionStatus.Succeeded && t.Status != TransactionStatus.Refunded))
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
