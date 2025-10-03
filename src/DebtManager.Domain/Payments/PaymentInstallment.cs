using System.Collections.Generic;
using System.Linq;
using DebtManager.Domain.Common;

namespace DebtManager.Domain.Payments;

public enum PaymentInstallmentStatus
{
    Scheduled,
    Paid,
    Partial,
    Failed,
    Skipped,
    WrittenOff,
    Cancelled
}

public class PaymentInstallment : Entity
{
    private readonly List<Transaction> _transactions;

    public Guid PaymentPlanId { get; private set; }
    public int Sequence { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public decimal AmountDue { get; private set; }
    public decimal AmountPaid { get; private set; }
    public PaymentInstallmentStatus Status { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public decimal LateFeeAmount { get; private set; }
    public string? TransactionReference { get; private set; }
    public string Notes { get; private set; }

    public PaymentPlan? PaymentPlan { get; private set; }
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private PaymentInstallment()
    {
        _transactions = new List<Transaction>();
        Notes = string.Empty;
        Status = PaymentInstallmentStatus.Scheduled;
    }

    public PaymentInstallment(Guid paymentPlanId, int sequence, DateTime dueAtUtc, decimal amountDue)
        : this()
    {
        if (amountDue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountDue));
        }

        PaymentPlanId = paymentPlanId;
        Sequence = sequence;
        DueAtUtc = dueAtUtc;
        AmountDue = amountDue;
    }

    public void Reschedule(DateTime newDueAtUtc)
    {
        DueAtUtc = newDueAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterPayment(decimal amount, DateTime paidAtUtc, string? transactionReference = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        AmountPaid += amount;
        PaidAtUtc = paidAtUtc;
        TransactionReference = transactionReference ?? TransactionReference;

        if (AmountPaid >= AmountDue)
        {
            Status = PaymentInstallmentStatus.Paid;
            AmountPaid = Math.Round(AmountPaid, 2);
        }
        else
        {
            Status = PaymentInstallmentStatus.Partial;
        }

        UpdatedAtUtc = paidAtUtc;
    }

    public void MarkFailed(string? reason = null)
    {
        Status = PaymentInstallmentStatus.Failed;
        AppendNote(reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSkipped(string? reason = null)
    {
        Status = PaymentInstallmentStatus.Skipped;
        AppendNote(reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyLateFee(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        LateFeeAmount += amount;
        AppendNote($"Late fee applied: {amount:F2}");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void WriteOff(string? reason = null)
    {
        Status = PaymentInstallmentStatus.WrittenOff;
        AppendNote(reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        Status = PaymentInstallmentStatus.Cancelled;
        AppendNote(reason);
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

    private void AppendNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes)
            ? note.Trim()
            : $"{Notes}\n{note.Trim()}";
    }
}
