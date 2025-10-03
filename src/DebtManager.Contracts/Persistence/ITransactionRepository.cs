using DebtManager.Domain.Payments;

namespace DebtManager.Contracts.Persistence;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<Transaction?> GetByProviderReferenceAsync(string providerRef, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByDebtAsync(Guid debtId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetUnsettledAsync(DateTime asOfUtc, CancellationToken ct = default);
}
