using GSTGuru.Web.Data;
using GSTGuru.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Features.Business;

[ApiController]
[Route("api/business")]
public class BusinessController(GstDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BusinessProfileDto>> Get(CancellationToken ct = default)
    {
        var profile = await db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(ct);
        if (profile is null)
        {
            return NotFound(new { message = "No business profile configured yet." });
        }

        return Ok(BusinessProfileDto.FromEntity(profile));
    }

    [HttpPut]
    public async Task<ActionResult<BusinessProfileDto>> Update(BusinessProfileInput input, CancellationToken ct = default)
    {
        var profile = await db.BusinessProfiles.FirstOrDefaultAsync(ct);
        if (profile is null)
        {
            profile = new BusinessProfile();
            db.BusinessProfiles.Add(profile);
        }

        Apply(input, profile);
        await db.SaveChangesAsync(ct);
        return Ok(BusinessProfileDto.FromEntity(profile));
    }

    private static void Apply(BusinessProfileInput input, BusinessProfile profile)
    {
        profile.LegalName = input.LegalName.Trim();
        profile.TradeName = string.IsNullOrWhiteSpace(input.TradeName) ? null : input.TradeName.Trim();
        profile.Gstin = string.IsNullOrWhiteSpace(input.Gstin) ? null : input.Gstin.Trim().ToUpperInvariant();
        profile.StateCode = input.StateCode.Trim();
        profile.StateName = input.StateName?.Trim();
        profile.Address = input.Address?.Trim();
        profile.Email = input.Email?.Trim();
        profile.Phone = input.Phone?.Trim();
        profile.InvoicePrefix = string.IsNullOrWhiteSpace(input.InvoicePrefix) ? "INV" : input.InvoicePrefix.Trim().ToUpperInvariant();
        if (input.NextInvoiceNumber > 0)
        {
            profile.NextInvoiceNumber = input.NextInvoiceNumber;
        }
    }
}
