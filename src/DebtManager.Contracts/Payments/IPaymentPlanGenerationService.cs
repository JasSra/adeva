using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;

namespace DebtManager.Contracts.Payments;

/// <summary>
/// Service for generating payment plan options for debtors
/// </summary>
public interface IPaymentPlanGenerationService
{
    /// <summary>
    /// Generates the three payment plan options for a debt:
    /// 1. Full payment with maximum discount
    /// 2. System-generated weekly plan with partial discount (AI + rules-based)
    /// 3. Custom payment schedule with admin fees
    /// </summary>
    /// <param name="debt">The debt to generate plans for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of payment plan options</returns>
    Task<IReadOnlyList<PaymentPlanOption>> GeneratePaymentPlanOptionsAsync(
        Debt debt,
        CancellationToken ct = default);
    
    /// <summary>
    /// Creates an actual payment plan from a selected option
    /// </summary>
    /// <param name="debt">The debt to attach the plan to</param>
    /// <param name="option">The selected payment plan option</param>
    /// <param name="userId">The user creating the plan</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created payment plan</returns>
    Task<PaymentPlan> CreatePaymentPlanFromOptionAsync(
        Debt debt,
        PaymentPlanOption option,
        string userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates a custom payment plan based on user-provided schedule
    /// </summary>
    /// <param name="debt">The debt to create plan for</param>
    /// <param name="customSchedule">User-defined payment schedule</param>
    /// <param name="userId">The user creating the plan</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created payment plan</returns>
    Task<PaymentPlan> CreateCustomPaymentPlanAsync(
        Debt debt,
        List<InstallmentPreview> customSchedule,
        string userId,
        CancellationToken ct = default);
}
