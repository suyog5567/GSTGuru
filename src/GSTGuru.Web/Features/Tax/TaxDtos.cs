using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;

namespace GSTGuru.Web.Features.Tax;

/// <summary>
/// A single line for the quick calculator. Every member is optional so callers
/// can post only the fields they care about.
/// </summary>
public sealed class QuickCalcLine
{
    public string? Description { get; set; }

    public string? HsnCode { get; set; }

    public string? Unit { get; set; }

    public decimal Quantity { get; set; } = 1m;

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal GstRate { get; set; }

    public decimal CessRate { get; set; }

    public GstLineInput ToInput() => new()
    {
        Description = string.IsNullOrWhiteSpace(Description) ? "Item" : Description.Trim(),
        HsnCode = HsnCode,
        Unit = string.IsNullOrWhiteSpace(Unit) ? "NOS" : Unit.Trim().ToUpperInvariant(),
        Quantity = Quantity,
        UnitPrice = UnitPrice,
        DiscountPercent = DiscountPercent,
        GstRate = GstRate,
        CessRate = CessRate,
    };
}

public sealed class QuickCalcRequest
{
    public string? SellerStateCode { get; set; }

    public string? PlaceOfSupplyStateCode { get; set; }

    /// <summary>Optional override; when omitted the state codes decide intra vs inter.</summary>
    public SupplyType? SupplyType { get; set; }

    public SupplyCategory SupplyCategory { get; set; } = SupplyCategory.Taxable;

    public List<QuickCalcLine> Lines { get; set; } = new();

    public SupplyType ResolvedSupplyType(IGstCalculator calculator)
        => SupplyType ?? calculator.ResolveSupplyType(SellerStateCode ?? string.Empty, PlaceOfSupplyStateCode ?? string.Empty);
}
