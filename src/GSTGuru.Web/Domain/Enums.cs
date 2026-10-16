namespace GSTGuru.Web.Domain;

/// <summary>Whether a party buys from or sells to the business.</summary>
public enum PartyType
{
    Customer = 1,
    Supplier = 2,
}

/// <summary>Lifecycle of a tax invoice.</summary>
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Cancelled = 3,
}

/// <summary>
/// How a supply is taxed. Intra-state supplies attract CGST + SGST,
/// inter-state supplies (and exports) attract IGST.
/// </summary>
public enum SupplyType
{
    IntraState = 1,
    InterState = 2,
}

/// <summary>Reporting bucket used by GSTR-3B Table 3.1.</summary>
public enum SupplyCategory
{
    /// <summary>Taxable outward supply (3.1a).</summary>
    Taxable = 1,

    /// <summary>Zero-rated supply such as exports / SEZ (3.1b).</summary>
    ZeroRated = 2,

    /// <summary>Nil-rated supply (3.1c).</summary>
    NilRated = 3,

    /// <summary>Exempt supply (3.1c).</summary>
    Exempt = 4,
}
