using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class PaymentPlanRepository(AppDbContext db) : IPaymentPlanRepository
{
    public async Task AddAsync(PaymentPlan entity, CancellationToken ct = default)
    {
        await db.PaymentPlans.AddAsync(entity, ct);
    }

    public Task<PaymentPlan?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.PaymentPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<PaymentPlan?> GetWithScheduleAsync(Guid id, CancellationToken ct = default)
    {
        return db.PaymentPlans
            .Include(x => x.Installments)
            .Include(x => x.Transactions)
            .Include(x => x.Debt)!.ThenInclude(d => d!.Debtor)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<PaymentPlan>> GetActiveByDebtAsync(Guid debtId, CancellationToken ct = default)
    {
        return await db.PaymentPlans
            .Where(x => x.DebtId == debtId && x.Status == PaymentPlanStatus.Active)
            .Include(x => x.Installments)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PaymentPlan>> GetPlansNeedingReviewAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        return await db.PaymentPlans
            .Where(x => (x.RequiresManualReview || x.Status == PaymentPlanStatus.PendingApproval) && x.CreatedAtUtc <= asOfUtc)
            .Include(x => x.Debt)
            .ThenInclude(d => d!.Debtor)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
