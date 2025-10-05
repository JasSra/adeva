using DebtManager.Domain.Analytics;

namespace DebtManager.Contracts.Analytics;

public interface IMetricService
{
    /// <summary>
    /// Record a metric value
    /// </summary>
    Task RecordMetricAsync(string key, MetricType type, decimal value, string? tags = null, Guid? organizationId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get aggregated metrics for a time period
    /// </summary>
    Task<Dictionary<string, decimal>> GetAggregatedMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? organizationId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get metrics by key
    /// </summary>
    Task<IReadOnlyList<Metric>> GetMetricsByKeyAsync(string key, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
}
