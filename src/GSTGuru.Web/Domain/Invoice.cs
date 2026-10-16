namespace GSTGuru.Web.Domain;

/// <summary>
/// A GST tax invoice raised by the business. Totals are stored (rather than
/// recomputed on read) so a historical invoice is immutable even if product
/// prices or tax rates change later.
/// </summary>
public class Invoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; }

    public int PartyId { get; set; }

    public Party? Party { get; set; }

    /// <summary>Two-digit state code where the supply is deemed to be received.</summary>
    public string PlaceOfSupplyStateCode { get; set; } = string.Empty;

    /// <summary>Two-digit state code of the registered business issuing the invoice.</summary>
    public string SellerStateCode { get; set; } = string.Empty;

    public SupplyType SupplyType { get; set; }

    public SupplyCategory SupplyCategory { get; set; } = SupplyCategory.Taxable;

    /// <summary>Whether tax is payable under reverse charge by the recipient.</summary>
    public bool ReverseCharge { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string? Notes { get; set; }

    public decimal TaxableValue { get; set; }

    public decimal Cgst { get; set; }

    public decimal Sgst { get; set; }

    public decimal Igst { get; set; }

    public decimal Cess { get; set; }

    /// <summary>Rounding adjustment applied to reach the nearest rupee.</summary>
    public decimal RoundOff { get; set; }

    public decimal GrandTotal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? IssuedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    /// <summary>Total tax across every component. Not persisted.</summary>
    public decimal TotalTax => Cgst + Sgst + Igst + Cess;
}
