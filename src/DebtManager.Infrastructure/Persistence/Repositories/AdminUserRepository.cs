using DebtManager.Contracts.Persistence;
using DebtManager.Domain.AdminUsers;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _context;

    public AdminUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUser?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AdminUsers.FindAsync(new object[] { id }, ct);
    }

    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync(id, ct);
    }

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(a => a.Email == email, ct);
    }

    public async Task<AdminUser?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken ct = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(a => a.ExternalAuthId == externalAuthId, ct);
    }

    public async Task<bool> AnyAdminExistsAsync(CancellationToken ct = default)
    {
        return await _context.AdminUsers
            .AnyAsync(a => a.IsActive && a.Role == AdminRole.Administrator, ct);
    }

    public async Task<List<AdminUser>> GetActiveAdminsAsync(CancellationToken ct = default)
    {
        return await _context.AdminUsers
            .Where(a => a.IsActive && a.Role == AdminRole.Administrator)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AdminUser entity, CancellationToken ct = default)
    {
        await _context.AdminUsers.AddAsync(entity, ct);
    }

    public Task UpdateAsync(AdminUser entity, CancellationToken ct = default)
    {
        _context.AdminUsers.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AdminUser entity, CancellationToken ct = default)
    {
        _context.AdminUsers.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
