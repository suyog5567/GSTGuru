using System.ComponentModel.DataAnnotations;
using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Features.Products;

public sealed record ProductDto(
    int Id,
    string Name,
    string? HsnCode,
    string Unit,
    decimal UnitPrice,
    decimal DefaultGstRate,
    decimal DefaultCessRate,
    bool IsActive)
{
    public static ProductDto FromEntity(Product p) => new(
        p.Id, p.Name, p.HsnCode, p.Unit, p.UnitPrice, p.DefaultGstRate, p.DefaultCessRate, p.IsActive);
}

public sealed class ProductInput
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(12)]
    public string? HsnCode { get; set; }

    [MaxLength(10)]
    public string Unit { get; set; } = "NOS";

    [Range(0, 1_000_000_000)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DefaultGstRate { get; set; }

    [Range(0, 100)]
    public decimal DefaultCessRate { get; set; }

    public bool IsActive { get; set; } = true;
}
