using DebtManager.Domain.Documents;

namespace DebtManager.Contracts.Persistence;

public interface IInvoiceDataRepository
{
    Task<InvoiceData?> GetAsync(Guid id, CancellationToken ct = default);
    Task<InvoiceData?> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceData>> GetPendingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceData>> GetFailedRetryableAsync(int maxRetries, CancellationToken ct = default);
    Task AddAsync(InvoiceData invoiceData, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
