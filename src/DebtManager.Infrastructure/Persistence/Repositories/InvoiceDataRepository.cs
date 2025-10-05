using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class InvoiceDataRepository(AppDbContext db) : IInvoiceDataRepository
{
    public Task<InvoiceData?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.InvoiceData
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<InvoiceData?> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
    {
        return db.InvoiceData
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.DocumentId == documentId, ct);
    }

    public async Task<IReadOnlyList<InvoiceData>> GetPendingAsync(CancellationToken ct = default)
    {
        return await db.InvoiceData
            .Include(x => x.Document)
            .Where(x => x.Status == InvoiceProcessingStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InvoiceData>> GetFailedRetryableAsync(int maxRetries, CancellationToken ct = default)
    {
        return await db.InvoiceData
            .Include(x => x.Document)
            .Where(x => x.Status == InvoiceProcessingStatus.Failed && x.RetryCount < maxRetries)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(InvoiceData invoiceData, CancellationToken ct = default)
    {
        await db.InvoiceData.AddAsync(invoiceData, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
