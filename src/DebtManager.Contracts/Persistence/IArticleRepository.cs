using DebtManager.Domain.Articles;

namespace DebtManager.Contracts.Persistence;

public interface IArticleRepository
{
    Task AddAsync(Article entity, CancellationToken ct = default);
    Task<Article?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Article>> GetAllAsync(bool publishedOnly = false, CancellationToken ct = default);
    Task<List<Article>> GetPagedAsync(int page, int pageSize, bool publishedOnly = false, CancellationToken ct = default);
    Task<int> CountAsync(bool publishedOnly = false, CancellationToken ct = default);
    Task DeleteAsync(Article entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
