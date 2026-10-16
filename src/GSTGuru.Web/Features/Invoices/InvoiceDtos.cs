using System.ComponentModel.DataAnnotations;
using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Features.Invoices;

public sealed record InvoiceLineDto(
    int Id,
    int? ProductId,
    string Description,
    string? HsnCode,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxableValue,
    decimal GstRate,
    decimal CessRate,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    decimal Cess,
    decimal LineTotal);

public sealed class InvoiceLineInput
{
    public int? ProductId { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(12)]
    public string? HsnCode { get; set; }

    [MaxLength(10)]
    public string Unit { get; set; } = "NOS";

    [Range(0.001, 1000000)]
    public decimal Quantity { get; set; }

    [Range(0, 100000000)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [Range(0, 100)]
    public decimal GstRate { get; set; }

    [Range(0, 100)]
    public decimal CessRate { get; set; }
}

public sealed record InvoiceDto(
    int Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    int PartyId,
    string PartyName,
    string PartyGstin,
    string PlaceOfSupplyStateCode,
    string SellerStateCode,
    SupplyType SupplyType,
    SupplyCategory SupplyCategory,
    bool ReverseCharge,
    InvoiceStatus Status,
    string? Notes,
    decimal TaxableValue,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    decimal Cess,
    decimal RoundOff,
    decimal GrandTotal,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    IReadOnlyList<InvoiceLineDto> Lines);

public sealed class InvoiceCreateRequest
{
    [Required]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public int PartyId { get; set; }

    /// <summary>Optional; falls back to the party's state when omitted.</summary>
    [StringLength(2, MinimumLength = 2)]
    public string? PlaceOfSupplyStateCode { get; set; }

    /// <summary>Optional; falls back to the business profile's state when omitted.</summary>
    [StringLength(2, MinimumLength = 2)]
    public string? SellerStateCode { get; set; }

    public SupplyType SupplyType { get; set; }

    public SupplyCategory SupplyCategory { get; set; } = SupplyCategory.Taxable;

    public bool ReverseCharge { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required, MinLength(1)]
    public List<InvoiceLineInput> Lines { get; set; } = new();
}
