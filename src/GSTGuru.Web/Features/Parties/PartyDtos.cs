using System.ComponentModel.DataAnnotations;
using GSTGuru.Web.Domain;

namespace GSTGuru.Web.Features.Parties;

public sealed record PartyDto(
    int Id,
    string Name,
    string? Gstin,
    PartyType PartyType,
    string StateCode,
    string? StateName,
    string? Address,
    string? Email,
    string? Phone,
    bool IsActive,
    bool IsRegistered)
{
    public static PartyDto FromEntity(Party p) => new(
        p.Id, p.Name, p.Gstin, p.PartyType, p.StateCode, p.StateName,
        p.Address, p.Email, p.Phone, p.IsActive, p.IsRegistered);
}

/// <summary>
/// Validates a 15-character GSTIN: 2 digit state code, 10 character PAN,
/// entity number, a literal 'Z' and a checksum character.
/// </summary>
public sealed class GstinAttribute : ValidationAttribute
{
    private const string Pattern = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // Optional for B2C parties.
        }

        var gstin = value.ToString()!.Trim().ToUpperInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(gstin, Pattern)
            ? ValidationResult.Success
            : new ValidationResult("GSTIN must be a valid 15-character value.");
    }
}

public sealed class PartyInput
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Gstin]
    public string? Gstin { get; set; }

    public PartyType PartyType { get; set; } = PartyType.Customer;

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

    public bool IsActive { get; set; } = true;
}
