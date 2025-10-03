using DebtManager.Domain.Payments;

namespace DebtManager.Contracts.Persistence;

public interface IPaymentPlanRepository : IRepository<PaymentPlan>
{
    Task<PaymentPlan?> GetWithScheduleAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentPlan>> GetActiveByDebtAsync(Guid debtId, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentPlan>> GetPlansNeedingReviewAsync(DateTime asOfUtc, CancellationToken ct = default);
}
