namespace GSTGuru.Web.Domain;

/// <summary>
/// A single line item on a tax invoice together with its computed tax split.
/// The tax columns are persisted snapshots of the calculation.
/// </summary>
public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    /// <summary>Optional link back to the catalogue product, if one was selected.</summary>
    public int? ProductId { get; set; }

    public Product? Product { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? HsnCode { get; set; }

    public string Unit { get; set; } = "NOS";

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    /// <summary>Line value before tax, after discount.</summary>
    public decimal TaxableValue { get; set; }

    public decimal GstRate { get; set; }

    public decimal CessRate { get; set; }

    public decimal Cgst { get; set; }

    public decimal Sgst { get; set; }

    public decimal Igst { get; set; }

    public decimal Cess { get; set; }

    public decimal LineTotal { get; set; }
}
