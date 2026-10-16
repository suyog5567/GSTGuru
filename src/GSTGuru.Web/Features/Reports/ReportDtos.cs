namespace GSTGuru.Web.Features.Reports;

public sealed record DashboardDto(
    int TotalInvoices,
    decimal TotalTaxableValue,
    decimal TotalTaxCollected,
    decimal TotalGrandValue,
    IReadOnlyList<RecentInvoiceDto> RecentInvoices,
    IReadOnlyList<MonthlyTax> MonthlyTaxTrend,
    IReadOnlyList<TopPartyDto> TopParties,
    DateTime From,
    DateTime To);

public sealed record RecentInvoiceDto(
    string InvoiceNumber,
    DateTime InvoiceDate,
    string PartyName,
    bool IsRegistered,
    decimal GrandTotal,
    GSTGuru.Web.Domain.InvoiceStatus Status);

public sealed record MonthlyTax(string Month, decimal Tax);

public sealed record TopPartyDto(string PartyName, decimal Amount, int InvoiceCount);

public sealed record TaxSummaryDto(
    decimal TaxableValue,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    decimal Cess,
    decimal GrandTotal,
    DateTime From,
    DateTime To);

public sealed record HsnSummaryDto(
    string HsnCode,
    decimal GstRate,
    string Unit,
    decimal Quantity,
    decimal TaxableValue,
    decimal Igst,
    decimal Cgst,
    decimal Sgst,
    decimal Cess,
    decimal LineTotal);

public sealed record Gstr1Dto(
    DateTime From,
    DateTime To,
    IReadOnlyList<B2BRowDto> B2B,
    IReadOnlyList<B2CRowDto> B2C,
    IReadOnlyList<RateWiseDto> RateWise,
    IEnumerable<HsnSummaryDto> HsnSummary,
    DocSummaryDto DocumentSummary);

public sealed record B2BRowDto(
    string InvoiceNumber,
    DateTime InvoiceDate,
    string Gstin,
    string PartyName,
    string PlaceOfSupplyStateCode,
    decimal TaxableValue,
    decimal Igst,
    decimal Cgst,
    decimal Sgst,
    decimal Cess,
    decimal GrandTotal,
    string Category,
    string SupplyType);

public sealed record B2CRowDto(
    string PlaceOfSupplyStateCode,
    string SupplyType,
    decimal TaxableValue,
    decimal Igst,
    decimal CgstSgst,
    decimal Cess,
    decimal GrandTotal);

public sealed record RateWiseDto(
    decimal GstRate,
    decimal TaxableValue,
    decimal Igst,
    decimal Cgst,
    decimal Sgst,
    decimal Cess);

public sealed record DocSummaryDto(int TotalInvoices, decimal TotalValue);

public sealed record Gstr3BDto(
    DateTime From,
    DateTime To,
    decimal Section3_1aTaxable,
    decimal Section3_1bZeroRated,
    decimal Section3_1cNilExempt,
    TaxPayableDto TaxPayable);

public sealed record TaxPayableDto(decimal Cgst, decimal Sgst, decimal Igst, decimal Cess);
