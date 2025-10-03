namespace DebtManager.Contracts.External;

public interface IAbrValidator
{
    Task<bool> ValidateAsync(string abn, CancellationToken ct = default);
}
