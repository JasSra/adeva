using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Articles;
using Markdig;
using System.Text.RegularExpressions;

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
    public async Task<IActionResult> Create(string title, string slug, string content, string? excerpt, IFormFile? headerImage, string? headerImageUrl, string? authorName, string? metaDescription, string? metaKeywords)
    {
        string? uploadedImageUrl = null;
        
        if (headerImage != null && headerImage.Length > 0)
        {
            uploadedImageUrl = await SaveUploadedFileAsync(headerImage);
        }
        
        var finalImageUrl = uploadedImageUrl ?? headerImageUrl;
        
        var article = Article.Create(title, slug, content, excerpt, finalImageUrl, authorName);
        article.Update(title, content, excerpt, finalImageUrl, authorName, metaDescription, metaKeywords);
        
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
    public async Task<IActionResult> Edit(Guid id, string title, string slug, string content, string? excerpt, IFormFile? headerImage, string? headerImageUrl, string? authorName, string? metaDescription, string? metaKeywords)
    {
        var article = await _articleRepository.GetAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        string? uploadedImageUrl = null;
        
        if (headerImage != null && headerImage.Length > 0)
        {
            uploadedImageUrl = await SaveUploadedFileAsync(headerImage);
        }
        
        var finalImageUrl = uploadedImageUrl ?? headerImageUrl ?? article.HeaderImageUrl;
        
        article.Update(title, content, excerpt, finalImageUrl, authorName, metaDescription, metaKeywords);
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

    // --- AI & Markdown Utilities ---
    [HttpPost]
    public IActionResult RenderMarkdown([FromBody] RenderMarkdownRequest request)
    {
        var pipeline = BuildMarkdownPipeline();
        var html = Markdown.ToHtml(request.Text ?? string.Empty, pipeline);
        return Content(html, "text/html");
    }

    [HttpPost]
    public IActionResult AnalyzeMarkdown([FromBody] AnalyzeMarkdownRequest request)
    {
        var text = request.Text ?? string.Empty;
        var wordCount = Regex.Matches(text, @"\b[\w']+\b").Count;
        var minutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        var headings = Regex.Matches(text, "(?m)^(#{1,6})\\s+(.+)$")
            .Select(m => new { Level = m.Groups[1].Value.Length, Text = m.Groups[2].Value.Trim() })
            .ToList();
        var links = Regex.Matches(text, "\\[[^]]+\\]\\([^)]*\\)").Count;
        var images = Regex.Matches(text, "!\\[[^]]*\\]\\([^)]*\\)").Count;
        var codeBlocks = Regex.Matches(text, @"(?ms)```[\s\S]*?```").Count;

        var suggestions = new List<string>();
        if (!headings.Any(h => h.Level == 1)) suggestions.Add("Add an H1 heading as the article title (e.g., '# Your Title').");
        if (wordCount < 300) suggestions.Add("Content is short; aim for at least 600+ words for SEO.");
        if (links == 0) suggestions.Add("Consider adding internal or external links for references.");
        if (!text.Contains("[") || !text.Contains("](")) suggestions.Add("Use Markdown links like [text](url) where relevant.");
        if (images > 0 && !Regex.IsMatch(text, "!\\[[^]]+\\]")) suggestions.Add("Add alt text to images for accessibility.");
        if (!text.Contains("\n\n")) suggestions.Add("Break content into paragraphs for readability.");

        var fm = ParseFrontMatter(text);

        return Json(new
        {
            wordCount,
            readTimeMinutes = minutes,
            headings,
            links,
            images,
            codeBlocks,
            frontMatter = fm,
            suggestions
        });
    }

    [HttpPost]
    public IActionResult SuggestSeo([FromBody] SuggestSeoRequest request)
    {
        var content = (request.Text ?? string.Empty).Trim();
        var title = (request.Title ?? string.Empty).Trim();
        var firstPara = Regex.Split(content, @"\r?\n\r?\n").FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

        var metaDescription = firstPara.Length > 155 ? firstPara.Substring(0, 152) + "..." : firstPara;
        if (string.IsNullOrWhiteSpace(metaDescription))
        {
            metaDescription = ($"{title} – Insights, how it works, and key takeaways.").Trim();
        }

        var keywords = string.Join(", ", ExtractKeywords(title + " " + content).Take(8));
        var slug = Slugify(!string.IsNullOrWhiteSpace(request.Slug) ? request.Slug! : (string.IsNullOrWhiteSpace(title) ? (firstPara.Split(' ').Take(5).Aggregate("", (a, b) => a + " " + b).Trim()) : title));

        return Json(new { metaDescription, metaKeywords = keywords, slug });
    }

    [HttpPost]
    public IActionResult ImproveMarkdown([FromBody] ImproveMarkdownRequest request)
    {
        var text = request.Text ?? string.Empty;
        if (!Regex.IsMatch(text, "(?m)^# "))
        {
            // Ensure H1 at top using title if provided
            var t = string.IsNullOrWhiteSpace(request.Title) ? "Article Title" : request.Title!.Trim();
            text = $"# {t}\n\n" + text.Trim();
        }

        // Ensure sections
        if (!Regex.IsMatch(text, "(?m)^## "))
        {
            text += "\n\n## Overview\n\nProvide a concise overview here.";
            text += "\n\n## Key Points\n\n- Point one\n- Point two\n- Point three";
            text += "\n\n## Next Steps\n\nCall to action or where to go next.";
        }

        // Convert obvious HTML paragraphs to Markdown
        text = Regex.Replace(text, "(?is)<p>\\s*(.*?)\\s*</p>", m => $"\n\n{m.Groups[1].Value}\n\n");

        return Json(new { improved = text.Trim() });
    }

    [HttpPost]
    public IActionResult GenerateOutline([FromBody] GenerateOutlineRequest request)
    {
        var text = request.Text ?? string.Empty;
        var headings = Regex.Matches(text, "(?m)^(#{1,3})\\s+(.+)$").Select(m => new { L = m.Groups[1].Value.Length, T = m.Groups[2].Value.Trim() }).ToList();
        List<string> lines = new();
        if (headings.Count > 0)
        {
            foreach (var h in headings)
            {
                var prefix = new string(' ', (h.L - 1) * 2) + "- ";
                lines.Add(prefix + h.T);
            }
        }
        else
        {
            lines.AddRange(new[]{
                "- Introduction",
                "- Problem/Context",
                "- Key Points",
                "  - Point 1",
                "  - Point 2",
                "- Examples",
                "- Conclusion / Next Steps"
            });
        }
        return Json(new { outline = string.Join("\n", lines) });
    }

    [HttpPost]
    public IActionResult HeadlineIdeas([FromBody] HeadlineIdeasRequest request)
    {
        var src = ((request.Title ?? string.Empty) + " " + (request.Text ?? string.Empty)).Trim();
        var keys = ExtractKeywords(src).Take(5).ToArray();
        var core = string.Join(" ", keys.Take(3)).Trim();
        if (string.IsNullOrWhiteSpace(core)) core = "Your Article";
        var ideas = new[]
        {
            $"{core}: A Practical Guide",
            $"{core} – What You Need to Know",
            $"Mastering {core}",
            $"{core} in 5 Minutes",
            $"{core}: Tips, Tricks, and Pitfalls"
        };
        return Json(new { ideas });
    }

    [HttpPost]
    public IActionResult Summarize([FromBody] SummarizeRequest request)
    {
        var text = (request.Text ?? string.Empty).Trim();
        var paras = Regex.Split(text, @"\r?\n\r?\n").Where(p => p.Trim().Length > 0).Take(3).ToArray();
        var summary = string.Join(" ", paras).Trim();
        summary = summary.Length > 320 ? summary.Substring(0, 317) + "..." : summary;
        if (string.IsNullOrWhiteSpace(summary)) summary = "Add an intro paragraph to enable summarization.";
        return Json(new { summary });
    }

    [HttpPost]
    public IActionResult SuggestTags([FromBody] SuggestTagsRequest request)
    {
        var tags = ExtractKeywords((request.Text ?? string.Empty) + " " + (request.Title ?? string.Empty)).Take(10);
        return Json(new { tags = tags.ToArray() });
    }

    private MarkdownPipeline BuildMarkdownPipeline() =>
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    private static Dictionary<string, string> ParseFrontMatter(string text)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var match = Regex.Match(text, "(?s)^---\\s*(.*?)\\s*---");
        if (!match.Success) return dict;
        var body = match.Groups[1].Value;
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim().Trim('"');
            dict[key] = value;
        }
        return dict;
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        var words = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z]{4,}\b").Select(m => m.Value);
        var stop = new HashSet<string>(new[] { "this","that","with","from","your","have","will","into","about","what","when","where","which","their","there","been","more","than","over","also","such","only","most","some","like","just","make","made","each","they","them","then","here","into","onto","upon","http","https" });
        return words.Where(w => !stop.Contains(w)).GroupBy(w => w).OrderByDescending(g => g.Count()).Select(g => g.Key);
    }

    private static string Slugify(string input)
    {
        var slug = input.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        slug = Regex.Replace(slug, @"-+", "-");
        return slug;
    }

    private async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "articles");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/uploads/articles/{uniqueFileName}";
    }
}

public class RenderMarkdownRequest { public string? Text { get; set; } }
public class AnalyzeMarkdownRequest { public string? Text { get; set; } }
public class SuggestSeoRequest { public string? Text { get; set; } public string? Title { get; set; } public string? Slug { get; set; } }
public class ImproveMarkdownRequest { public string? Text { get; set; } public string? Title { get; set; } }
public class GenerateOutlineRequest { public string? Text { get; set; } }
public class HeadlineIdeasRequest { public string? Text { get; set; } public string? Title { get; set; } }
public class SummarizeRequest { public string? Text { get; set; } }
public class SuggestTagsRequest { public string? Text { get; set; } public string? Title { get; set; } }
