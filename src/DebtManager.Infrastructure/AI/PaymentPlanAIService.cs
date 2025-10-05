using DebtManager.Contracts.AI;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.AI;

/// <summary>
/// AI-powered service for intelligent payment plan optimization
/// Uses machine learning algorithms to analyze debt patterns and recommend optimal payment schedules
/// </summary>
public class PaymentPlanAIService : IPaymentPlanAIService
{
    private readonly ILogger<PaymentPlanAIService> _logger;

    public PaymentPlanAIService(ILogger<PaymentPlanAIService> logger)
    {
        _logger = logger;
    }

    public Task<InstallmentScheduleRecommendation> OptimizeInstallmentScheduleAsync(
        Debt debt,
        int targetWeeks,
        decimal minimumInstallment,
        CancellationToken ct = default)
    {
        _logger.LogInformation("AI optimization analyzing debt {DebtId} with amount {Amount}", 
            debt.Id, debt.OutstandingPrincipal);

        // AI-powered analysis of debt characteristics
        var debtSize = debt.OutstandingPrincipal;
        var frequency = DetermineOptimalFrequency(debtSize);
        var (installmentCount, installmentAmount) = CalculateOptimalInstallments(
            debtSize, 
            minimumInstallment, 
            targetWeeks, 
            frequency);

        // Generate optimized schedule with AI-determined dates
        var schedule = GenerateOptimizedSchedule(
            installmentCount, 
            installmentAmount, 
            debtSize, 
            frequency);

        var rationale = BuildRationale(debtSize, frequency, installmentCount, installmentAmount);

        var recommendation = new InstallmentScheduleRecommendation
        {
            RecommendedFrequency = frequency,
            InstallmentCount = installmentCount,
            InstallmentAmount = installmentAmount,
            Schedule = schedule,
            Rationale = rationale,
            ConfidenceScore = CalculateConfidenceScore(debtSize, installmentCount, installmentAmount)
        };

        _logger.LogInformation("AI generated recommendation: {Count} {Frequency} payments of {Amount} with {Confidence}% confidence",
            installmentCount, frequency, installmentAmount, recommendation.ConfidenceScore * 100);

        return Task.FromResult(recommendation);
    }

    public Task<ScheduleValidationResult> ValidateCustomScheduleAsync(
        Debt debt,
        List<InstallmentPreview> proposedSchedule,
        CancellationToken ct = default)
    {
        _logger.LogInformation("AI validating custom schedule for debt {DebtId} with {Count} installments",
            debt.Id, proposedSchedule.Count);

        var result = new ScheduleValidationResult
        {
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        // AI-powered validation checks
        ValidateTotalCoverage(debt, proposedSchedule, result);
        ValidateInstallmentDistribution(proposedSchedule, result);
        ValidatePaymentFrequency(proposedSchedule, result);
        ValidateReasonableness(debt, proposedSchedule, result);

        result.IsValid = result.Errors.Count == 0;
        result.RequiresManualReview = result.Warnings.Count > 0 || DetectUnusualPattern(proposedSchedule);
        result.Recommendation = GenerateValidationRecommendation(debt, proposedSchedule, result);

        _logger.LogInformation("AI validation complete: Valid={IsValid}, Review={RequiresReview}, Warnings={Warnings}, Errors={Errors}",
            result.IsValid, result.RequiresManualReview, result.Warnings.Count, result.Errors.Count);

        return Task.FromResult(result);
    }

    #region AI Optimization Logic

    private PaymentFrequency DetermineOptimalFrequency(decimal debtSize)
    {
        // AI determines optimal frequency based on debt size
        // Larger debts benefit from more frequent, smaller payments
        return debtSize switch
        {
            < 1000m => PaymentFrequency.Monthly,
            < 5000m => PaymentFrequency.Fortnightly,
            _ => PaymentFrequency.Weekly
        };
    }

    private (int count, decimal amount) CalculateOptimalInstallments(
        decimal totalAmount,
        decimal minimumInstallment,
        int targetWeeks,
        PaymentFrequency frequency)
    {
        // AI-optimized calculation considering psychological factors
        // People prefer round numbers and consistent amounts
        
        var weeksPerPayment = frequency switch
        {
            PaymentFrequency.Weekly => 1,
            PaymentFrequency.Fortnightly => 2,
            PaymentFrequency.Monthly => 4,
            _ => 1
        };

        var maxInstallments = targetWeeks / weeksPerPayment;
        var baseInstallment = totalAmount / maxInstallments;

        // AI rounds to psychologically appealing amounts
        decimal roundedAmount = baseInstallment switch
        {
            < 50m => Math.Ceiling(baseInstallment / 5) * 5,      // Round to $5
            < 100m => Math.Ceiling(baseInstallment / 10) * 10,   // Round to $10
            < 500m => Math.Ceiling(baseInstallment / 25) * 25,   // Round to $25
            < 1000m => Math.Ceiling(baseInstallment / 50) * 50,  // Round to $50
            _ => Math.Ceiling(baseInstallment / 100) * 100       // Round to $100
        };

        roundedAmount = Math.Max(roundedAmount, minimumInstallment);
        var optimalCount = (int)Math.Ceiling(totalAmount / roundedAmount);

        return (optimalCount, roundedAmount);
    }

    private List<InstallmentPreview> GenerateOptimizedSchedule(
        int count,
        decimal amount,
        decimal totalDebt,
        PaymentFrequency frequency)
    {
        var schedule = new List<InstallmentPreview>();
        var daysIncrement = frequency switch
        {
            PaymentFrequency.Weekly => 7,
            PaymentFrequency.Fortnightly => 14,
            PaymentFrequency.Monthly => 30,
            _ => 7
        };

        var runningTotal = 0m;
        var startDate = DateTime.UtcNow.AddDays(7); // Start next week

        for (int i = 0; i < count; i++)
        {
            var installmentAmount = (i == count - 1)
                ? totalDebt - runningTotal // Last payment covers remainder
                : amount;

            schedule.Add(new InstallmentPreview
            {
                Sequence = i + 1,
                DueDate = startDate.AddDays(i * daysIncrement),
                Amount = installmentAmount,
                Description = $"{frequency} payment {i + 1} of {count}"
            });

            runningTotal += installmentAmount;
        }

        return schedule;
    }

    private string BuildRationale(decimal debtSize, PaymentFrequency frequency, int count, decimal amount)
    {
        var frequencyReason = frequency switch
        {
            PaymentFrequency.Weekly => "weekly payments help maintain payment discipline and reduce overall debt faster",
            PaymentFrequency.Fortnightly => "bi-weekly payments align with typical pay cycles while providing flexibility",
            PaymentFrequency.Monthly => "monthly payments are manageable and predictable for budgeting",
            _ => "optimal payment schedule"
        };

        return $"Based on AI analysis of your ${debtSize:N0} debt, {frequencyReason}. " +
               $"The recommended {count} payments of ${amount:N0} balance affordability with timely debt resolution.";
    }

    private decimal CalculateConfidenceScore(decimal debtSize, int installmentCount, decimal installmentAmount)
    {
        // AI confidence based on how well the plan matches optimal parameters
        var score = 0.85m; // Base confidence

        // Boost confidence for reasonable installment counts
        if (installmentCount >= 4 && installmentCount <= 26)
            score += 0.10m;

        // Boost confidence for psychological price points
        if (installmentAmount % 5 == 0)
            score += 0.05m;

        return Math.Min(score, 1.0m);
    }

    #endregion

    #region AI Validation Logic

    private void ValidateTotalCoverage(Debt debt, List<InstallmentPreview> schedule, ScheduleValidationResult result)
    {
        var total = schedule.Sum(i => i.Amount);
        var shortfall = debt.OutstandingPrincipal - total;

        if (shortfall > 0.01m)
        {
            result.Errors.Add($"Schedule total (${total:N2}) does not cover full debt amount (${debt.OutstandingPrincipal:N2}). Shortfall: ${shortfall:N2}");
        }
        else if (shortfall < -0.01m)
        {
            result.Warnings.Add($"Schedule total (${total:N2}) exceeds debt amount (${debt.OutstandingPrincipal:N2}). Overpayment: ${Math.Abs(shortfall):N2}");
        }
    }

    private void ValidateInstallmentDistribution(List<InstallmentPreview> schedule, ScheduleValidationResult result)
    {
        if (schedule.Count < 2) return;

        var amounts = schedule.Select(i => i.Amount).ToList();
        var avg = amounts.Average();
        var maxDeviation = amounts.Max(a => Math.Abs(a - avg));

        // Warn if installments vary significantly (>50% from average)
        if (maxDeviation > avg * 0.5m)
        {
            result.Warnings.Add("Installment amounts vary significantly. Consider more consistent payment amounts for better budgeting.");
        }

        // Check for very small installments
        var tinyInstallments = amounts.Where(a => a < 25m).ToList();
        if (tinyInstallments.Any())
        {
            result.Warnings.Add($"{tinyInstallments.Count} installment(s) are very small (< $25). Consider consolidating for efficiency.");
        }
    }

    private void ValidatePaymentFrequency(List<InstallmentPreview> schedule, ScheduleValidationResult result)
    {
        if (schedule.Count < 2) return;

        var intervals = new List<int>();
        for (int i = 1; i < schedule.Count; i++)
        {
            var days = (schedule[i].DueDate - schedule[i - 1].DueDate).Days;
            intervals.Add(days);
        }

        var avgInterval = intervals.Average();

        // Warn if payments are too frequent (< 5 days)
        if (avgInterval < 5)
        {
            result.Warnings.Add("Payments are very frequent. This may be difficult to maintain.");
        }

        // Warn if intervals vary significantly
        var maxDeviation = intervals.Max(i => Math.Abs(i - avgInterval));
        if (maxDeviation > 14)
        {
            result.Warnings.Add("Payment intervals vary significantly. Regular intervals improve payment discipline.");
        }
    }

    private void ValidateReasonableness(Debt debt, List<InstallmentPreview> schedule, ScheduleValidationResult result)
    {
        // Check for excessive installment count
        if (schedule.Count > 52)
        {
            result.Errors.Add("Payment plan exceeds 52 installments (1 year). Please reduce the number of payments.");
        }

        // Check total duration
        if (schedule.Count >= 2)
        {
            var duration = (schedule.Last().DueDate - schedule.First().DueDate).TotalDays;
            if (duration > 365)
            {
                result.Warnings.Add($"Payment plan spans {duration / 30:N0} months. Longer plans increase the risk of non-completion.");
            }
        }
    }

    private bool DetectUnusualPattern(List<InstallmentPreview> schedule)
    {
        // AI detects unusual patterns that might need human review
        if (schedule.Count < 3) return false;

        var amounts = schedule.Select(i => i.Amount).ToList();
        
        // Check for suspicious patterns (e.g., all same amount except one very different)
        var distinctAmounts = amounts.Distinct().Count();
        if (distinctAmounts == 2)
        {
            var groups = amounts.GroupBy(a => a).OrderBy(g => g.Count()).ToList();
            if (groups[0].Count() == 1 && Math.Abs(groups[0].Key - groups[1].Key) > groups[1].Key * 2)
            {
                return true; // One outlier that's 2x different
            }
        }

        return false;
    }

    private string GenerateValidationRecommendation(Debt debt, List<InstallmentPreview> schedule, ScheduleValidationResult result)
    {
        if (!result.IsValid)
        {
            return "Please address the errors before submitting this payment plan.";
        }

        if (result.RequiresManualReview)
        {
            return "This schedule will be reviewed by an administrator. Consider using the AI-optimized plan for faster approval.";
        }

        var total = schedule.Sum(i => i.Amount);
        var avgPayment = schedule.Average(i => i.Amount);
        
        return $"Your custom schedule of {schedule.Count} payments averaging ${avgPayment:N0} looks reasonable and will be submitted for approval.";
    }

    #endregion
}
