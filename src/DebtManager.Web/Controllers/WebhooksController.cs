using DebtManager.Contracts.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

[ApiController]
[Route("api/webhooks")] 
public class WebhooksController : ControllerBase
{
    private readonly IWebhookProcessor _webhookProcessor;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookProcessor webhookProcessor,
        ILogger<WebhooksController> logger)
    {
        _webhookProcessor = webhookProcessor;
        _logger = logger;
    }

    [HttpPost("stripe")] 
    public async Task<IActionResult> Stripe()
    {
        var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Stripe webhook request missing signature");
            return BadRequest("Missing signature");
        }

        try
        {
            await _webhookProcessor.ProcessStripeWebhookAsync(payload, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500);
        }
    }
}
