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
        return Task.FromResult(!string.IsNullOrWhiteSpace(abn) && abn.Replace(" ", "").Length == 11);
    }
    
    public Task<AbrValidationResult> ValidateAbnAsync(string abn, CancellationToken ct = default)
    {
        var isValid = !string.IsNullOrWhiteSpace(abn) && abn.Replace(" ", "").Length == 11;
        return Task.FromResult(new AbrValidationResult
        {
            IsValid = isValid,
            Abn = abn?.Replace(" ", ""),
            BusinessName = isValid ? "Test Business Pty Ltd" : null,
            LegalName = isValid ? "Test Business Pty Ltd" : null,
            TradingName = null,
            EntityType = isValid ? "Private Company" : null,
            ErrorMessage = isValid ? null : "Invalid ABN format"
        });
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
        var result = await ValidateAbnAsync(abn, ct);
        return result.IsValid;
    }

    public async Task<AbrValidationResult> ValidateAbnAsync(string abn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(abn))
        {
            return new AbrValidationResult
            {
                IsValid = false,
                ErrorMessage = "ABN is required"
            };
        }

        try
        {
            var baseUrl = await _appConfig.GetAsync("AbrApi:BaseUrl", ct);
            var apiKey = await _appConfig.GetAsync("AbrApi:ApiKey", ct);
            
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                // If not configured, fallback to stub behavior
                return await new AbrValidatorStub().ValidateAbnAsync(abn, ct);
            }

            var cleanAbn = abn.Replace(" ", "");
            
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), $"validate?abn={Uri.EscapeDataString(cleanAbn)}"));
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                req.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
            }
            
            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("ABR validation failed with status {Status}", resp.StatusCode);
                return new AbrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"ABR service returned {resp.StatusCode}"
                };
            }
            
            var payload = await resp.Content.ReadFromJsonAsync<AbrValidationResponse>(cancellationToken: ct);
            
            return new AbrValidationResult
            {
                IsValid = payload?.IsValid ?? false,
                BusinessName = payload?.BusinessName,
                LegalName = payload?.LegalName,
                TradingName = payload?.TradingName,
                Abn = payload?.Abn ?? cleanAbn,
                Acn = payload?.Acn,
                EntityType = payload?.EntityType,
                ErrorMessage = payload?.IsValid == false ? "ABN not active or not found" : null
            };
        }
        catch (Exception ex)
        {
            var def = await _appConfig.GetAsync("AbrApi:DefinitionUrl", ct) ?? "https://abr.business.gov.au/ApiDocumentation";
            _logger.LogError(ex, "Error calling ABR service. See definition: {Url}", def);
            return new AbrValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error validating ABN: {ex.Message}"
            };
        }
    }

    private sealed class AbrValidationResponse
    {
        public bool IsValid { get; set; }
        public string? BusinessName { get; set; }
        public string? LegalName { get; set; }
        public string? TradingName { get; set; }
        public string? Abn { get; set; }
        public string? Acn { get; set; }
        public string? EntityType { get; set; }
    }
}
