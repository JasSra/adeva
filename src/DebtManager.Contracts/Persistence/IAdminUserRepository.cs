using DebtManager.Domain.AdminUsers;

namespace DebtManager.Contracts.Persistence;

public interface IAdminUserRepository : IRepository<AdminUser>
{
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AdminUser?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken ct = default);
    Task<bool> AnyAdminExistsAsync(CancellationToken ct = default);
    Task<List<AdminUser>> GetActiveAdminsAsync(CancellationToken ct = default);
}
