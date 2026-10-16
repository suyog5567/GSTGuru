using System.Globalization;
using System.Text;

namespace GSTGuru.Web.Common;

/// <summary>Minimal RFC 4180 style CSV writer used for report exports.</summary>
public static class Csv
{
    public static string Write(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(Escape)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(Escape)));
        }

        return builder.ToString();
    }

    private static string Escape(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
            double d => d.ToString("0.00", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}
