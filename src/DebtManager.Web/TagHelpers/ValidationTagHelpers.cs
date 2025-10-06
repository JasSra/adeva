using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DebtManager.Web.TagHelpers;

/// <summary>
/// Automatically applies validation styling to input fields
/// Usage: <input asp-for="PropertyName" asp-validation-class="true" />
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-validation-class")]
[HtmlTargetElement("select", Attributes = "asp-validation-class")]
[HtmlTargetElement("textarea", Attributes = "asp-validation-class")]
public class ValidationClassTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    [HtmlAttributeName("asp-validation-class")]
    public bool EnableValidationClass { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!EnableValidationClass || For == null || ViewContext == null)
        {
            return;
        }

        var fieldName = For.Name;
        var tempData = ViewContext.TempData;
        var validationErrors = tempData.Peek("ValidationErrors") as Dictionary<string, string[]>;

        bool hasError = false;

        // Check TempData validation errors
        if (validationErrors != null && validationErrors.ContainsKey(fieldName))
        {
            hasError = true;
        }

        // Check ModelState errors
        if (ViewContext.ViewData.ModelState.TryGetValue(fieldName, out var modelStateEntry))
        {
            if (modelStateEntry.Errors.Count > 0)
            {
                hasError = true;
            }
        }

        if (hasError)
        {
            // Get existing class attribute
            var existingClass = output.Attributes.FirstOrDefault(a => a.Name == "class");
            var classValue = existingClass?.Value?.ToString() ?? string.Empty;

            // Add error classes
            var errorClasses = " border-red-500 dark:border-red-400 focus:ring-red-500 focus:border-red-500";

            // Remove success classes if present
            classValue = classValue
                .Replace("border-green-500", "")
                .Replace("focus:ring-green-500", "")
                .Replace("focus:border-green-500", "")
                .Trim();

            // Add error classes
            classValue = (classValue + errorClasses).Trim();

            output.Attributes.SetAttribute("class", classValue);
            output.Attributes.SetAttribute("aria-invalid", "true");
            output.Attributes.SetAttribute("aria-describedby", $"{fieldName}-error");
        }
    }
}

/// <summary>
/// Displays validation errors for a field
/// Usage: <span asp-validation-for="PropertyName"></span>
/// </summary>
[HtmlTargetElement("span", Attributes = "asp-validation-for")]
public class ValidationMessageTagHelper : TagHelper
{
    [HtmlAttributeName("asp-validation-for")]
    public ModelExpression? For { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (For == null || ViewContext == null)
        {
            output.SuppressOutput();
            return;
        }

        var fieldName = For.Name;
        var tempData = ViewContext.TempData;
        var validationErrors = tempData.Peek("ValidationErrors") as Dictionary<string, string[]>;
        var errors = new List<string>();

        // Check TempData validation errors
        if (validationErrors != null && validationErrors.ContainsKey(fieldName))
        {
            errors.AddRange(validationErrors[fieldName]);
        }

        // Check ModelState errors
        if (ViewContext.ViewData.ModelState.TryGetValue(fieldName, out var modelStateEntry))
        {
            errors.AddRange(modelStateEntry.Errors.Select(e => e.ErrorMessage));
        }

        if (!errors.Any())
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "p";
        output.Attributes.SetAttribute("id", $"{fieldName}-error");
        output.Attributes.SetAttribute("class", "mt-1 text-sm text-red-600 dark:text-red-400");
        output.Attributes.SetAttribute("role", "alert");
        output.Content.SetContent(string.Join(", ", errors.Distinct()));
    }
}
