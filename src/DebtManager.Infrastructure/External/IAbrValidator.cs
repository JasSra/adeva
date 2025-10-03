using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using DebtManager.Contracts.External;

namespace DebtManager.Infrastructure.External;

public class AbrValidatorStub : IAbrValidator
{
    public Task<bool> ValidateAsync(string abn, CancellationToken ct = default)
    {
        // Placeholder: accept 11-digit numbers
        return Task.FromResult(!string.IsNullOrWhiteSpace(abn) && abn.Length == 11);
    }
}

public class AbrHttpValidator : IAbrValidator
{
    private readonly HttpClient _http;
    private readonly ILogger<AbrHttpValidator> _logger;
    private readonly string _definitionUrl;

    public AbrHttpValidator(HttpClient http, IConfiguration config, ILogger<AbrHttpValidator> logger)
    {
        _http = http;
        _logger = logger;
        var def = config["AbrApi:DefinitionUrl"];
        _definitionUrl = string.IsNullOrWhiteSpace(def) ? "https://abr.business.gov.au/ApiDocumentation" : def;
    }

    public async Task<bool> ValidateAsync(string abn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(abn)) return false;
        try
        {
            // Example GET /validate?abn=... expecting { isValid: bool }
            var resp = await _http.GetAsync($"validate?abn={Uri.EscapeDataString(abn)}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("ABR validation failed with status {Status}", resp.StatusCode);
                return false;
            }
            var payload = await resp.Content.ReadFromJsonAsync<AbrValidationResponse>(cancellationToken: ct);
            return payload?.IsValid ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ABR service. See definition: {Url}", _definitionUrl);
            return false;
        }
    }

    private sealed class AbrValidationResponse
    {
        public bool IsValid { get; set; }
    }
}
