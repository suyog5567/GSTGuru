using System.ComponentModel.DataAnnotations;
using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Features.Business;

public sealed record BusinessProfileDto(
    int Id,
    string LegalName,
    string? TradeName,
    string? Gstin,
    string StateCode,
    string? StateName,
    string? Address,
    string? Email,
    string? Phone,
    string InvoicePrefix,
    int NextInvoiceNumber)
{
    public static BusinessProfileDto FromEntity(BusinessProfile p) => new(
        p.Id, p.LegalName, p.TradeName, p.Gstin, p.StateCode, p.StateName,
        p.Address, p.Email, p.Phone, p.InvoicePrefix, p.NextInvoiceNumber);
}

public sealed class BusinessProfileInput
{
    [Required, MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TradeName { get; set; }

    [MaxLength(15)]
    public string? Gstin { get; set; }

    [Required, StringLength(2, MinimumLength = 2)]
    public string StateCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? StateName { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required, MaxLength(20)]
    public string InvoicePrefix { get; set; } = "INV";

    [Range(1, 999999)]
    public int NextInvoiceNumber { get; set; } = 1;
}
