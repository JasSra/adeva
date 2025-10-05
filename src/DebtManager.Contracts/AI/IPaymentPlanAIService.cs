using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;

namespace DebtManager.Contracts.AI;

/// <summary>
/// AI service for optimizing payment plan recommendations
/// </summary>
public interface IPaymentPlanAIService
{
    /// <summary>
    /// Uses AI to optimize payment plan installment schedule based on debt amount,
    /// debtor history, and best practices
    /// </summary>
    /// <param name="debt">The debt to optimize plan for</param>
    /// <param name="targetWeeks">Target number of weeks for the plan</param>
    /// <param name="minimumInstallment">Minimum acceptable installment amount</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Optimized installment schedule</returns>
    Task<InstallmentScheduleRecommendation> OptimizeInstallmentScheduleAsync(
        Debt debt,
        int targetWeeks,
        decimal minimumInstallment,
        CancellationToken ct = default);
    
    /// <summary>
    /// Determines if a custom payment schedule is reasonable
    /// </summary>
    /// <param name="debt">The debt</param>
    /// <param name="proposedSchedule">The proposed schedule</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ScheduleValidationResult> ValidateCustomScheduleAsync(
        Debt debt,
        List<InstallmentPreview> proposedSchedule,
        CancellationToken ct = default);
}

public class InstallmentScheduleRecommendation
{
    public PaymentFrequency RecommendedFrequency { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public List<InstallmentPreview> Schedule { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
}

public class ScheduleValidationResult
{
    public bool IsValid { get; set; }
    public bool RequiresManualReview { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}
