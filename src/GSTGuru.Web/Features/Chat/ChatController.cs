using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GSTGuru.Web.Features.Chat;

[ApiController]
[Route("api/chat")]
public class ChatController(ChatService chatService, IOptions<AiOptions> options, ChatProviderFactory factory) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request, CancellationToken ct = default)
    {
        if (request.Messages is null || request.Messages.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Messages), "At least one message is required.");
            return ValidationProblem(ModelState);
        }

        var response = await chatService.SendAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("tax-rate")]
    public async Task<ActionResult<TaxRateLookupResponse>> TaxRate(TaxRateLookupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            ModelState.AddModelError(nameof(request.Description), "Description is required.");
            return ValidationProblem(ModelState);
        }

        var response = await chatService.LookupTaxRateAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("status")]
    public ActionResult<object> Status()
    {
        var provider = factory.Resolve();
        return Ok(new
        {
            provider = provider.Name,
            isConfigured = provider.IsConfigured,
            model = options.Value.Model,
            configuredProvider = options.Value.Provider
        });
    }

    [HttpGet("suggestions")]
    public ActionResult<IEnumerable<string>> Suggestions()
        => Ok(new[]
        {
            "GST on ₹5000 at 18% intra-state",
            "HSN code for laptop",
            "Input Tax Credit eligibility",
            "GST registration threshold for services",
            "Difference between CGST/SGST and IGST",
            "Explain GSTR-1 vs GSTR-3B",
            "E-invoicing basics"
        });
}
