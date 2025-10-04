namespace DebtManager.Contracts.Configuration;

public interface IAppConfigService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync(string key, string? value, bool isSecret = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<IDictionary<string, (string? value, bool isSecret)>> GetAllAsync(CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
