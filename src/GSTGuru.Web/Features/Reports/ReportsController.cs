using GSTGuru.Web.Common;
using GSTGuru.Web.Data;
using GSTGuru.Web.Domain;
using GSTGuru.Web.Features.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Features.Reports;

[ApiController]
[Route("api/reports")]
public class ReportsController(GstDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var range = GetDateRange(from, to);
        var invoices = await QueryInvoices(db, range.From, range.To).ToListAsync(ct);

        var totalInvoices = invoices.Count;
        var taxable = invoices.Sum(i => i.TaxableValue);
        var tax = invoices.Sum(i => i.Cgst + i.Sgst + i.Igst + i.Cess);
        var grand = invoices.Sum(i => i.GrandTotal);

        var trend = await BuildMonthlyTrendAsync(db, range.To);

        var recent = invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(10)
            .Select(i => new RecentInvoiceDto(
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Party!.Name,
                i.Party.IsRegistered,
                i.GrandTotal,
                i.Status))
            .ToList();

        var topParties = invoices
            .GroupBy(i => i.Party!.Name)
            .Select(g => new TopPartyDto(g.Key, g.Sum(x => x.GrandTotal), g.Count()))
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToList();

        return Ok(new DashboardDto(totalInvoices, taxable, tax, grand, recent, trend, topParties, range.From, range.To));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TaxSummaryDto>> Summary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var range = GetDateRange(from, to);
        var invoices = await QueryInvoices(db, range.From, range.To).ToListAsync(ct);

        return Ok(new TaxSummaryDto(
            invoices.Sum(i => i.TaxableValue),
            invoices.Sum(i => i.Cgst),
            invoices.Sum(i => i.Sgst),
            invoices.Sum(i => i.Igst),
            invoices.Sum(i => i.Cess),
            invoices.Sum(i => i.GrandTotal),
            range.From,
            range.To));
    }

    [HttpGet("hsn")]
    public async Task<ActionResult<IEnumerable<HsnSummaryDto>>> HsnSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var range = GetDateRange(from, to);

        var lines = await db.InvoiceLines
            .AsNoTracking()
            .Where(l => l.Invoice!.Status == InvoiceStatus.Issued)
            .Where(l => l.Invoice!.InvoiceDate >= range.From && l.Invoice!.InvoiceDate <= range.To)
            .Select(l => new { l.HsnCode, l.GstRate, l.Unit, l.Quantity, l.TaxableValue, l.Igst, l.Cgst, l.Sgst, l.Cess, l.LineTotal })
            .ToListAsync(ct);

        var rows = lines
            .GroupBy(l => new { l.HsnCode, l.GstRate, l.Unit })
            .Select(g => new HsnSummaryDto(
                g.Key.HsnCode ?? string.Empty,
                g.Key.GstRate,
                g.Key.Unit,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.TaxableValue),
                g.Sum(x => x.Igst),
                g.Sum(x => x.Cgst),
                g.Sum(x => x.Sgst),
                g.Sum(x => x.Cess),
                g.Sum(x => x.LineTotal)))
            .OrderBy(r => r.HsnCode)
            .ThenBy(r => r.GstRate)
            .ToList();

        return Ok(rows);
    }

    [HttpGet("gstr1")]
    public async Task<ActionResult<Gstr1Dto>> Gstr1(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
        => Ok(await BuildGstr1Async(from, to, ct));

    private async Task<Gstr1Dto> BuildGstr1Async(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var range = GetDateRange(from, to);
        var invoices = await QueryInvoices(db, range.From, range.To)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.PlaceOfSupplyStateCode,
                i.TaxableValue,
                i.Igst,
                i.Cgst,
                i.Sgst,
                i.Cess,
                i.GrandTotal,
                i.SupplyCategory,
                i.SupplyType,
                i.ReverseCharge,
                Party = new { i.Party!.Name, i.Party.Gstin, i.Party.IsRegistered },
                Lines = i.Lines.Select(l => new { l.HsnCode, l.GstRate, l.Unit, l.Quantity, l.TaxableValue, l.Igst, l.Cgst, l.Sgst, l.Cess, l.LineTotal }).ToList(),
            })
            .ToListAsync(ct);

        var b2b = invoices
            .Where(i => i.Party.IsRegistered && !i.ReverseCharge)
            .Select(i => new B2BRowDto(
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Party.Gstin ?? string.Empty,
                i.Party.Name,
                i.PlaceOfSupplyStateCode,
                i.TaxableValue,
                i.Igst,
                i.Cgst,
                i.Sgst,
                i.Cess,
                i.GrandTotal,
                i.SupplyCategory.ToString(),
                i.SupplyType.ToString()))
            .OrderBy(r => r.InvoiceDate)
            .ThenBy(r => r.InvoiceNumber)
            .ToList();

        var b2c = invoices
            .Where(i => !i.Party.IsRegistered && i.SupplyCategory == SupplyCategory.Taxable)
            .GroupBy(i => new { i.PlaceOfSupplyStateCode, i.SupplyType })
            .Select(g => new B2CRowDto(
                g.Key.PlaceOfSupplyStateCode,
                g.Key.SupplyType.ToString(),
                g.Sum(x => x.TaxableValue),
                g.Sum(x => x.Igst),
                g.Sum(x => x.Cgst + x.Sgst),
                g.Sum(x => x.Cess),
                g.Sum(x => x.GrandTotal)))
            .OrderBy(r => r.PlaceOfSupplyStateCode)
            .ToList();

        var rateWise = invoices
            .SelectMany(i => i.Lines)
            .GroupBy(l => l.GstRate)
            .Select(g => new RateWiseDto(
                g.Key,
                g.Sum(x => x.TaxableValue),
                g.Sum(x => x.Igst),
                g.Sum(x => x.Cgst),
                g.Sum(x => x.Sgst),
                g.Sum(x => x.Cess)))
            .OrderBy(r => r.GstRate)
            .ToList();

        var hsn = invoices
            .SelectMany(i => i.Lines)
            .GroupBy(l => new { l.HsnCode, l.GstRate, l.Unit })
            .Select(g => new HsnSummaryDto(
                g.Key.HsnCode ?? string.Empty,
                g.Key.GstRate,
                g.Key.Unit,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.TaxableValue),
                g.Sum(x => x.Igst),
                g.Sum(x => x.Cgst),
                g.Sum(x => x.Sgst),
                g.Sum(x => x.Cess),
                g.Sum(x => x.LineTotal)))
            .OrderBy(r => r.HsnCode)
            .ThenBy(r => r.GstRate)
            .ToList();

        var docSummary = new DocSummaryDto(invoices.Count, invoices.Sum(i => i.GrandTotal));

        return new Gstr1Dto(range.From, range.To, b2b, b2c, rateWise, hsn, docSummary);
    }

    [HttpGet("gstr1/export.csv")]
    public async Task<IActionResult> Gstr1Csv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var gstr1 = await BuildGstr1Async(from, to, ct);

        var headers = new[]
        {
            "Invoice Number", "Invoice Date", "GSTIN", "Party Name", "POS State Code",
            "Taxable Value", "IGST", "CGST", "SGST", "Cess", "Grand Total", "Category", "Supply Type",
        };

        var rows = gstr1.B2B.Select(b => new object[]
        {
            b.InvoiceNumber,
            b.InvoiceDate.ToString("yyyy-MM-dd"),
            b.Gstin,
            b.PartyName,
            b.PlaceOfSupplyStateCode,
            b.TaxableValue,
            b.Igst,
            b.Cgst,
            b.Sgst,
            b.Cess,
            b.GrandTotal,
            b.Category,
            b.SupplyType,
        });

        var content = Csv.Write(headers, rows);
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/csv", $"GSTR-1-{gstr1.From:yyyy-MM-dd}-to-{gstr1.To:yyyy-MM-dd}.csv");
    }

    [HttpGet("gstr3b")]
    public async Task<ActionResult<Gstr3BDto>> Gstr3B(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var range = GetDateRange(from, to);
        var issued = await QueryInvoices(db, range.From, range.To)
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Select(i => new
            {
                i.SupplyCategory,
                i.TaxableValue,
                i.Cgst,
                i.Sgst,
                i.Igst,
                i.Cess,
            })
            .ToListAsync(ct);

        var section3_1a = issued.Where(i => i.SupplyCategory == SupplyCategory.Taxable).Sum(i => i.TaxableValue);
        var section3_1b = issued.Where(i => i.SupplyCategory == SupplyCategory.ZeroRated).Sum(i => i.TaxableValue);
        var section3_1c = issued
            .Where(i => i.SupplyCategory is SupplyCategory.Exempt or SupplyCategory.NilRated)
            .Sum(i => i.TaxableValue);

        var taxPayable = new TaxPayableDto(
            issued.Sum(i => i.Cgst),
            issued.Sum(i => i.Sgst),
            issued.Sum(i => i.Igst),
            issued.Sum(i => i.Cess));

        return Ok(new Gstr3BDto(range.From, range.To, section3_1a, section3_1b, section3_1c, taxPayable));
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<IEnumerable<MonthlyTax>>> Monthly(
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        var end = DateTime.UtcNow.Date;
        var start = new DateTime(end.Year, end.Month, 1).AddMonths(-Math.Max(1, months - 1));

        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end.AddDays(1))
            .Select(i => new { i.InvoiceDate, i.Cgst, i.Sgst, i.Igst, i.Cess })
            .ToListAsync(ct);

        var trend = new List<MonthlyTax>();
        for (var month = new DateTime(start.Year, start.Month, 1); month <= new DateTime(end.Year, end.Month, 1); month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);
            var tax = invoices
                .Where(i => i.InvoiceDate >= month && i.InvoiceDate < next)
                .Sum(i => i.Cgst + i.Sgst + i.Igst + i.Cess);
            trend.Add(new MonthlyTax(month.ToString("MMM yyyy"), tax));
        }

        return Ok(trend);
    }

    private static async Task<IReadOnlyList<MonthlyTax>> BuildMonthlyTrendAsync(GstDbContext db, DateTime to)
    {
        var start = new DateTime(to.Year, to.Month, 1).AddMonths(-5);

        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= to.AddDays(1))
            .Select(i => new { i.InvoiceDate, i.Cgst, i.Sgst, i.Igst, i.Cess })
            .ToListAsync();

        var trend = new List<MonthlyTax>();
        for (var month = new DateTime(start.Year, start.Month, 1); month <= new DateTime(to.Year, to.Month, 1); month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);
            var tax = invoices
                .Where(i => i.InvoiceDate >= month && i.InvoiceDate < next)
                .Sum(i => i.Cgst + i.Sgst + i.Igst + i.Cess);
            trend.Add(new MonthlyTax(month.ToString("MMM yyyy"), tax));
        }

        return trend;
    }

    private static DateRange GetDateRange(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow.Date;
        var f = from.HasValue ? from.Value.Date : now.AddMonths(-3).Date;
        var t = to.HasValue ? to.Value.Date.AddDays(1).AddTicks(-1) : now.AddDays(1).AddTicks(-1);
        if (t < f)
        {
            t = f.AddDays(1).AddTicks(-1);
        }

        return new DateRange(f, t);
    }

    private static IQueryable<Invoice> QueryInvoices(GstDbContext db, DateTime from, DateTime to)
        => db.Invoices
            .AsNoTracking()
            .Include(i => i.Party)
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);

    private sealed record DateRange(DateTime From, DateTime To);
}
