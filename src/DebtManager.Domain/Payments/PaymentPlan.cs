using System.Collections.Generic;
using System.Linq;
using DebtManager.Domain.Common;
using DebtManager.Domain.Debts;

namespace DebtManager.Domain.Payments;

public enum PaymentPlanType
{
    FullSettlement,
    SystemGenerated,
    Custom,
    Hardship,
    Trial
}

public enum PaymentPlanStatus
{
    Draft,
    PendingApproval,
    Active,
    Completed,
    Cancelled,
    Defaulted
}

public enum PaymentFrequency
{
    OneOff,
    Weekly,
    Fortnightly,
    Monthly,
    Quarterly,
    Custom
}

public class PaymentPlan : Entity
{
    private readonly List<PaymentInstallment> _installments;
    private readonly List<Transaction> _transactions;

    public Guid DebtId { get; private set; }
    public string Reference { get; private set; }
    public PaymentPlanType Type { get; private set; }
    public PaymentPlanStatus Status { get; private set; }
    public PaymentFrequency Frequency { get; private set; }
    public DateTime StartDateUtc { get; private set; }
    public DateTime? EndDateUtc { get; private set; }
    public decimal InstallmentAmount { get; private set; }
    public int InstallmentCount { get; private set; }
    public decimal TotalPayable { get; private set; }
    public decimal? DiscountAmount { get; private set; }
    public decimal? DownPaymentAmount { get; private set; }
    public DateTime? DownPaymentDueAtUtc { get; private set; }
    public int GracePeriodInDays { get; private set; }
    public bool RequiresManualReview { get; private set; }
    public string Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? DefaultedAtUtc { get; private set; }
    public string? CreatedByUserId { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public string TagsCsv { get; private set; }

    public Debt? Debt { get; private set; }
    public IReadOnlyCollection<PaymentInstallment> Installments => _installments.AsReadOnly();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private PaymentPlan()
    {
        Reference = string.Empty;
        Notes = TagsCsv = string.Empty;
        _installments = new List<PaymentInstallment>();
        _transactions = new List<Transaction>();
        Status = PaymentPlanStatus.Draft;
        Frequency = PaymentFrequency.OneOff;
        StartDateUtc = DateTime.UtcNow;
    }

    public PaymentPlan(Guid debtId, string reference, PaymentPlanType type, PaymentFrequency frequency, DateTime startDateUtc, decimal installmentAmount, int installmentCount)
        : this()
    {
        if (installmentAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentAmount), "Installment amount must be positive.");
        }

        if (installmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentCount), "Installment count must be positive.");
        }

        DebtId = debtId;
        Reference = reference;
        Type = type;
        Frequency = frequency;
        StartDateUtc = startDateUtc;
        InstallmentAmount = installmentAmount;
        InstallmentCount = installmentCount;
        TotalPayable = installmentAmount * installmentCount;
    }

    public void SetCreatedBy(string userId)
    {
        CreatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RequireManualReview()
    {
        RequiresManualReview = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGracePeriod(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        GracePeriodInDays = days;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDownPayment(decimal? amount, DateTime? dueAtUtc)
    {
        if (amount is <= 0)
        {
            amount = null;
        }

        DownPaymentAmount = amount;
        DownPaymentDueAtUtc = amount.HasValue ? dueAtUtc : null;
        RecalculateTotals();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal? discountAmount)
    {
        if (discountAmount is <= 0)
        {
            DiscountAmount = null;
        }
        else
        {
            DiscountAmount = discountAmount;
        }

        RecalculateTotals();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateSchedule(PaymentFrequency frequency, DateTime startDateUtc)
    {
        Frequency = frequency;
        StartDateUtc = startDateUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(string approvedByUserId, DateTime? activatedAtUtc = null)
    {
        Status = PaymentPlanStatus.Active;
        ApprovedByUserId = approvedByUserId;
        UpdatedAtUtc = activatedAtUtc ?? DateTime.UtcNow;
    }

    public void Complete(DateTime? completedAtUtc = null)
    {
        Status = PaymentPlanStatus.Completed;
        EndDateUtc = completedAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = EndDateUtc;
    }

    public void Cancel(string reason, DateTime? cancelledAtUtc = null)
    {
        Status = PaymentPlanStatus.Cancelled;
        CancellationReason = reason;
        CancelledAtUtc = cancelledAtUtc ?? DateTime.UtcNow;
        AppendNote($"Plan cancelled: {reason}");
        UpdatedAtUtc = CancelledAtUtc;
    }

    public void MarkDefaulted(DateTime? defaultedAtUtc = null, string? notes = null)
    {
        Status = PaymentPlanStatus.Defaulted;
        DefaultedAtUtc = defaultedAtUtc ?? DateTime.UtcNow;
        AppendNote(notes);
        UpdatedAtUtc = DefaultedAtUtc;
    }

    public PaymentInstallment ScheduleInstallment(int sequence, DateTime dueAtUtc, decimal amountDue)
    {
        var installment = new PaymentInstallment(Id, sequence, dueAtUtc, amountDue);
        _installments.Add(installment);
        RecalculateTotals();
        UpdatedAtUtc = DateTime.UtcNow;
        return installment;
    }

    public void AddInstallment(PaymentInstallment installment)
    {
        if (installment.PaymentPlanId != Id)
        {
            throw new InvalidOperationException("Installment does not belong to this plan.");
        }

        _installments.Add(installment);
        RecalculateTotals();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveInstallment(Guid installmentId)
    {
        var existing = _installments.FirstOrDefault(x => x.Id == installmentId);
        if (existing is null)
        {
            return;
        }

        _installments.Remove(existing);
        RecalculateTotals();
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

    private void RecalculateTotals()
    {
        if (_installments.Count == 0)
        {
            TotalPayable = (DownPaymentAmount ?? 0) - (DiscountAmount ?? 0);
            InstallmentCount = 0;
            EndDateUtc = null;
            return;
        }

        InstallmentCount = _installments.Count;
        InstallmentAmount = Math.Round(_installments.Average(x => x.AmountDue), 2);
        TotalPayable = _installments.Sum(x => x.AmountDue) + (DownPaymentAmount ?? 0) - (DiscountAmount ?? 0);
        EndDateUtc = _installments.Max(x => x.DueAtUtc);
    }
}
