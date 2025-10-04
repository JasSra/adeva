using DebtManager.Contracts.Persistence;

namespace DebtManager.Web.Services;

/// <summary>
/// Service to manage admin user signup restrictions
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Check if any admin exists in the system
    /// </summary>
    Task<bool> AnyAdminExistsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if admin signup should be allowed (only when no admin exists)
    /// </summary>
    Task<bool> IsInitialAdminSignupAllowedAsync(CancellationToken ct = default);
}

public class AdminService : IAdminService
{
    private readonly IAdminUserRepository _adminUserRepository;

    public AdminService(IAdminUserRepository adminUserRepository)
    {
        _adminUserRepository = adminUserRepository;
    }

    public async Task<bool> AnyAdminExistsAsync(CancellationToken ct = default)
    {
        return await _adminUserRepository.AnyAdminExistsAsync(ct);
    }

    public async Task<bool> IsInitialAdminSignupAllowedAsync(CancellationToken ct = default)
    {
        // Only allow signup if no admin exists
        return !await _adminUserRepository.AnyAdminExistsAsync(ct);
    }
}
