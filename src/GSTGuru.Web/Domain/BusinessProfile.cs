namespace GSTGuru.Web.Domain;

/// <summary>
/// The registered business that issues invoices. A single row is expected,
/// but it is modelled as an entity so the app stays multi-tenant friendly.
/// </summary>
public class BusinessProfile
{
    public int Id { get; set; }

    public string LegalName { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    public string? Gstin { get; set; }

    /// <summary>Two-digit GST state code of the place of business.</summary>
    public string StateCode { get; set; } = string.Empty;

    public string? StateName { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    /// <summary>Prefix used when generating the next invoice number.</summary>
    public string InvoicePrefix { get; set; } = "INV";

    /// <summary>Sequence counter for the next invoice number.</summary>
    public int NextInvoiceNumber { get; set; } = 1;
}
