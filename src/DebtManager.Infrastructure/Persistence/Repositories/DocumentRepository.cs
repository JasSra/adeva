using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Documents.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<Document>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await db.Documents
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByDebtorAsync(Guid debtorId, CancellationToken ct = default)
    {
        return await db.Documents
            .Where(x => x.DebtorId == debtorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        await db.Documents.AddAsync(document, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
