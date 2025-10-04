using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class CommsController : Controller
{
    public IActionResult Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Communications & Templates";
        return View();
    }

    public IActionResult Templates(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Message Templates";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult CreateTemplate()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Template";
        return View();
    }

    public IActionResult EditTemplate(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Template";
        ViewBag.TemplateId = id;
        return View();
    }

    [HttpPost]
    public IActionResult GenerateSampleTemplate([FromBody] GenerateSampleRequest request)
    {
        // AI-powered sample template generation
        var templates = new Dictionary<string, Dictionary<string, string>>
        {
            ["email"] = new Dictionary<string, string>
            {
                ["payment-reminder"] = "Payment Reminder: {Amount} Due on {DueDate}",
                ["payment-confirmation"] = "Payment Received - Thank You!",
                ["account-notice"] = "Important Account Notice from {ClientName}",
                ["default"] = "Message from {ClientName}"
            },
            ["sms"] = new Dictionary<string, string>
            {
                ["payment-reminder"] = "Hi {DebtorName}, your payment of {Amount} is due on {DueDate}. Pay now: {PaymentUrl}",
                ["payment-confirmation"] = "Payment of {Amount} received. Thank you! - {ClientName}",
                ["account-notice"] = "Important: Please contact {ClientName} regarding your account {ReferenceId}",
                ["default"] = "Message from {ClientName}. Ref: {ReferenceId}"
            }
        };

        var contentTemplates = new Dictionary<string, Dictionary<string, string>>
        {
            ["email"] = new Dictionary<string, string>
            {
                ["payment-reminder"] = @"<h2>Payment Reminder</h2>
<p>Dear {DebtorName},</p>
<p>This is a friendly reminder that your payment of <strong>{Amount}</strong> is due on <strong>{DueDate}</strong>.</p>
<p>Reference Number: {ReferenceId}</p>
<p>To avoid any late fees or additional charges, please make your payment as soon as possible.</p>
<p><a href=""{PaymentUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px;"">Pay Now</a></p>
<p>If you have any questions or concerns, please don't hesitate to contact us.</p>
<p>Thank you for your prompt attention to this matter.</p>
<p>Best regards,<br/>{ClientName}</p>",
                ["payment-confirmation"] = @"<h2>Payment Received - Thank You!</h2>
<p>Dear {DebtorName},</p>
<p>We have successfully received your payment of <strong>{Amount}</strong>.</p>
<p>Reference Number: {ReferenceId}</p>
<p>Payment Date: {DueDate}</p>
<p>Your payment has been processed and your account has been updated accordingly.</p>
<p>Thank you for your prompt payment. We appreciate your business!</p>
<p>Best regards,<br/>{ClientName}</p>",
                ["default"] = @"<p>Dear {DebtorName},</p>
<p>This is a message from {ClientName} regarding reference {ReferenceId}.</p>
<p>Amount: {Amount}<br/>
Due Date: {DueDate}</p>
<p><a href=""{PaymentUrl}"">Click here for more information</a></p>
<p>Best regards,<br/>{ClientName}</p>"
            },
            ["sms"] = new Dictionary<string, string>
            {
                ["payment-reminder"] = "Hi {DebtorName}, this is a reminder that your payment of {Amount} is due on {DueDate}. Please pay at: {PaymentUrl} - {ClientName}",
                ["payment-confirmation"] = "Payment confirmed! We received your payment of {Amount}. Ref: {ReferenceId}. Thank you! - {ClientName}",
                ["default"] = "{ClientName}: Your payment of {Amount} is due {DueDate}. Ref: {ReferenceId}. Pay: {PaymentUrl}"
            }
        };

        if (!templates.ContainsKey(request.Type))
        {
            return BadRequest(new { error = "Invalid template type" });
        }

        var templateKey = DetermineTemplateKey(request.Name);
        var subject = request.Type == "email" && templates[request.Type].ContainsKey(templateKey)
            ? templates[request.Type][templateKey]
            : templates[request.Type]["default"];

        var content = contentTemplates[request.Type].ContainsKey(templateKey)
            ? contentTemplates[request.Type][templateKey]
            : contentTemplates[request.Type]["default"];

        return Json(new { subject, content });
    }

    [HttpPost]
    public IActionResult SuggestSubject([FromBody] SuggestSubjectRequest request)
    {
        // AI-powered subject line suggestions based on content
        var suggestion = GenerateSubjectFromContent(request.Content, request.Name);
        return Json(new { suggestion });
    }

    [HttpPost]
    public IActionResult ImproveContent([FromBody] ImproveContentRequest request)
    {
        // AI-powered content improvement
        var improved = EnhanceContent(request.Content, request.Type);
        return Json(new { improved });
    }

    private string DetermineTemplateKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "default";

        var lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("reminder") || lowerName.Contains("due"))
            return "payment-reminder";
        if (lowerName.Contains("confirmation") || lowerName.Contains("receipt") || lowerName.Contains("thank"))
            return "payment-confirmation";
        if (lowerName.Contains("notice") || lowerName.Contains("alert"))
            return "account-notice";

        return "default";
    }

    private string GenerateSubjectFromContent(string content, string? name)
    {
        // Simple AI simulation - analyze content to suggest subject
        if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{name} - Important Notice";
        }

        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("reminder") || lowerContent.Contains("due"))
            return "Payment Reminder: Action Required";
        if (lowerContent.Contains("thank") || lowerContent.Contains("received"))
            return "Payment Confirmation - Thank You!";
        if (lowerContent.Contains("overdue") || lowerContent.Contains("urgent"))
            return "Urgent: Overdue Payment Notice";
        if (lowerContent.Contains("payment plan") || lowerContent.Contains("arrangement"))
            return "Your Payment Plan Details";

        return "Important Message from {ClientName}";
    }

    private string EnhanceContent(string content, string type)
    {
        // Simple content enhancement - add professional touches
        if (string.IsNullOrWhiteSpace(content))
            return content;

        if (type == "email")
        {
            // Ensure email has proper structure
            if (!content.Contains("<p>") && !content.Contains("<h"))
            {
                // Wrap plain text in paragraphs
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                content = string.Join("", lines.Select(line => $"<p>{line.Trim()}</p>"));
            }

            // Ensure greeting if not present
            if (!content.ToLowerInvariant().Contains("dear") && !content.ToLowerInvariant().Contains("hello") && !content.ToLowerInvariant().Contains("hi "))
            {
                content = "<p>Dear {DebtorName},</p>\n" + content;
            }

            // Ensure closing if not present
            if (!content.ToLowerInvariant().Contains("regards") && !content.ToLowerInvariant().Contains("sincerely"))
            {
                content += "\n<p>Best regards,<br/>{ClientName}</p>";
            }
        }
        else if (type == "sms")
        {
            // Keep SMS concise and clear
            content = content.Trim();
            
            // Ensure it's not too long
            if (content.Length > 160)
            {
                // Suggest a shorter version
                if (content.Contains("http"))
                {
                    // Keep the URL
                    var urlIndex = content.IndexOf("http");
                    var beforeUrl = content.Substring(0, urlIndex).Trim();
                    var url = content.Substring(urlIndex);
                    content = beforeUrl.Length > 100 
                        ? beforeUrl.Substring(0, 97) + "... " + url
                        : beforeUrl + " " + url;
                }
                else
                {
                    content = content.Substring(0, 157) + "...";
                }
            }
        }

        return content;
    }
}

public class GenerateSampleRequest
{
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class SuggestSubjectRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class ImproveContentRequest
{
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
