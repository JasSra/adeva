using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

[ApiController]
[Route("api/payment-plans")]
public class PaymentPlanApiController : ControllerBase
{
    private readonly IPaymentPlanGenerationService _planGenerationService;
    private readonly IDebtRepository _debtRepository;
    private readonly IPaymentPlanRepository _paymentPlanRepository;
    private readonly ILogger<PaymentPlanApiController> _logger;

    public PaymentPlanApiController(
        IPaymentPlanGenerationService planGenerationService,
        IDebtRepository debtRepository,
        IPaymentPlanRepository paymentPlanRepository,
        ILogger<PaymentPlanApiController> logger)
    {
        _planGenerationService = planGenerationService;
        _debtRepository = debtRepository;
        _paymentPlanRepository = paymentPlanRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get the three payment plan options for a debt
    /// </summary>
    /// <param name="debtId">The debt ID</param>
    [HttpGet("options/{debtId:guid}")]
    public async Task<IActionResult> GetPaymentPlanOptions(Guid debtId)
    {
        try
        {
            var debt = await _debtRepository.GetAsync(debtId);
            if (debt == null)
            {
                return NotFound(new { error = "Debt not found" });
            }

            var options = await _planGenerationService.GeneratePaymentPlanOptionsAsync(debt);
            
            _logger.LogInformation("Generated {Count} payment plan options for debt {DebtId}", 
                options.Count, debtId);

            return Ok(new { options });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment plan options for debt {DebtId}", debtId);
            return StatusCode(500, new { error = "Failed to generate payment plan options" });
        }
    }

    /// <summary>
    /// Accept a payment plan option and create the actual payment plan
    /// </summary>
    /// <param name="request">The selected option and user details</param>
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptPaymentPlan([FromBody] AcceptPaymentPlanRequest request)
    {
        if (request.DebtId == Guid.Empty)
        {
            return BadRequest(new { error = "Invalid debt ID" });
        }

        if (request.SelectedOption == null)
        {
            return BadRequest(new { error = "Payment plan option is required" });
        }

        try
        {
            var debt = await _debtRepository.GetAsync(request.DebtId);
            if (debt == null)
            {
                return NotFound(new { error = "Debt not found" });
            }

            var userId = request.UserId ?? "anonymous-user"; // TODO: Get from auth context
            PaymentPlan paymentPlan;

            // If it's a custom plan with user-provided schedule, use the custom creation method
            if (request.SelectedOption.Type == PaymentPlanType.Custom && 
                request.CustomSchedule != null && 
                request.CustomSchedule.Count > 0)
            {
                paymentPlan = await _planGenerationService.CreateCustomPaymentPlanAsync(
                    debt, 
                    request.CustomSchedule, 
                    userId);
            }
            else
            {
                paymentPlan = await _planGenerationService.CreatePaymentPlanFromOptionAsync(
                    debt, 
                    request.SelectedOption, 
                    userId);
            }

            // Attach plan to debt
            debt.AttachPaymentPlan(paymentPlan);

            // Save to database
            await _paymentPlanRepository.AddAsync(paymentPlan);
            await _paymentPlanRepository.SaveChangesAsync();

            _logger.LogInformation("Created payment plan {PlanId} for debt {DebtId} with type {Type}",
                paymentPlan.Id, debt.Id, paymentPlan.Type);

            return Ok(new
            {
                paymentPlanId = paymentPlan.Id,
                reference = paymentPlan.Reference,
                type = paymentPlan.Type.ToString(),
                totalPayable = paymentPlan.TotalPayable,
                installmentCount = paymentPlan.InstallmentCount,
                startDate = paymentPlan.StartDateUtc,
                requiresApproval = paymentPlan.RequiresManualReview,
                downPaymentAmount = paymentPlan.DownPaymentAmount,
                downPaymentDueDate = paymentPlan.DownPaymentDueAtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating payment plan for debt {DebtId}", request.DebtId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting payment plan for debt {DebtId}", request.DebtId);
            return StatusCode(500, new { error = "Failed to create payment plan" });
        }
    }

    /// <summary>
    /// Validate a custom payment schedule before accepting
    /// </summary>
    /// <param name="request">The custom schedule to validate</param>
    [HttpPost("validate-custom")]
    public async Task<IActionResult> ValidateCustomSchedule([FromBody] ValidateCustomScheduleRequest request)
    {
        if (request.DebtId == Guid.Empty)
        {
            return BadRequest(new { error = "Invalid debt ID" });
        }

        if (request.Schedule == null || request.Schedule.Count == 0)
        {
            return BadRequest(new { error = "Schedule is required" });
        }

        try
        {
            var debt = await _debtRepository.GetAsync(request.DebtId);
            if (debt == null)
            {
                return NotFound(new { error = "Debt not found" });
            }

            // The service will validate the schedule
            var userId = "validation-check";
            await _planGenerationService.CreateCustomPaymentPlanAsync(
                debt, 
                request.Schedule, 
                userId);

            return Ok(new { 
                isValid = true, 
                message = "Custom schedule is valid" 
            });
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new { 
                isValid = false, 
                error = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating custom schedule for debt {DebtId}", request.DebtId);
            return StatusCode(500, new { error = "Failed to validate schedule" });
        }
    }
}

public class AcceptPaymentPlanRequest
{
    public Guid DebtId { get; set; }
    public PaymentPlanOption SelectedOption { get; set; } = null!;
    public List<InstallmentPreview>? CustomSchedule { get; set; }
    public string? UserId { get; set; }
}

public class ValidateCustomScheduleRequest
{
    public Guid DebtId { get; set; }
    public List<InstallmentPreview> Schedule { get; set; } = new();
}
