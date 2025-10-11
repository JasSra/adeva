using DebtManager.Contracts.AI;
using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace DebtManager.Application.Payments;

/// <summary>
/// Service for generating smart payment plan options using both AI and rules-based approaches
/// </summary>
public class PaymentPlanGenerationService : IPaymentPlanGenerationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPaymentPlanAIService? _aiService;
    private readonly ILogger<PaymentPlanGenerationService> _logger;

    public PaymentPlanGenerationService(
        IOrganizationRepository organizationRepository,
        ILogger<PaymentPlanGenerationService> logger,
        IPaymentPlanAIService? aiService = null)
    {
        _organizationRepository = organizationRepository;
        _logger = logger;
        _aiService = aiService;
    }

    public async Task<IReadOnlyList<PaymentPlanOption>> GeneratePaymentPlanOptionsAsync(
        Debt debt,
        CancellationToken ct = default)
    {
        var organization = await _organizationRepository.GetAsync(debt.OrganizationId, ct);
        if (organization is null)
        {
            throw new InvalidOperationException($"Organization {debt.OrganizationId} not found");
        }

        // Get or create default fee configuration
        var feeConfig = await GetOrCreateFeeConfigurationAsync(organization, ct);

        var options = new List<PaymentPlanOption>();

        // Option A: Full Payment with Maximum Discount
        options.Add(GenerateFullPaymentOption(debt, feeConfig));

        // Option B: System-Generated Weekly Plan with Partial Discount (AI + Rules-based)
        options.Add(await GenerateSystemGeneratedPlanAsync(debt, feeConfig, ct));

        // Option C: Custom Payment Schedule with Admin Fees
        options.Add(GenerateCustomPlanTemplateOption(debt, feeConfig));

        return options.AsReadOnly();
    }

    public Task<PaymentPlan> CreatePaymentPlanFromOptionAsync(
        Debt debt,
        PaymentPlanOption option,
        string userId,
        CancellationToken ct = default)
    {
        var reference = $"PP-{debt.Id:N}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var paymentPlan = new PaymentPlan(
            debt.Id,
            reference,
            option.Type,
            option.Frequency,
            option.StartDate,
            option.InstallmentAmount,
            option.InstallmentCount);

        paymentPlan.SetCreatedBy(userId);

        // Apply discount if applicable
        if (option.DiscountAmount.HasValue && option.DiscountAmount.Value > 0)
        {
            paymentPlan.ApplyDiscount(option.DiscountAmount.Value);
        }

        // Set down payment if applicable
        if (option.DownPaymentAmount.HasValue && option.DownPaymentAmount.Value > 0)
        {
            paymentPlan.SetDownPayment(option.DownPaymentAmount.Value, option.DownPaymentDueDate);
        }

        // Add installments to the plan
        foreach (var installment in option.InstallmentSchedule)
        {
            paymentPlan.ScheduleInstallment(installment.Sequence, installment.DueDate, installment.Amount);
        }

        // Custom plans require manual review
        if (option.Type == PaymentPlanType.Custom)
        {
            paymentPlan.RequireManualReview();
        }

        return Task.FromResult(paymentPlan);
    }

    public async Task<PaymentPlan> CreateCustomPaymentPlanAsync(
        Debt debt,
        List<InstallmentPreview> customSchedule,
        string userId,
        CancellationToken ct = default)
    {
        var organization = await _organizationRepository.GetAsync(debt.OrganizationId, ct);
        if (organization is null)
        {
            throw new InvalidOperationException($"Organization {debt.OrganizationId} not found");
        }

        var feeConfig = await GetOrCreateFeeConfigurationAsync(organization, ct);

        // Validate custom schedule
        if (_aiService != null)
        {
            var validation = await _aiService.ValidateCustomScheduleAsync(debt, customSchedule, ct);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Invalid custom schedule: {string.Join(", ", validation.Errors)}");
            }
        }
        else
        {
            // Rules-based validation
            ValidateCustomScheduleRulesBased(debt, customSchedule, feeConfig);
        }

        // Apply smart admin fees to each installment
        var totalScheduledAmount = customSchedule.Sum(i => i.Amount);
        var adminFeePerInstallment = CalculateSmartAdminFee(totalScheduledAmount, customSchedule.Count, feeConfig);

        // Adjust installments with admin fees
        var adjustedSchedule = customSchedule.Select(i => new InstallmentPreview
        {
            Sequence = i.Sequence,
            DueDate = i.DueDate,
            Amount = i.Amount + adminFeePerInstallment,
            Description = $"Payment installment + admin fee ({debt.Currency} {adminFeePerInstallment:F2})"
        }).ToList();

        var reference = $"PP-CUSTOM-{debt.Id:N}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var avgInstallment = adjustedSchedule.Average(i => i.Amount);
        
        var paymentPlan = new PaymentPlan(
            debt.Id,
            reference,
            PaymentPlanType.Custom,
            PaymentFrequency.Custom,
            adjustedSchedule.First().DueDate,
            avgInstallment,
            adjustedSchedule.Count);

        paymentPlan.SetCreatedBy(userId);
        paymentPlan.RequireManualReview();

        foreach (var installment in adjustedSchedule)
        {
            paymentPlan.ScheduleInstallment(installment.Sequence, installment.DueDate, installment.Amount);
        }

        paymentPlan.AppendNote($"Custom payment plan with {customSchedule.Count} installments. Admin fee per installment: {debt.Currency} {adminFeePerInstallment:F2}");

        return paymentPlan;
    }

    #region Private Helper Methods

    private Task<OrganizationFeeConfiguration> GetOrCreateFeeConfigurationAsync(
        Organization organization,
        CancellationToken ct)
    {
        // TODO: Load from database - for now create default
        return Task.FromResult(new OrganizationFeeConfiguration(organization.Id));
    }

    private PaymentPlanOption GenerateFullPaymentOption(Debt debt, OrganizationFeeConfiguration feeConfig)
    {
        var discountAmount = debt.OutstandingPrincipal * (feeConfig.FullPaymentDiscountPercentage / 100m);
        var totalAmount = debt.OutstandingPrincipal - discountAmount;

        return new PaymentPlanOption
        {
            Type = PaymentPlanType.FullSettlement,
            Title = "Pay in Full with Discount",
            Description = "Pay the entire debt now and receive maximum discount",
            OriginalAmount = debt.OutstandingPrincipal,
            DiscountAmount = discountAmount,
            DiscountPercentage = feeConfig.FullPaymentDiscountPercentage,
            TotalAmount = totalAmount,
            Frequency = PaymentFrequency.OneOff,
            InstallmentCount = 1,
            InstallmentAmount = totalAmount,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsRecommended = true,
            RequiresApproval = false,
            InstallmentSchedule = new List<InstallmentPreview>
            {
                new InstallmentPreview
                {
                    Sequence = 1,
                    DueDate = DateTime.UtcNow.AddDays(1),
                    Amount = totalAmount,
                    Description = "Full payment (one-time)"
                }
            },
            Benefits = new List<string>
            {
                $"Save {debt.Currency} {discountAmount:F2} ({feeConfig.FullPaymentDiscountPercentage}% discount)",
                "Debt settled immediately",
                "No ongoing payments or fees",
                "Best value option"
            }
        };
    }

    private async Task<PaymentPlanOption> GenerateSystemGeneratedPlanAsync(
        Debt debt,
        OrganizationFeeConfiguration feeConfig,
        CancellationToken ct)
    {
        InstallmentScheduleRecommendation? aiRecommendation = null;

        // Try AI service first
        if (_aiService != null)
        {
            try
            {
                aiRecommendation = await _aiService.OptimizeInstallmentScheduleAsync(
                    debt,
                    feeConfig.DefaultInstallmentPeriodWeeks,
                    feeConfig.MinimumInstallmentAmount,
                    ct);
                
                _logger.LogInformation("AI-optimized payment plan generated for debt {DebtId} with confidence {Confidence}",
                    debt.Id, aiRecommendation.ConfidenceScore);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI service failed for debt {DebtId}, falling back to rules-based approach", debt.Id);
            }
        }

        // Use AI recommendation or fall back to rules-based approach
        if (aiRecommendation != null && aiRecommendation.ConfidenceScore > 0.7m)
        {
            return BuildPlanOptionFromAIRecommendation(debt, feeConfig, aiRecommendation);
        }
        else
        {
            return GenerateRulesBasedSystemPlan(debt, feeConfig);
        }
    }

    private PaymentPlanOption GenerateRulesBasedSystemPlan(Debt debt, OrganizationFeeConfiguration feeConfig)
    {
        // Smart rules-based installment calculation
        var discountAmount = debt.OutstandingPrincipal * (feeConfig.SystemPlanDiscountPercentage / 100m);
        var amountAfterDiscount = debt.OutstandingPrincipal - discountAmount;

        // Determine smart installment count and amount
        var (installmentCount, installmentAmount) = CalculateSmartInstallments(
            amountAfterDiscount,
            feeConfig.MinimumInstallmentAmount,
            feeConfig.DefaultInstallmentPeriodWeeks,
            feeConfig.MaximumInstallmentCount);

        // Generate weekly schedule
        var schedule = new List<InstallmentPreview>();
        var startDate = DateTime.UtcNow.AddDays(7); // Start next week
        
        for (int i = 0; i < installmentCount; i++)
        {
            var dueDate = startDate.AddDays(i * 7); // Weekly payments
            var amount = i == installmentCount - 1
                ? amountAfterDiscount - (installmentAmount * (installmentCount - 1)) // Last installment gets remainder
                : installmentAmount;

            schedule.Add(new InstallmentPreview
            {
                Sequence = i + 1,
                DueDate = dueDate,
                Amount = amount,
                Description = $"Weekly installment {i + 1} of {installmentCount}"
            });
        }

        return new PaymentPlanOption
        {
            Type = PaymentPlanType.SystemGenerated,
            Title = "Weekly Payment Plan",
            Description = "Automated weekly installments with partial discount",
            OriginalAmount = debt.OutstandingPrincipal,
            DiscountAmount = discountAmount,
            DiscountPercentage = feeConfig.SystemPlanDiscountPercentage,
            TotalAmount = amountAfterDiscount,
            Frequency = PaymentFrequency.Weekly,
            InstallmentCount = installmentCount,
            InstallmentAmount = installmentAmount,
            StartDate = startDate,
            EndDate = schedule.Last().DueDate,
            IsRecommended = false,
            RequiresApproval = false,
            InstallmentSchedule = schedule,
            Benefits = new List<string>
            {
                $"Save {debt.Currency} {discountAmount:F2} ({feeConfig.SystemPlanDiscountPercentage}% discount)",
                $"Manageable weekly payments of ~{debt.Currency} {installmentAmount:F2}",
                "Automatic payment reminders",
                "Fixed schedule over " + installmentCount + " weeks"
            }
        };
    }

    private PaymentPlanOption BuildPlanOptionFromAIRecommendation(
        Debt debt,
        OrganizationFeeConfiguration feeConfig,
        InstallmentScheduleRecommendation recommendation)
    {
        var discountAmount = debt.OutstandingPrincipal * (feeConfig.SystemPlanDiscountPercentage / 100m);
        var totalAmount = recommendation.Schedule.Sum(s => s.Amount);

        return new PaymentPlanOption
        {
            Type = PaymentPlanType.SystemGenerated,
            Title = "AI-Optimized Payment Plan",
            Description = recommendation.Rationale,
            OriginalAmount = debt.OutstandingPrincipal,
            DiscountAmount = discountAmount,
            DiscountPercentage = feeConfig.SystemPlanDiscountPercentage,
            TotalAmount = totalAmount,
            Frequency = recommendation.RecommendedFrequency,
            InstallmentCount = recommendation.InstallmentCount,
            InstallmentAmount = recommendation.InstallmentAmount,
            StartDate = recommendation.Schedule.First().DueDate,
            EndDate = recommendation.Schedule.Last().DueDate,
            IsRecommended = true,
            RequiresApproval = false,
            InstallmentSchedule = recommendation.Schedule,
            Benefits = new List<string>
            {
                $"Save {debt.Currency} {discountAmount:F2} ({feeConfig.SystemPlanDiscountPercentage}% discount)",
                "AI-optimized for your situation",
                "Automatic payment reminders",
                recommendation.Rationale
            }
        };
    }

    private PaymentPlanOption GenerateCustomPlanTemplateOption(Debt debt, OrganizationFeeConfiguration feeConfig)
    {
        return new PaymentPlanOption
        {
            Type = PaymentPlanType.Custom,
            Title = "Custom Payment Schedule",
            Description = "Propose your own payment dates and amounts",
            OriginalAmount = debt.OutstandingPrincipal,
            TotalAmount = debt.OutstandingPrincipal,
            DiscountAmount = null,
            DiscountPercentage = null,
            AdminFee = feeConfig.CustomPlanAdminFeeFlat,
            Frequency = PaymentFrequency.Custom,
            InstallmentCount = 0,
            InstallmentAmount = 0,
            StartDate = DateTime.UtcNow.AddDays(7),
            IsRecommended = false,
            RequiresApproval = true,
            InstallmentSchedule = new List<InstallmentPreview>(),
            Benefits = new List<string>
            {
                "Flexible payment schedule",
                "Pay according to your cash flow",
                "Subject to admin approval"
            }
        };
    }

    /// <summary>
    /// Calculates smart installment count and amount to avoid silly scenarios
    /// </summary>
    private (int count, decimal amount) CalculateSmartInstallments(
        decimal totalAmount,
        decimal minimumInstallment,
        int targetWeeks,
        int maxInstallments)
    {
        // Rule 1: Never allow installments below minimum (e.g., no $10 payments for $5000 debt)
        // Rule 2: Prefer round numbers for installment amounts
        // Rule 3: Don't create unnecessarily long payment plans

        var idealInstallmentCount = (int)Math.Ceiling(totalAmount / minimumInstallment);
        
        // Cap at target weeks and max installments
        idealInstallmentCount = Math.Min(idealInstallmentCount, targetWeeks);
        idealInstallmentCount = Math.Min(idealInstallmentCount, maxInstallments);
        
        // Ensure at least 1 installment
        idealInstallmentCount = Math.Max(1, idealInstallmentCount);

        var baseInstallmentAmount = totalAmount / idealInstallmentCount;

        // Round to nearest 5 or 10 for cleaner amounts
        decimal roundedAmount;
        if (baseInstallmentAmount >= 100)
        {
            roundedAmount = Math.Ceiling(baseInstallmentAmount / 10) * 10; // Round to nearest $10
        }
        else if (baseInstallmentAmount >= 20)
        {
            roundedAmount = Math.Ceiling(baseInstallmentAmount / 5) * 5; // Round to nearest $5
        }
        else
        {
            roundedAmount = Math.Ceiling(baseInstallmentAmount); // Round to nearest $1
        }

        // Recalculate count based on rounded amount
        var adjustedCount = (int)Math.Ceiling(totalAmount / roundedAmount);
        adjustedCount = Math.Min(adjustedCount, maxInstallments);

        return (adjustedCount, roundedAmount);
    }

    /// <summary>
    /// Calculates smart admin fee to avoid silly amounts
    /// </summary>
    private decimal CalculateSmartAdminFee(
        decimal totalAmount,
        int installmentCount,
        OrganizationFeeConfiguration feeConfig)
    {
        var flatFee = feeConfig.CustomPlanAdminFeeFlat;
        var percentageFee = totalAmount * (feeConfig.CustomPlanAdminFeePercentage / 100m);
        var totalFee = flatFee + percentageFee;

        // Distribute fee evenly across installments, but keep it reasonable
        var feePerInstallment = totalFee / installmentCount;

        // Don't charge silly small amounts - round to nearest dollar
        if (feePerInstallment < 1)
        {
            feePerInstallment = 1; // Minimum $1 per installment
        }
        else if (feePerInstallment >= 5)
        {
            feePerInstallment = Math.Ceiling(feePerInstallment / 5) * 5; // Round to nearest $5
        }
        else
        {
            feePerInstallment = Math.Ceiling(feePerInstallment); // Round to nearest $1
        }

        return feePerInstallment;
    }

    private void ValidateCustomScheduleRulesBased(
        Debt debt,
        List<InstallmentPreview> schedule,
        OrganizationFeeConfiguration feeConfig)
    {
        if (schedule == null || schedule.Count == 0)
        {
            throw new InvalidOperationException("Custom schedule cannot be empty");
        }

        var totalScheduled = schedule.Sum(i => i.Amount);
        if (totalScheduled < debt.OutstandingPrincipal)
        {
            throw new InvalidOperationException("Custom schedule does not cover full debt amount");
        }

        // Check for silly small installments
        var tooSmallInstallments = schedule.Where(i => i.Amount < feeConfig.MinimumInstallmentAmount / 2).ToList();
        if (tooSmallInstallments.Any())
        {
            throw new InvalidOperationException(
                $"Some installments are too small. Minimum recommended: {debt.Currency} {feeConfig.MinimumInstallmentAmount / 2:F2}");
        }

        // Check for unreasonably long plans
        if (schedule.Count > feeConfig.MaximumInstallmentCount)
        {
            throw new InvalidOperationException($"Payment plan cannot exceed {feeConfig.MaximumInstallmentCount} installments");
        }
    }

    #endregion
}
