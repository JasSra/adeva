using DebtManager.Contracts.Configuration;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DebtManager.Infrastructure.Configuration;

public class AppConfigService : IAppConfigService
{
    private readonly AppDbContext _db;
    private readonly IDistributedCache? _distributedCache;
    private readonly MemoryCache<string, (string? value, bool isSecret)> _cache = new();

    public AppConfigService(AppDbContext db, IDistributedCache? distributedCache = null)
    {
        _db = db;
        _distributedCache = distributedCache;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        // Prefer distributed cache if available
        if (_distributedCache != null)
        {
            var cached = await _distributedCache.GetStringAsync(CacheKey(key), ct);
            if (cached != null)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CacheDto>(cached);
                    return parsed?.Value;
                }
                catch { /* ignore cache decode errors */ }
            }
        }

        if (_cache.TryGet(key, out var v)) return v.value;

        var entry = await _db.AppConfigEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Key == key, ct);
        var tuple = (entry?.Value, entry?.IsSecret ?? false);

        // Set caches
        _cache.Set(key, tuple, TimeSpan.FromMinutes(5));
        if (_distributedCache != null)
        {
            var dto = new CacheDto { Value = tuple.Item1, IsSecret = tuple.Item2 };
            var json = JsonSerializer.Serialize(dto);
            await _distributedCache.SetStringAsync(CacheKey(key), json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            }, ct);
        }
        return tuple.Item1;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var raw = await GetAsync(key, ct);
        if (raw is null) return default;
        try
        {
            return (T?)Convert.ChangeType(raw, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync(string key, string? value, bool isSecret = false, CancellationToken ct = default)
    {
        var entry = await _db.AppConfigEntries.FirstOrDefaultAsync(e => e.Key == key, ct);
        if (entry == null)
        {
            _db.AppConfigEntries.Add(new Domain.Configuration.AppConfigEntry(key, value, isSecret));
        }
        else
        {
            entry.Update(value, isSecret);
        }
        await _db.SaveChangesAsync(ct);

        // Invalidate caches
        _cache.Remove(key);
        if (_distributedCache != null)
        {
            await _distributedCache.RemoveAsync(CacheKey(key), ct);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return await _db.AppConfigEntries.AnyAsync(e => e.Key == key, ct);
    }

    public async Task<IDictionary<string, (string? value, bool isSecret)>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db.AppConfigEntries.AsNoTracking().ToListAsync(ct);
        return list.ToDictionary(e => e.Key, e => (e.Value, e.IsSecret));
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var entry = await _db.AppConfigEntries.FirstOrDefaultAsync(e => e.Key == key, ct);
        if (entry != null)
        {
            _db.AppConfigEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
            _cache.Remove(key);
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(CacheKey(key), ct);
            }
        }
    }

    private static string CacheKey(string key) => $"appcfg:{key}";

    private sealed class CacheDto
    {
        public string? Value { get; set; }
        public bool IsSecret { get; set; }
    }

    // Simple in-memory cache helper
    private sealed class MemoryCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, (DateTimeOffset expires, TValue value)> _store = new();
        public bool TryGet(TKey key, out TValue value)
        {
            if (_store.TryGetValue(key, out var tuple))
            {
                if (tuple.expires > DateTimeOffset.UtcNow)
                {
                    value = tuple.value;
                    return true;
                }
                _store.Remove(key);
            }
            value = default!;
            return false;
        }
        public void Set(TKey key, TValue value, TimeSpan ttl)
        {
            _store[key] = (DateTimeOffset.UtcNow.Add(ttl), value);
        }
        public void Remove(TKey key) => _store.Remove(key);
    }
}
