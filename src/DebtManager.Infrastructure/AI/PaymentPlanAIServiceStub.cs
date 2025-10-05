using DebtManager.Contracts.AI;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.AI;

/// <summary>
/// Stub implementation of AI service for payment plan optimization
/// This is a placeholder for future AI integration (e.g., Azure OpenAI, custom ML models)
/// </summary>
public class PaymentPlanAIServiceStub : IPaymentPlanAIService
{
    private readonly ILogger<PaymentPlanAIServiceStub> _logger;

    public PaymentPlanAIServiceStub(ILogger<PaymentPlanAIServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<InstallmentScheduleRecommendation> OptimizeInstallmentScheduleAsync(
        Debt debt,
        int targetWeeks,
        decimal minimumInstallment,
        CancellationToken ct = default)
    {
        _logger.LogInformation("AI service stub called for debt {DebtId}. Returning low confidence to trigger fallback.", debt.Id);
        
        // Return low confidence so the service falls back to rules-based approach
        // When real AI is implemented, this will call Azure OpenAI or similar service
        return Task.FromResult(new InstallmentScheduleRecommendation
        {
            RecommendedFrequency = PaymentFrequency.Weekly,
            InstallmentCount = 0,
            InstallmentAmount = 0,
            Schedule = new List<InstallmentPreview>(),
            Rationale = "AI service not yet configured - using rules-based approach",
            ConfidenceScore = 0.0m // Low confidence triggers fallback
        });
    }

    public Task<ScheduleValidationResult> ValidateCustomScheduleAsync(
        Debt debt,
        List<InstallmentPreview> proposedSchedule,
        CancellationToken ct = default)
    {
        _logger.LogInformation("AI validation stub called for debt {DebtId}. Deferring to rules-based validation.", debt.Id);
        
        // When real AI is implemented, this could use ML to detect unusual patterns
        // For now, we defer to rules-based validation by returning IsValid = true
        return Task.FromResult(new ScheduleValidationResult
        {
            IsValid = true,
            RequiresManualReview = false,
            Warnings = new List<string>(),
            Errors = new List<string>(),
            Recommendation = "AI validation not yet configured - using rules-based validation"
        });
    }
}
