using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentApiController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IDebtRepository _debtRepository;
    private readonly ILogger<PaymentApiController> _logger;

    public PaymentApiController(
        IPaymentService paymentService,
        IDebtRepository debtRepository,
        ILogger<PaymentApiController> logger)
    {
        _paymentService = paymentService;
        _debtRepository = debtRepository;
        _logger = logger;
    }

    /// <summary>
    /// Find debt by reference number for anonymous payment
    /// </summary>
    [HttpGet("find-by-reference")]
    public async Task<IActionResult> FindDebtByReference([FromQuery] string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return BadRequest(new { error = "Reference is required" });
        }

        // Search by external account ID or client reference number
        var debts = await _debtRepository.GetPendingReconciliationAsync(DateTime.UtcNow.AddYears(10));
        var debt = debts.FirstOrDefault(d => 
            d.ExternalAccountId.Equals(reference, StringComparison.OrdinalIgnoreCase) ||
            d.ClientReferenceNumber.Equals(reference, StringComparison.OrdinalIgnoreCase));

        if (debt == null)
        {
            return NotFound(new { error = "Debt not found with the provided reference" });
        }

        return Ok(new
        {
            debtId = debt.Id,
            reference = debt.ClientReferenceNumber,
            amount = debt.OutstandingPrincipal,
            currency = debt.Currency,
            organizationId = debt.OrganizationId
        });
    }

    /// <summary>
    /// Get available payment methods
    /// </summary>
    [HttpGet("methods")]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var methods = await _paymentService.GetAvailablePaymentMethodsAsync();
        return Ok(methods);
    }

    /// <summary>
    /// Create payment intent for 2-click payment flow
    /// </summary>
    [HttpPost("create-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        if (request.DebtId == Guid.Empty)
        {
            return BadRequest(new { error = "Invalid debt ID" });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than zero" });
        }

        try
        {
            var result = await _paymentService.CreatePaymentIntentAsync(
                request.DebtId,
                request.Amount,
                request.Currency ?? "AUD");

            _logger.LogInformation(
                "Created payment intent {IntentId} for debt {DebtId}",
                result.IntentId, request.DebtId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for debt {DebtId}", request.DebtId);
            return StatusCode(500, new { error = "Failed to create payment intent" });
        }
    }

    /// <summary>
    /// Get payment status
    /// </summary>
    [HttpGet("status/{paymentIntentId}")]
    public async Task<IActionResult> GetPaymentStatus(string paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            return BadRequest(new { error = "Payment intent ID is required" });
        }

        try
        {
            var status = await _paymentService.GetPaymentStatusAsync(paymentIntentId);
            return Ok(new { status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {IntentId}", paymentIntentId);
            return StatusCode(500, new { error = "Failed to get payment status" });
        }
    }
}

public class CreatePaymentIntentRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
}
