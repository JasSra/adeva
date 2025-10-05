using DebtManager.Contracts.Analytics;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Analytics;

namespace DebtManager.Infrastructure.Analytics;

public class MetricService(IMetricRepository metricRepository) : IMetricService
{
    public async Task RecordMetricAsync(string key, MetricType type, decimal value, string? tags = null, Guid? organizationId = null, CancellationToken ct = default)
    {
        var metric = Metric.Record(key, type, value, tags, organizationId);
        await metricRepository.AddAsync(metric, ct);
        await metricRepository.SaveChangesAsync(ct);
    }

    public Task<Dictionary<string, decimal>> GetAggregatedMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? organizationId = null, CancellationToken ct = default)
    {
        return metricRepository.GetAggregatedMetricsAsync(fromUtc, toUtc, organizationId, ct);
    }

    public Task<IReadOnlyList<Metric>> GetMetricsByKeyAsync(string key, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        return metricRepository.GetByKeyAsync(key, fromUtc, toUtc, ct);
    }
}
