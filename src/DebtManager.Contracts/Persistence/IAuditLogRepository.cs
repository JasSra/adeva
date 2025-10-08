using DebtManager.Domain.Audit;

namespace DebtManager.Contracts.Persistence;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(
        string? search = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);
    Task<int> GetCountAsync(
        string? search = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
}
