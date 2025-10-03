using DebtManager.Domain.Common;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;

namespace DebtManager.Domain.Payments;

public enum TransactionStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
    Cancelled
}

public enum TransactionDirection
{
    Inbound,
    Outbound
}

public enum PaymentMethod
{
    Card,
    BankTransfer,
    DirectDebit,
    Cash,
    Cheque,
    ManualAdjustment,
    Other
}

public class Transaction : Entity
{
    public Guid DebtId { get; private set; }
    public Guid DebtorId { get; private set; }
    public Guid? PaymentPlanId { get; private set; }
    public Guid? PaymentInstallmentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public TransactionDirection Direction { get; private set; }
    public TransactionStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string Provider { get; private set; }
    public string ProviderRef { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public decimal? FeeAmount { get; private set; }
    public string? FeeCurrency { get; private set; }
    public string MetadataJson { get; private set; }

    public Debt? Debt { get; private set; }
    public Debtor? Debtor { get; private set; }
    public PaymentPlan? PaymentPlan { get; private set; }
    public PaymentInstallment? PaymentInstallment { get; private set; }

    private Transaction()
    {
        Currency = "AUD";
        Provider = string.Empty;
        ProviderRef = string.Empty;
        MetadataJson = string.Empty;
        Status = TransactionStatus.Pending;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public Transaction(
        Guid debtId,
        Guid debtorId,
        Guid? paymentPlanId,
        Guid? paymentInstallmentId,
        decimal amount,
        string currency,
        TransactionDirection direction,
        PaymentMethod method,
        string provider,
        string providerRef)
        : this()
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        DebtId = debtId;
        DebtorId = debtorId;
        PaymentPlanId = paymentPlanId;
        PaymentInstallmentId = paymentInstallmentId;
        Amount = amount;
        Currency = currency;
        Direction = direction;
        Method = method;
        Provider = provider;
        ProviderRef = providerRef;
    }

    public void MarkPending()
    {
        Status = TransactionStatus.Pending;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSettled(DateTime settledAtUtc, string? providerReference = null)
    {
        Status = TransactionStatus.Succeeded;
        SettledAtUtc = settledAtUtc;
        if (!string.IsNullOrWhiteSpace(providerReference))
        {
            ProviderRef = providerReference;
        }

        UpdatedAtUtc = settledAtUtc;
    }

    public void MarkFailed(string failureReason)
    {
        Status = TransactionStatus.Failed;
        FailureReason = failureReason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRefunded(DateTime refundedAtUtc, string? providerReference = null)
    {
        Status = TransactionStatus.Refunded;
        SettledAtUtc = refundedAtUtc;
        FailureReason = null;
        if (!string.IsNullOrWhiteSpace(providerReference))
        {
            ProviderRef = providerReference;
        }

        UpdatedAtUtc = refundedAtUtc;
    }

    public void Cancel(string reason)
    {
        Status = TransactionStatus.Cancelled;
        FailureReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyFee(decimal amount, string feeCurrency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        FeeAmount = amount;
        FeeCurrency = feeCurrency;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachMetadata(string metadataJson)
    {
        MetadataJson = metadataJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
