namespace GSTGuru.Web.Domain;

/// <summary>
/// A good or service sold by the business. Carries the default HSN/SAC code
/// and GST rate so invoices can be prepared quickly and consistently.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>HSN code for goods or SAC code for services.</summary>
    public string? HsnCode { get; set; }

    public string Unit { get; set; } = "NOS";

    public decimal UnitPrice { get; set; }

    /// <summary>Default GST rate as a percentage (e.g. 18 for 18%).</summary>
    public decimal DefaultGstRate { get; set; }

    /// <summary>Default compensation cess rate as a percentage.</summary>
    public decimal DefaultCessRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
