using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Gst;

/// <summary>Applies GST rules to invoice lines.</summary>
public interface IGstCalculator
{
    /// <summary>Computes the tax split for a single line.</summary>
    GstLineResult CalculateLine(GstLineInput input, SupplyType supplyType);

    /// <summary>Computes totals for a set of lines plus the rupee round-off.</summary>
    GstBreakdown Calculate(IEnumerable<GstLineInput> lines, SupplyType supplyType);

    /// <summary>
    /// Computes totals, honouring the reporting category. Non-taxable
    /// categories (zero-rated, nil-rated, exempt) carry no tax.
    /// </summary>
    GstBreakdown Calculate(IEnumerable<GstLineInput> lines, SupplyType supplyType, SupplyCategory category);

    /// <summary>
    /// Determines whether a supply is intra-state (CGST + SGST) or inter-state
    /// (IGST) from the seller and place-of-supply state codes.
    /// </summary>
    SupplyType ResolveSupplyType(string sellerStateCode, string placeOfSupplyStateCode);
}
