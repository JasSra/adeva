using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Articles;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;

    public ArticlesController(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Articles";
        
        var articles = await _articleRepository.GetPagedAsync(page, pageSize, publishedOnly: false);
        var totalCount = await _articleRepository.CountAsync(publishedOnly: false);
        
        ViewBag.Articles = articles;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        return View();
    }

    public IActionResult Create()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Article";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string title, string slug, string content, string? excerpt, string? headerImageUrl, string? authorName, string? metaDescription, string? metaKeywords)
    {
        var article = Article.Create(title, slug, content, excerpt, headerImageUrl, authorName);
        article.Update(title, content, excerpt, headerImageUrl, authorName, metaDescription, metaKeywords);
        
        await _articleRepository.AddAsync(article);
        await _articleRepository.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Article";
        
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        ViewBag.Article = article;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, string title, string slug, string content, string? excerpt, string? headerImageUrl, string? authorName, string? metaDescription, string? metaKeywords)
    {
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        article.Update(title, content, excerpt, headerImageUrl, authorName, metaDescription, metaKeywords);
        if (article.Slug != slug)
        {
            article.UpdateSlug(slug);
        }
        
        await _articleRepository.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Publish(Guid id)
    {
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        article.Publish();
        await _articleRepository.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        article.Unpublish();
        await _articleRepository.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        await _articleRepository.DeleteAsync(article);
        await _articleRepository.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
}
