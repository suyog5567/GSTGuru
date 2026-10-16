using GSTGuru.Web.Data;
using GSTGuru.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Features.Products;

[ApiController]
[Route("api/products")]
public class ProductsController(GstDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.HsnCode != null && p.HsnCode.Contains(term)));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => ProductDto.FromEntity(p))
            .ToListAsync(ct);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? NotFound() : Ok(ProductDto.FromEntity(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductInput input, CancellationToken ct)
    {
        var product = new Product();
        Apply(input, product);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductDto.FromEntity(product));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductInput input, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return NotFound();
        }

        Apply(input, product);
        await db.SaveChangesAsync(ct);

        return Ok(ProductDto.FromEntity(product));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return NotFound();
        }

        // Keep historical invoices intact: archive instead of deleting when the
        // product has been used, otherwise remove it outright.
        var isUsed = await db.InvoiceLines.AnyAsync(l => l.ProductId == id, ct);
        if (isUsed)
        {
            product.IsActive = false;
        }
        else
        {
            db.Products.Remove(product);
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(ProductInput input, Product product)
    {
        product.Name = input.Name.Trim();
        product.HsnCode = string.IsNullOrWhiteSpace(input.HsnCode) ? null : input.HsnCode.Trim();
        product.Unit = string.IsNullOrWhiteSpace(input.Unit) ? "NOS" : input.Unit.Trim().ToUpperInvariant();
        product.UnitPrice = input.UnitPrice;
        product.DefaultGstRate = input.DefaultGstRate;
        product.DefaultCessRate = input.DefaultCessRate;
        product.IsActive = input.IsActive;
    }
}
