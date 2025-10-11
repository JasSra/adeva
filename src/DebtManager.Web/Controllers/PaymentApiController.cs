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

    /// <summary>
    /// Get Stripe publishable key for frontend
    /// </summary>
    [HttpGet("stripe-key")]
    public async Task<IActionResult> GetStripePublishableKey()
    {
        try {
            var config = HttpContext.RequestServices.GetRequiredService<DebtManager.Contracts.Configuration.IAppConfigService>();
            var publishableKey = await config.GetAsync("Stripe:PublishableKey");
            
            if (string.IsNullOrWhiteSpace(publishableKey))
            {
                return StatusCode(500, new { error = "Stripe publishable key not configured" });
            }

            return Ok(new { publishableKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe publishable key");
            return StatusCode(500, new { error = "Failed to get Stripe key" });
        }
    }

    /// <summary>
    /// Create test payment intent for testing Stripe integration
    /// </summary>
    [HttpPost("create-test-intent")]
    public async Task<IActionResult> CreateTestPaymentIntent([FromBody] CreateTestPaymentIntentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than zero" });
        }

        try
        {
            // Create a test payment intent (not linked to any debt)
            var config = HttpContext.RequestServices.GetRequiredService<DebtManager.Contracts.Configuration.IAppConfigService>();
            var secretKey = await config.GetAsync("Stripe:SecretKey");
            
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return StatusCode(500, new { error = "Stripe secret key not configured" });
            }

            Stripe.StripeConfiguration.ApiKey = secretKey;

            var options = new Stripe.PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = request.Currency?.ToLowerInvariant() ?? "aud",
                AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "test_mode", "true" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var service = new Stripe.PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return Ok(new
            {
                intentId = paymentIntent.Id,
                clientSecret = paymentIntent.ClientSecret,
                amount = request.Amount,
                currency = request.Currency ?? "AUD"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test payment intent");
            return StatusCode(500, new { error = "Failed to create test payment intent" });
        }
    }
}

public class CreatePaymentIntentRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
}

public class CreateTestPaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
}
