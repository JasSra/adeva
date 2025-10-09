using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLog?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.AuditLogs.FindAsync(new object[] { id }, ct);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync(id, ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.AuditLogs
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        return await _db.AuditLogs
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(
        string? search = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.UserEmail.Contains(search) ||
                a.Action.Contains(search) ||
                (a.EntityId != null && a.EntityId.Contains(search)) ||
                (a.Details != null && a.Details.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(
        string? search = null,
        string? entityType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.UserEmail.Contains(search) ||
                a.Action.Contains(search) ||
                (a.EntityId != null && a.EntityId.Contains(search)) ||
                (a.Details != null && a.Details.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc <= toDate.Value);
        }

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(AuditLog entity, CancellationToken ct = default)
    {
        await _db.AuditLogs.AddAsync(entity, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AuditLog entity, CancellationToken ct = default)
    {
        _db.AuditLogs.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(AuditLog entity, CancellationToken ct = default)
    {
        _db.AuditLogs.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
