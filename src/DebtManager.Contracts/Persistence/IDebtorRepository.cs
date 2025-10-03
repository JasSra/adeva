using DebtManager.Domain.Debtors;

namespace DebtManager.Contracts.Persistence;

public interface IDebtorRepository : IRepository<Debtor>
{
    Task<Debtor?> GetByReferenceAsync(string referenceId, CancellationToken ct = default);
}
