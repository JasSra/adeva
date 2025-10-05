namespace DebtManager.Domain.Payments;

/// <summary>
/// Represents a payment plan option presented to the debtor
/// </summary>
public class PaymentPlanOption
{
    public PaymentPlanType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? AdminFee { get; set; }
    public PaymentFrequency Frequency { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<InstallmentPreview> InstallmentSchedule { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    public bool IsRecommended { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal? DownPaymentAmount { get; set; }
    public DateTime? DownPaymentDueDate { get; set; }
}

/// <summary>
/// Preview of an individual installment in a payment plan
/// </summary>
public class InstallmentPreview
{
    public int Sequence { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
