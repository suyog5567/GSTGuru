using GSTGuru.Web.Data;
using GSTGuru.Web.Domain;
using GSTGuru.Web.Features.Invoices;
using GSTGuru.Web.Gst;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Features.Invoices;

[ApiController]
[Route("api/invoices")]
public class InvoicesController(GstDbContext db, IGstCalculator calculator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? partyId,
        [FromQuery] InvoiceStatus? status,
        CancellationToken ct = default)
    {
        var query = db.Invoices
            .AsNoTracking()
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= from.Value.Date);
        }
        if (to.HasValue)
        {
            var t = to.Value.Date.AddDays(1);
            query = query.Where(i => i.InvoiceDate < t);
        }
        if (partyId.HasValue)
        {
            query = query.Where(i => i.PartyId == partyId.Value);
        }
        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(ct);

        return Ok(invoices.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(ToDto(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(InvoiceCreateRequest request, CancellationToken ct)
    {
        if (!request.Lines.Any())
        {
            ModelState.AddModelError(nameof(request.Lines), "At least one line is required.");
            return ValidationProblem(ModelState);
        }

        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == request.PartyId, ct);
        if (party is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.PartyId)] = new[] { "Party not found." }
            }));
        }

        var profile = await db.BusinessProfiles.FirstOrDefaultAsync(ct);
        var seller = profile?.StateCode;
        if (string.IsNullOrWhiteSpace(seller))
        {
            seller = request.SellerStateCode;
        }
        if (string.IsNullOrWhiteSpace(seller))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.SellerStateCode)] = new[] { "Seller state code is required. Set it on the business profile." }
            }));
        }

        var pos = string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateCode) ? party.StateCode : request.PlaceOfSupplyStateCode!;

        var supplyType = request.SupplyType == default
            ? calculator.ResolveSupplyType(seller!, pos)
            : request.SupplyType;

        var lineInputs = request.Lines.Select(l => new GstLineInput
        {
            Description = l.Description.Trim(),
            HsnCode = string.IsNullOrWhiteSpace(l.HsnCode) ? null : l.HsnCode.Trim(),
            Unit = string.IsNullOrWhiteSpace(l.Unit) ? "NOS" : l.Unit.Trim().ToUpperInvariant(),
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            DiscountPercent = l.DiscountPercent,
            GstRate = l.GstRate,
            CessRate = l.CessRate,
        }).ToList();

        var breakdown = calculator.Calculate(lineInputs, supplyType, request.SupplyCategory);

        // Generate invoice number
        if (profile is null)
        {
            profile = new BusinessProfile { LegalName = "GST Guru", InvoicePrefix = "INV", NextInvoiceNumber = 1 };
            db.BusinessProfiles.Add(profile);
        }
        var invoiceNumber = $"{profile.InvoicePrefix}-{profile.NextInvoiceNumber:D4}";
        profile.NextInvoiceNumber++;

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate.Date,
            PartyId = party.Id,
            PlaceOfSupplyStateCode = pos,
            SellerStateCode = seller!,
            SupplyType = supplyType,
            SupplyCategory = request.SupplyCategory,
            ReverseCharge = request.ReverseCharge,
            Status = InvoiceStatus.Draft,
            Notes = request.Notes,
            TaxableValue = breakdown.TaxableValue,
            Cgst = breakdown.Cgst,
            Sgst = breakdown.Sgst,
            Igst = breakdown.Igst,
            Cess = breakdown.Cess,
            RoundOff = breakdown.RoundOff,
            GrandTotal = breakdown.GrandTotal,
            CreatedAt = DateTime.UtcNow,
        };

        var lines = new List<InvoiceLine>();
        for (var i = 0; i < lineInputs.Count; i++)
        {
            var result = breakdown.Lines[i];
            var reqLine = request.Lines[i];
            lines.Add(new InvoiceLine
            {
                ProductId = reqLine.ProductId,
                Description = result.Description,
                HsnCode = result.HsnCode,
                Unit = result.Unit,
                Quantity = result.Quantity,
                UnitPrice = result.UnitPrice,
                DiscountPercent = result.DiscountPercent,
                TaxableValue = result.TaxableValue,
                GstRate = result.GstRate,
                CessRate = result.CessRate,
                Cgst = result.Cgst,
                Sgst = result.Sgst,
                Igst = result.Igst,
                Cess = result.Cess,
                LineTotal = result.LineTotal,
            });
        }
        invoice.Lines = lines;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        var created = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPost("{id:int}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(int id, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
        {
            return NotFound();
        }
        if (invoice.Status != InvoiceStatus.Draft)
        {
            return BadRequest(new { message = "Only draft invoices can be issued." });
        }

        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(invoice));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<InvoiceDto>> Cancel(int id, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
        {
            return NotFound();
        }
        if (invoice.Status != InvoiceStatus.Issued)
        {
            return BadRequest(new { message = "Only issued invoices can be cancelled." });
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(invoice));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
        {
            return NotFound();
        }
        if (invoice.Status == InvoiceStatus.Issued)
        {
            return BadRequest(new { message = "Issued invoices cannot be deleted. Cancel them instead." });
        }

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static InvoiceDto ToDto(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.InvoiceDate,
        i.PartyId,
        i.Party?.Name ?? string.Empty,
        i.Party?.Gstin ?? string.Empty,
        i.PlaceOfSupplyStateCode,
        i.SellerStateCode,
        i.SupplyType,
        i.SupplyCategory,
        i.ReverseCharge,
        i.Status,
        i.Notes,
        i.TaxableValue,
        i.Cgst,
        i.Sgst,
        i.Igst,
        i.Cess,
        i.RoundOff,
        i.GrandTotal,
        i.CreatedAt,
        i.IssuedAt,
        i.Lines.Select(l => new InvoiceLineDto(
            l.Id,
            l.ProductId,
            l.Description,
            l.HsnCode,
            l.Unit,
            l.Quantity,
            l.UnitPrice,
            l.DiscountPercent,
            l.TaxableValue,
            l.GstRate,
            l.CessRate,
            l.Cgst,
            l.Sgst,
            l.Igst,
            l.Cess,
            l.LineTotal))
            .ToList());
}
