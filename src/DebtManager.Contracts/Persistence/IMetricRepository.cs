using DebtManager.Domain.Analytics;

namespace DebtManager.Contracts.Persistence;

public interface IMetricRepository
{
    Task<IReadOnlyList<Metric>> GetByKeyAsync(string key, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
    Task<IReadOnlyList<Metric>> GetByOrganizationAsync(Guid organizationId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
    Task<Dictionary<string, decimal>> GetAggregatedMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? organizationId = null, CancellationToken ct = default);
    Task AddAsync(Metric metric, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
