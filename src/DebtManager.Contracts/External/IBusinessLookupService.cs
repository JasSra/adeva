namespace DebtManager.Contracts.External;

public interface IBusinessLookupService
{
    Task<bool> ValidateAcnAsync(string acn, CancellationToken ct = default);
}
