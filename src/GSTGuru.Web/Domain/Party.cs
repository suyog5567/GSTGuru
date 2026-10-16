namespace GSTGuru.Web.Domain;

/// <summary>
/// A customer or supplier the business trades with. <see cref="Gstin"/> is null
/// for unregistered (B2C) parties, and <see cref="StateCode"/> drives whether a
/// supply is treated as intra-state or inter-state.
/// </summary>
public class Party
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>15-character GSTIN, or null for an unregistered party.</summary>
    public string? Gstin { get; set; }

    public PartyType PartyType { get; set; } = PartyType.Customer;

    /// <summary>Two-digit GST state code, e.g. "27" for Maharashtra.</summary>
    public string StateCode { get; set; } = string.Empty;

    public string? StateName { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True when the party has a GSTIN (B2B rather than B2C).</summary>
    public bool IsRegistered => !string.IsNullOrWhiteSpace(Gstin);
}
