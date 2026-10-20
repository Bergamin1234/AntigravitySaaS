using AntigravitySaaS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace AntigravitySaaS.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class StripeWebhookController(
    IConfiguration config, 
    IBillingService billingService,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    private readonly string _endpointSecret = config["Stripe:WebhookSecret"] 
        ?? throw new InvalidOperationException("Stripe:WebhookSecret não configurado.");

    [HttpPost("stripe")]
    public async Task<IActionResult> ReceiveEvent(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _endpointSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Assinatura do webhook Stripe inválida.");
            return BadRequest(new { Error = "Assinatura do webhook inválida." });
        }

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            if (stripeEvent.Data.Object is Session session)
            {
                if (Guid.TryParse(session.ClientReferenceId, out var userId))
                {
                    int creditsPurchased = 10;
                    if (session.Metadata != null && session.Metadata.TryGetValue("credits", out var metaCredits))
                    {
                        int.TryParse(metaCredits, out creditsPurchased);
                    }

                    // Injeção com idempotência (session.Id)
                    await billingService.AddCreditsAsync(userId, creditsPurchased, session.Id, cancellationToken);
                }
                else
                {
                    logger.LogError("Checkout sem ClientReferenceId válido. SessionId: {SessionId}", session.Id);
                }
            }
        }

        return Ok();
    }
}
