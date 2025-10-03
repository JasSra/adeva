using DebtManager.Domain.Debts;

namespace DebtManager.Contracts.Persistence;

public interface IDebtRepository : IRepository<Debt>
{
    Task<Debt?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Debt>> GetOpenByDebtorAsync(Guid debtorId, CancellationToken ct = default);
    Task<IReadOnlyList<Debt>> GetPendingReconciliationAsync(DateTime asOfUtc, CancellationToken ct = default);
}
