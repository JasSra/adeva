using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Configuration;

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
    private readonly IAppConfigService _appConfig;

    public AbrHttpValidator(HttpClient http, IAppConfigService appConfig, ILogger<AbrHttpValidator> logger)
    {
        _http = http;
        _logger = logger;
        _appConfig = appConfig;
    }

    public async Task<bool> ValidateAsync(string abn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(abn)) return false;
        try
        {
            var baseUrl = await _appConfig.GetAsync("AbrApi:BaseUrl", ct);
            var apiKey = await _appConfig.GetAsync("AbrApi:ApiKey", ct);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                // If not configured, fallback stub rules
                return abn.Length == 11;
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), $"validate?abn={Uri.EscapeDataString(abn)}"));
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                req.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
            }
            var resp = await _http.SendAsync(req, ct);
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
            var def = await _appConfig.GetAsync("AbrApi:DefinitionUrl", ct) ?? "https://abr.business.gov.au/ApiDocumentation";
            _logger.LogError(ex, "Error calling ABR service. See definition: {Url}", def);
            return false;
        }
    }

    private sealed class AbrValidationResponse
    {
        public bool IsValid { get; set; }
    }
}
