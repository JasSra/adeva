using Microsoft.AspNetCore.Mvc;
using DebtManager.Contracts.Persistence;

namespace DebtManager.Web.Controllers;

public class ArticleController : Controller
{
    private readonly IArticleRepository _articleRepository;

    public ArticleController(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    [Route("Article/View/{slug}")]
    public async Task<IActionResult> ViewArticle(string slug)
    {
        var article = await _articleRepository.GetBySlugAsync(slug);
        
        if (article == null || !article.IsPublished)
        {
            return NotFound();
        }

        article.IncrementViewCount();
        await _articleRepository.SaveChangesAsync();

        ViewBag.Article = article;
        return View("View");
    }
}
