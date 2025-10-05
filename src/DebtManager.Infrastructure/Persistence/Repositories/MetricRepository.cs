using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Analytics;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class MetricRepository(AppDbContext db) : IMetricRepository
{
    public async Task<IReadOnlyList<Metric>> GetByKeyAsync(string key, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        var query = db.Metrics.Where(x => x.Key == key);

        if (fromUtc.HasValue)
            query = query.Where(x => x.RecordedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.RecordedAtUtc <= toUtc.Value);

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Metric>> GetByOrganizationAsync(Guid organizationId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        var query = db.Metrics.Where(x => x.OrganizationId == organizationId);

        if (fromUtc.HasValue)
            query = query.Where(x => x.RecordedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.RecordedAtUtc <= toUtc.Value);

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<string, decimal>> GetAggregatedMetricsAsync(DateTime fromUtc, DateTime toUtc, Guid? organizationId = null, CancellationToken ct = default)
    {
        var query = db.Metrics
            .Where(x => x.RecordedAtUtc >= fromUtc && x.RecordedAtUtc <= toUtc);

        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);

        var grouped = await query
            .GroupBy(x => x.Key)
            .Select(g => new { Key = g.Key, Value = g.Sum(m => m.Value) })
            .ToListAsync(ct);

        return grouped.ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task AddAsync(Metric metric, CancellationToken ct = default)
    {
        await db.Metrics.AddAsync(metric, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
