using GSTGuru.Web.Data;
using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Features.Parties;

[ApiController]
[Route("api/parties")]
public class PartiesController(GstDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PartyDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] PartyType? partyType,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = db.Parties.AsNoTracking();

        if (partyType is not null)
        {
            query = query.Where(p => p.PartyType == partyType);
        }

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Gstin != null && p.Gstin.Contains(term)));
        }

        var parties = await query
            .OrderBy(p => p.Name)
            .Select(p => PartyDto.FromEntity(p))
            .ToListAsync(ct);

        return Ok(parties);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartyDto>> GetById(int id, CancellationToken ct)
    {
        var party = await db.Parties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return party is null ? NotFound() : Ok(PartyDto.FromEntity(party));
    }

    [HttpPost]
    public async Task<ActionResult<PartyDto>> Create(PartyInput input, CancellationToken ct)
    {
        if (await GstinExistsAsync(input.Gstin, null, ct))
        {
            ModelState.AddModelError(nameof(input.Gstin), "Another party already uses this GSTIN.");
            return ValidationProblem(ModelState);
        }

        var party = new Party();
        Apply(input, party);

        db.Parties.Add(party);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = party.Id }, PartyDto.FromEntity(party));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartyDto>> Update(int id, PartyInput input, CancellationToken ct)
    {
        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (party is null)
        {
            return NotFound();
        }

        if (await GstinExistsAsync(input.Gstin, id, ct))
        {
            ModelState.AddModelError(nameof(input.Gstin), "Another party already uses this GSTIN.");
            return ValidationProblem(ModelState);
        }

        Apply(input, party);
        await db.SaveChangesAsync(ct);

        return Ok(PartyDto.FromEntity(party));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (party is null)
        {
            return NotFound();
        }

        var hasInvoices = await db.Invoices.AnyAsync(i => i.PartyId == id, ct);
        if (hasInvoices)
        {
            party.IsActive = false;
        }
        else
        {
            db.Parties.Remove(party);
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<bool> GstinExistsAsync(string? gstin, int? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(gstin))
        {
            return Task.FromResult(false);
        }

        var normalised = gstin.Trim().ToUpperInvariant();
        return db.Parties.AnyAsync(
            p => p.Gstin == normalised && (excludeId == null || p.Id != excludeId),
            ct);
    }

    private static void Apply(PartyInput input, Party party)
    {
        party.Name = input.Name.Trim();
        party.Gstin = string.IsNullOrWhiteSpace(input.Gstin) ? null : input.Gstin.Trim().ToUpperInvariant();
        party.PartyType = input.PartyType;
        party.StateCode = input.StateCode.Trim();
        party.StateName = string.IsNullOrWhiteSpace(input.StateName)
            ? StateCodes.NameFor(party.StateCode)
            : input.StateName.Trim();
        party.Address = input.Address?.Trim();
        party.Email = input.Email?.Trim();
        party.Phone = input.Phone?.Trim();
        party.IsActive = input.IsActive;
    }
}
