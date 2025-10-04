using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Articles;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence.Repositories;

public class ArticleRepository(AppDbContext db) : IArticleRepository
{
    public async Task AddAsync(Article entity, CancellationToken ct = default)
    {
        await db.Articles.AddAsync(entity, ct);
    }

    public Task<Article?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Articles.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return db.Articles.FirstOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public Task<List<Article>> GetAllAsync(bool publishedOnly = false, CancellationToken ct = default)
    {
        var query = db.Articles.AsQueryable();
        
        if (publishedOnly)
        {
            query = query.Where(x => x.IsPublished);
        }
        
        return query.OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc).ToListAsync(ct);
    }

    public Task<List<Article>> GetPagedAsync(int page, int pageSize, bool publishedOnly = false, CancellationToken ct = default)
    {
        var query = db.Articles.AsQueryable();
        
        if (publishedOnly)
        {
            query = query.Where(x => x.IsPublished);
        }
        
        return query
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(bool publishedOnly = false, CancellationToken ct = default)
    {
        var query = db.Articles.AsQueryable();
        
        if (publishedOnly)
        {
            query = query.Where(x => x.IsPublished);
        }
        
        return query.CountAsync(ct);
    }

    public Task DeleteAsync(Article entity, CancellationToken ct = default)
    {
        db.Articles.Remove(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
