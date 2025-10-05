using DebtManager.Domain.Documents;

namespace DebtManager.Contracts.Persistence;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByDebtorAsync(Guid debtorId, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
