using DebtManager.Domain.Common;

namespace DebtManager.Domain.Articles;

public class Article : Entity
{
    public string Title { get; private set; }
    public string Slug { get; private set; }
    public string Content { get; private set; }
    public string? Excerpt { get; private set; }
    public string? HeaderImageUrl { get; private set; }
    public string? AuthorName { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int ViewCount { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }

    private Article()
    {
        Title = Slug = Content = string.Empty;
    }

    public Article(
        string title,
        string slug,
        string content,
        string? excerpt = null,
        string? headerImageUrl = null,
        string? authorName = null)
        : this()
    {
        Title = title;
        Slug = slug;
        Content = content;
        Excerpt = excerpt;
        HeaderImageUrl = headerImageUrl;
        AuthorName = authorName;
        IsPublished = false;
    }

    public static Article Create(
        string title,
        string slug,
        string content,
        string? excerpt = null,
        string? headerImageUrl = null,
        string? authorName = null)
    {
        return new Article(title, slug, content, excerpt, headerImageUrl, authorName);
    }

    public void Update(
        string title,
        string content,
        string? excerpt = null,
        string? headerImageUrl = null,
        string? authorName = null,
        string? metaDescription = null,
        string? metaKeywords = null)
    {
        Title = title;
        Content = content;
        Excerpt = excerpt;
        HeaderImageUrl = headerImageUrl;
        AuthorName = authorName;
        MetaDescription = metaDescription;
        MetaKeywords = metaKeywords;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        if (!IsPublished)
        {
            return;
        }

        IsPublished = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void UpdateSlug(string slug)
    {
        Slug = slug;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
