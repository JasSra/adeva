namespace DebtManager.Contracts.Payments;

/// <summary>
/// Service for generating payment receipts
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Generates a PDF receipt for a transaction
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Receipt PDF bytes</returns>
    Task<byte[]> GenerateReceiptPdfAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the receipt for a transaction if it exists
    /// </summary>
    Task<byte[]?> GetReceiptAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a receipt exists for a transaction
    /// </summary>
    Task<bool> HasReceiptAsync(Guid transactionId, CancellationToken ct = default);
}
