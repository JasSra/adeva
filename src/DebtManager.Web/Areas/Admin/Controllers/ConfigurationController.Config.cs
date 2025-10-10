using System.Text.Json;
using DebtManager.Contracts.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.Admin.Controllers;

public partial class ConfigurationController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Secrets(string? q)
    {
        var dict = await _configService.GetAllAsync();
        var filtered = string.IsNullOrWhiteSpace(q)
            ? dict
            : dict.Where(kv => kv.Key.Contains(q, StringComparison.OrdinalIgnoreCase))
                  .ToDictionary(kv => kv.Key, kv => kv.Value);

        var entries = filtered.Select(kv => new ConfigEntryVm
        {
            Key = kv.Key,
            Value = kv.Value.value,
            IsSecret = kv.Value.isSecret
        }).OrderBy(x => x.Key).ToList();

        var groups = entries
            .GroupBy(e => e.Key.Contains(':') ? e.Key.Split(':', 2)[0] : "Other")
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        var envName = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName;
        var health = await ConfigHealthVm.BuildAsync(_configService, envName);
        return View(new SecretsVm { Entries = entries, Groups = groups, Health = health, Query = q ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ConfigEntryVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Key))
        {
            ModelState.AddModelError("Key", "Key is required");
        }
        else
        {
            var errors = ValidateConfigEntry(vm.Key, vm.Value);
            foreach (var err in errors)
            {
                ModelState.AddModelError("Value", err);
            }
        }
        if (!ModelState.IsValid)
        {
            TempData["Message"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Secrets");
        }
        
        // Audit log
        var existingValue = await _configService.GetAsync(vm.Key);
        var action = existingValue == null ? "CREATE_CONFIG" : "UPDATE_CONFIG";
        await _auditService.LogAsync(action, "Configuration", vm.Key, JsonSerializer.Serialize(new { 
            key = vm.Key, 
            isSecret = vm.IsSecret,
            valueChanged = existingValue != vm.Value 
        }));
        
        await _configService.SetAsync(vm.Key, vm.Value, vm.IsSecret);
        TempData["Message"] = $"Configuration '{vm.Key}' saved successfully";
        return RedirectToAction("Secrets");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            // Audit log
            await _auditService.LogAsync("DELETE_CONFIG", "Configuration", key,  key );
            
            await _configService.DeleteAsync(key);
            TempData["Message"] = $"Configuration '{key}' deleted successfully";
        }
        return RedirectToAction("Secrets");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reveal(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest();

        // Audit without exposing secret value
        await _auditService.LogAsync("REVEAL_CONFIG", "Configuration", key);

        var value = await _configService.GetAsync(key);
        return Json(new { key, value });
    }

    private static IEnumerable<string> ValidateConfigEntry(string key, string? value)
    {
        if (value == null) yield break;
        // Basic validations by known keys/prefixes
        if (key.Equals("Stripe:SecretKey", StringComparison.OrdinalIgnoreCase))
        {
            if (!value.StartsWith("sk_", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("rk_", StringComparison.OrdinalIgnoreCase))
                yield return "Stripe SecretKey should start with sk_";
            if (value.Length < 20) yield return "Stripe SecretKey looks too short";
        }
        if (key.Equals("Stripe:WebhookSecret", StringComparison.OrdinalIgnoreCase))
        {
            if (!value.StartsWith("whsec_", StringComparison.OrdinalIgnoreCase))
                yield return "Stripe WebhookSecret should start with whsec_";
        }
        if (key.Equals("Twilio:AccountSid", StringComparison.OrdinalIgnoreCase))
        {
            if (!value.StartsWith("AC", StringComparison.OrdinalIgnoreCase))
                yield return "Twilio AccountSid should start with AC";
        }
        if (key.Equals("Twilio:AuthToken", StringComparison.OrdinalIgnoreCase))
        {
            if (value.Length < 16) yield return "Twilio AuthToken looks too short";
        }
        if (key.Equals("OpenAI:ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            if (!value.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
                yield return "OpenAI ApiKey should start with sk-";
        }
        if (key.Equals("AbrApi:BaseUrl", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                yield return "AbrApi BaseUrl must be an absolute URL";
        }
        if (key.Equals("System:BootstrapComplete", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized != "true" && normalized != "false")
                yield return "System:BootstrapComplete must be 'true' or 'false'";
        }
    }
}

public class SecretsVm
{
    public List<ConfigEntryVm> Entries { get; set; } = new();
    public Dictionary<string, List<ConfigEntryVm>> Groups { get; set; } = new();
    public ConfigHealthVm Health { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}

public class ConfigHealthVm
{
    public List<string> MissingKeys { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool IsBootstrapComplete => MissingKeys.Count == 0;
    public string Environment { get; set; } = string.Empty;

    public static async Task<ConfigHealthVm> BuildAsync(IAppConfigService cfg, string envName)
    {
        var vm = new ConfigHealthVm { Environment = envName };

        // Allow sentinel to bypass checks
        var sentinel = (await cfg.GetAsync("System:BootstrapComplete"))?.Trim().ToLowerInvariant();
        if (sentinel == "true") return vm; // no missing keys

        var required = new List<string>();
        if (string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase) || string.Equals(envName, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            required.AddRange(new[] { "Stripe:SecretKey", "Stripe:WebhookSecret" });
        }
        else
        {
            // Dev: no hard requirements
        }

        foreach (var key in required)
        {
            var v = await cfg.GetAsync(key);
            if (string.IsNullOrWhiteSpace(v)) vm.MissingKeys.Add(key);
        }

        // Soft requirements
        var twilioSid = await cfg.GetAsync("Twilio:AccountSid");
        var twilioToken = await cfg.GetAsync("Twilio:AuthToken");
        if (string.IsNullOrWhiteSpace(twilioSid) || string.IsNullOrWhiteSpace(twilioToken))
        {
            vm.Warnings.Add("Twilio keys missing; SMS will be simulated.");
        }

        var abrBase = await cfg.GetAsync("AbrApi:BaseUrl");
        if (string.IsNullOrWhiteSpace(abrBase))
        {
            vm.Warnings.Add("ABR API BaseUrl not set; ABN validation uses stub.");
        }
        return vm;
    }
}

public class ConfigEntryVm
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSecret { get; set; }
}
