namespace GSTGuru.Web.Gst;

/// <summary>Standard GST rate slabs in India.</summary>
public static class GstRates
{
    public static IReadOnlyList<decimal> Slabs { get; } = new[]
    {
        0m, 0.25m, 3m, 5m, 12m, 18m, 28m,
    };

    public static bool IsValidSlab(decimal rate) => Slabs.Contains(rate);
}

/// <summary>Two-digit GST state codes and their names.</summary>
public sealed record StateCode(string Code, string Name);

/// <summary>Reference data for Indian states and union territories.</summary>
public static class StateCodes
{
    public static IReadOnlyList<StateCode> All { get; } = new List<StateCode>
    {
        new("01", "Jammu and Kashmir"),
        new("02", "Himachal Pradesh"),
        new("03", "Punjab"),
        new("04", "Chandigarh"),
        new("05", "Uttarakhand"),
        new("06", "Haryana"),
        new("07", "Delhi"),
        new("08", "Rajasthan"),
        new("09", "Uttar Pradesh"),
        new("10", "Bihar"),
        new("11", "Sikkim"),
        new("12", "Arunachal Pradesh"),
        new("13", "Nagaland"),
        new("14", "Manipur"),
        new("15", "Mizoram"),
        new("16", "Tripura"),
        new("17", "Meghalaya"),
        new("18", "Assam"),
        new("19", "West Bengal"),
        new("20", "Jharkhand"),
        new("21", "Odisha"),
        new("22", "Chhattisgarh"),
        new("23", "Madhya Pradesh"),
        new("24", "Gujarat"),
        new("26", "Dadra and Nagar Haveli and Daman and Diu"),
        new("27", "Maharashtra"),
        new("29", "Karnataka"),
        new("30", "Goa"),
        new("31", "Lakshadweep"),
        new("32", "Kerala"),
        new("33", "Tamil Nadu"),
        new("34", "Puducherry"),
        new("35", "Andaman and Nicobar Islands"),
        new("36", "Telangana"),
        new("37", "Andhra Pradesh"),
        new("38", "Ladakh"),
        new("97", "Other Territory"),
    };

    public static string? NameFor(string? code)
        => All.FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;
}
