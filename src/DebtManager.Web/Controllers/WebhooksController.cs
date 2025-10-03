using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

[ApiController]
[Route("api/webhooks")] 
public class WebhooksController : ControllerBase
{
    [HttpPost("stripe")] 
    public IActionResult Stripe()
    {
        // TODO: verify signature and process events
        return Ok();
    }
}
