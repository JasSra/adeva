namespace DebtManager.Contracts.External;

public class AbrValidationResult
{
    public bool IsValid { get; set; }
    public string? BusinessName { get; set; }
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? Abn { get; set; }
    public string? Acn { get; set; }
    public string? EntityType { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAbrValidator
{
    /// <summary>
    /// Simple validation - returns true if ABN is valid
    /// </summary>
    Task<bool> ValidateAsync(string abn, CancellationToken ct = default);
    
    /// <summary>
    /// Full validation - returns detailed business information
    /// </summary>
    Task<AbrValidationResult> ValidateAbnAsync(string abn, CancellationToken ct = default);
}
