using System.Collections.Generic;
using System.Linq;
using DebtManager.Domain.Common;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;

namespace DebtManager.Domain.Debts;

public enum DebtStatus
{
    PendingAssignment,
    Active,
    InArrears,
    Settled,
    WrittenOff,
    Disputed,
    Closed
}

public enum InterestCalculationMethod
{
    None,
    Simple,
    Compound
}

public class Debt : Entity
{
    private readonly List<PaymentPlan> _paymentPlans;
    private readonly List<Transaction> _transactions;

    public Guid OrganizationId { get; private set; }
    public Guid DebtorId { get; private set; }
    public string ExternalAccountId { get; private set; }
    public string ClientReferenceNumber { get; private set; }
    public string PortfolioCode { get; private set; }
    public string Category { get; private set; }
    public string Currency { get; private set; }
    public DebtStatus Status { get; private set; }
    public decimal OriginalPrincipal { get; private set; }
    public decimal OutstandingPrincipal { get; private set; }
    public decimal AccruedInterest { get; private set; }
    public decimal AccruedFees { get; private set; }
    public decimal? InterestRateAnnualPercentage { get; private set; }
    public InterestCalculationMethod InterestCalculationMethod { get; private set; }
    public decimal? LateFeeFlat { get; private set; }
    public decimal? LateFeePercentage { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public DateTime? LastPaymentAtUtc { get; private set; }
    public DateTime? NextActionAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public string? WriteOffReason { get; private set; }
    public string? DisputeReason { get; private set; }
    public string? AssignedCollectorUserId { get; private set; }
    public string Notes { get; private set; }
    public decimal? SettlementOfferAmount { get; private set; }
    public DateTime? SettlementOfferExpiresAtUtc { get; private set; }
    public int GraceDays { get; private set; }
    public string TagsCsv { get; private set; }

    public Debtor? Debtor { get; private set; }
    public Organization? Organization { get; private set; }
    public IReadOnlyCollection<PaymentPlan> PaymentPlans => _paymentPlans.AsReadOnly();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Debt()
    {
        _paymentPlans = new List<PaymentPlan>();
        _transactions = new List<Transaction>();
        ExternalAccountId = ClientReferenceNumber = PortfolioCode = Category = string.Empty;
        Currency = "AUD";
        Notes = TagsCsv = string.Empty;
        OpenedAtUtc = DateTime.UtcNow;
        Status = DebtStatus.PendingAssignment;
    }

    public Debt(Guid organizationId, Guid debtorId, decimal originalPrincipal, string currency, string externalAccountId, string? clientReferenceNumber = null)
        : this()
    {
        if (originalPrincipal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalPrincipal), "Principal must be greater than zero.");
        }

        OrganizationId = organizationId;
        DebtorId = debtorId;
        OriginalPrincipal = originalPrincipal;
        OutstandingPrincipal = originalPrincipal;
        Currency = currency;
        ExternalAccountId = externalAccountId;
        ClientReferenceNumber = clientReferenceNumber ?? string.Empty;
    }

    public void SetInterest(decimal? annualPercentageRate, InterestCalculationMethod method)
    {
        InterestRateAnnualPercentage = annualPercentageRate;
        InterestCalculationMethod = method;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetLateFees(decimal? flatFee, decimal? percentage)
    {
        if (flatFee is <= 0)
        {
            flatFee = null;
        }

        if (percentage is <= 0)
        {
            percentage = null;
        }

        LateFeeFlat = flatFee;
        LateFeePercentage = percentage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyPayment(decimal amount, DateTime paidAtUtc)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }

        OutstandingPrincipal = Math.Max(0, OutstandingPrincipal - amount);
        LastPaymentAtUtc = paidAtUtc;
        UpdatedAtUtc = paidAtUtc;
    }

    public void AccrueInterest(decimal amount, DateTime? asOfUtc = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Accrued interest cannot be negative.");
        }

        AccruedInterest += amount;
        OutstandingPrincipal += amount;
        UpdatedAtUtc = asOfUtc ?? DateTime.UtcNow;
    }

    public void AddFee(decimal amount, string reason, DateTime? appliedAtUtc = null)
    {
        if (Status is DebtStatus.Settled or DebtStatus.WrittenOff or DebtStatus.Closed)
        {
            throw new InvalidOperationException("Cannot add fees to a closed/settled debt.");
        }
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Fee amount must be positive.");
        }

        AccruedFees += amount;
        OutstandingPrincipal += amount;
        AppendNote($"Fee applied: {reason} ({Currency} {amount:F2})");
        UpdatedAtUtc = appliedAtUtc ?? DateTime.UtcNow;
    }

    public void SetDueDate(DateTime? dueDateUtc)
    {
        DueDateUtc = dueDateUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ScheduleNextAction(DateTime? nextActionUtc)
    {
        NextActionAtUtc = nextActionUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignCollector(string? userId)
    {
        AssignedCollectorUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetCategory(string? category, string? portfolioCode = null)
    {
        Category = category ?? string.Empty;
        PortfolioCode = portfolioCode ?? PortfolioCode;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGraceDays(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        GraceDays = days;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(DebtStatus status, string? reason = null)
    {
        Status = status;

        switch (status)
        {
            case DebtStatus.Settled or DebtStatus.WrittenOff or DebtStatus.Closed:
                ClosedAtUtc = DateTime.UtcNow;
                break;
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            AppendNote($"Status changed to {status}: {reason}");
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void WriteOff(string reason)
    {
        if (Status is DebtStatus.Settled or DebtStatus.WrittenOff or DebtStatus.Closed)
        {
            throw new InvalidOperationException("Debt is already closed or settled.");
        }
        SetStatus(DebtStatus.WrittenOff, reason);
        WriteOffReason = reason;
    }

    public void FlagDispute(string reason)
    {
        if (Status is DebtStatus.Settled or DebtStatus.WrittenOff or DebtStatus.Closed)
        {
            throw new InvalidOperationException("Cannot dispute a closed/settled debt.");
        }
        DisputeReason = reason;
        SetStatus(DebtStatus.Disputed, reason);
    }

    public void ResolveDispute()
    {
        if (Status != DebtStatus.Disputed)
        {
            throw new InvalidOperationException("Debt is not in disputed status.");
        }
        DisputeReason = null;
        SetStatus(DebtStatus.Active, "Dispute resolved");
    }

    public void ProposeSettlement(decimal amount, DateTime expiresAtUtc)
    {
        if (Status is DebtStatus.Settled or DebtStatus.WrittenOff or DebtStatus.Closed)
        {
            throw new InvalidOperationException("Cannot propose settlement for a closed/settled debt.");
        }
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }
        if (OutstandingPrincipal <= 0)
        {
            throw new InvalidOperationException("Debt has no outstanding principal to settle.");
        }
        if (amount > OutstandingPrincipal)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Settlement cannot exceed outstanding principal.");
        }
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Expiry must be in the future.");
        }

        SettlementOfferAmount = amount;
        SettlementOfferExpiresAtUtc = expiresAtUtc;
        AppendNote($"Settlement offer recorded for {Currency} {amount:F2} expiring {expiresAtUtc:u}");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearSettlementOffer()
    {
        SettlementOfferAmount = null;
        SettlementOfferExpiresAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachPaymentPlan(PaymentPlan paymentPlan)
    {
        if (paymentPlan is null)
        {
            throw new ArgumentNullException(nameof(paymentPlan));
        }

        if (_paymentPlans.Any(x => x.Id == paymentPlan.Id))
        {
            return;
        }

        _paymentPlans.Add(paymentPlan);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachTransaction(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (_transactions.Any(x => x.Id == transaction.Id))
        {
            return;
        }

        _transactions.Add(transaction);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AppendNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes)
            ? note.Trim()
            : $"{Notes}\n{note.Trim()}";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetTags(IEnumerable<string> tags)
    {
        TagsCsv = string.Join(',', tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AcceptSettlement()
    {
        if (SettlementOfferAmount is null || SettlementOfferExpiresAtUtc is null)
        {
            throw new InvalidOperationException("No active settlement offer to accept.");
        }
        if (SettlementOfferExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Settlement offer has expired.");
        }

        // Accepting a settlement closes the debt as settled.
        OutstandingPrincipal = 0;
        AccruedInterest = 0;
        AppendNote($"Settlement accepted for {Currency} {SettlementOfferAmount.Value:F2}.");
        ClearSettlementOffer();
        SetStatus(DebtStatus.Settled, "Settlement accepted");
    }

    public void RejectSettlement(string? reason = null)
    {
        if (SettlementOfferAmount is null)
        {
            throw new InvalidOperationException("No settlement offer to reject.");
        }
        AppendNote($"Settlement offer rejected{(string.IsNullOrWhiteSpace(reason) ? string.Empty : ": " + reason)}.");
        ClearSettlementOffer();
    }
}
