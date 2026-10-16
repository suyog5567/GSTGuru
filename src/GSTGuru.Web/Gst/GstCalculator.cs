using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Gst;

/// <summary>
/// Stateless GST calculation engine.
///
/// Rules implemented:
/// <list type="bullet">
/// <item>Trade discount is deducted before tax is applied.</item>
/// <item>Intra-state supply splits GST equally into CGST and SGST.</item>
/// <item>Inter-state supply levies the full rate as IGST.</item>
/// <item>Compensation cess is charged on the taxable value where applicable.</item>
/// <item>Zero-rated, nil-rated and exempt supplies carry no tax.</item>
/// <item>Tax amounts are rounded to 2 decimals and the invoice total to the nearest rupee.</item>
/// </list>
/// </summary>
public sealed class GstCalculator : IGstCalculator
{
    public SupplyType ResolveSupplyType(string sellerStateCode, string placeOfSupplyStateCode)
    {
        var seller = Normalise(sellerStateCode);
        var place = Normalise(placeOfSupplyStateCode);

        // Fall back to intra-state when the place of supply is unknown so the
        // invoice still balances; the UI always supplies a value in practice.
        if (string.IsNullOrEmpty(seller) || string.IsNullOrEmpty(place))
        {
            return SupplyType.IntraState;
        }

        return string.Equals(seller, place, StringComparison.OrdinalIgnoreCase)
            ? SupplyType.IntraState
            : SupplyType.InterState;
    }

    public GstLineResult CalculateLine(GstLineInput input, SupplyType supplyType)
        => CalculateLine(input, supplyType, isTaxable: true);

    public GstBreakdown Calculate(IEnumerable<GstLineInput> lines, SupplyType supplyType)
        => Calculate(lines, supplyType, SupplyCategory.Taxable);

    public GstBreakdown Calculate(
        IEnumerable<GstLineInput> lines,
        SupplyType supplyType,
        SupplyCategory category)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var isTaxable = category is SupplyCategory.Taxable;
        var results = lines
            .Select(line => CalculateLine(line, supplyType, isTaxable))
            .ToList();

        var taxableValue = Round2(results.Sum(r => r.TaxableValue));
        var cgst = Round2(results.Sum(r => r.Cgst));
        var sgst = Round2(results.Sum(r => r.Sgst));
        var igst = Round2(results.Sum(r => r.Igst));
        var cess = Round2(results.Sum(r => r.Cess));
        var subTotal = Round2(results.Sum(r => r.LineTotal));

        // Indian invoices are rounded to the nearest whole rupee.
        var grandTotal = Math.Round(subTotal, 0, MidpointRounding.AwayFromZero);
        var roundOff = Round2(grandTotal - subTotal);

        return new GstBreakdown
        {
            SupplyType = supplyType,
            TaxableValue = taxableValue,
            Cgst = cgst,
            Sgst = sgst,
            Igst = igst,
            Cess = cess,
            TotalTax = Round2(cgst + sgst + igst + cess),
            SubTotal = subTotal,
            RoundOff = roundOff,
            GrandTotal = grandTotal,
            Lines = results,
        };
    }

    private static GstLineResult CalculateLine(
        GstLineInput input,
        SupplyType supplyType,
        bool isTaxable)
    {
        ArgumentNullException.ThrowIfNull(input);

        var gross = input.Quantity * input.UnitPrice;
        var discount = gross * (input.DiscountPercent / 100m);
        var taxableValue = Round2(gross - discount);

        var gstRate = isTaxable ? input.GstRate : 0m;
        var cessRate = isTaxable ? input.CessRate : 0m;

        decimal cgst = 0m;
        decimal sgst = 0m;
        decimal igst = 0m;

        if (supplyType is SupplyType.IntraState)
        {
            // GST is split equally between centre and state.
            cgst = Round2(taxableValue * (gstRate / 2m) / 100m);
            sgst = Round2(taxableValue * (gstRate / 2m) / 100m);
        }
        else
        {
            igst = Round2(taxableValue * gstRate / 100m);
        }

        var cess = Round2(taxableValue * cessRate / 100m);
        var lineTotal = Round2(taxableValue + cgst + sgst + igst + cess);

        return new GstLineResult
        {
            Description = input.Description,
            HsnCode = input.HsnCode,
            Unit = input.Unit,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice,
            DiscountPercent = input.DiscountPercent,
            TaxableValue = taxableValue,
            GstRate = gstRate,
            CessRate = cessRate,
            Cgst = cgst,
            Sgst = sgst,
            Igst = igst,
            Cess = cess,
            LineTotal = lineTotal,
        };
    }

    /// <summary>Rounds a monetary amount to two decimals (away from zero).</summary>
    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Normalise(string? code) => (code ?? string.Empty).Trim();
}
