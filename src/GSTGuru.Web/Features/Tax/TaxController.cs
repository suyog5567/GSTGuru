using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;
using Microsoft.AspNetCore.Mvc;

namespace GSTGuru.Web.Features.Tax;

[ApiController]
[Route("api/tax")]
public class TaxController(IGstCalculator calculator) : ControllerBase
{
    [HttpPost("calculate")]
    public ActionResult<GstBreakdown> Calculate([FromBody] QuickCalcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines is null || request.Lines.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Lines), "At least one line is required.");
            return ValidationProblem(ModelState);
        }

        var supplyType = request.ResolvedSupplyType(calculator);
        var inputs = request.Lines.Select(l => l.ToInput()).ToList();
        var breakdown = calculator.Calculate(inputs, supplyType, request.SupplyCategory);

        return Ok(breakdown);
    }

    [HttpGet("hsn")]
    public ActionResult<IReadOnlyList<HsnEntry>> Hsn([FromQuery] string? search, [FromQuery] int limit = 20)
    {
        var results = HsnCatalog.Search(search, limit);
        return Ok(results);
    }

    [HttpGet("guess-hsn")]
    public ActionResult<HsnEntry?> Guess([FromQuery] string description)
        => Ok(HsnCatalog.Guess(description));

    [HttpGet("rates")]
    public ActionResult<IReadOnlyList<decimal>> Rates()
        => Ok(GstRates.Slabs);

    [HttpGet("statecodes")]
    public ActionResult<IReadOnlyList<StateCode>> StateCodes()
        => Ok(Gst.StateCodes.All);

    [HttpPost("guess")]
    public ActionResult<HsnEntry?> GuessFromBody([FromQuery] string description)
        => Ok(HsnCatalog.Guess(description));
}
