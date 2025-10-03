using DebtManager.Domain.Common;

namespace DebtManager.Contracts.Persistence;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
