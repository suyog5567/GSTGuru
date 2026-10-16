using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Gst;

/// <summary>Input for a single invoice line before tax is computed.</summary>
public sealed record GstLineInput
{
    public string Description { get; init; } = string.Empty;

    public string? HsnCode { get; init; }

    public string Unit { get; init; } = "NOS";

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    /// <summary>Trade discount as a percentage applied before tax.</summary>
    public decimal DiscountPercent { get; init; }

    /// <summary>GST rate as a percentage, e.g. 18.</summary>
    public decimal GstRate { get; init; }

    /// <summary>Compensation cess rate as a percentage.</summary>
    public decimal CessRate { get; init; }
}

/// <summary>A line after GST has been applied, with the intra/inter split resolved.</summary>
public sealed record GstLineResult
{
    public string Description { get; init; } = string.Empty;

    public string? HsnCode { get; init; }

    public string Unit { get; init; } = "NOS";

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal DiscountPercent { get; init; }

    public decimal TaxableValue { get; init; }

    public decimal GstRate { get; init; }

    public decimal CessRate { get; init; }

    public decimal Cgst { get; init; }

    public decimal Sgst { get; init; }

    public decimal Igst { get; init; }

    public decimal Cess { get; init; }

    public decimal LineTotal { get; init; }
}

/// <summary>Aggregated totals for an invoice or a quick calculation.</summary>
public sealed record GstBreakdown
{
    public SupplyType SupplyType { get; init; }

    public decimal TaxableValue { get; init; }

    public decimal Cgst { get; init; }

    public decimal Sgst { get; init; }

    public decimal Igst { get; init; }

    public decimal Cess { get; init; }

    public decimal TotalTax { get; init; }

    /// <summary>Sum of every line total before the invoice-level rounding adjustment.</summary>
    public decimal SubTotal { get; init; }

    /// <summary>Adjustment applied so the grand total lands on a whole rupee.</summary>
    public decimal RoundOff { get; init; }

    public decimal GrandTotal { get; init; }

    public IReadOnlyList<GstLineResult> Lines { get; init; } = Array.Empty<GstLineResult>();
}
