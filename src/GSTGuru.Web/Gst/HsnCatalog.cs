namespace GSTGuru.Web.Gst;

/// <summary>An HSN/SAC code entry with an indicative GST and cess rate.</summary>
public sealed record HsnEntry(
    string HsnCode,
    string Description,
    string Category,
    decimal GstRate,
    decimal CessRate = 0m);

/// <summary>
/// A small, indicative catalogue of HSN/SAC codes used to pre-fill tax rates,
/// seed demo data and power the offline fallback of the AI assistant.
/// Real rates should always be confirmed against the latest CBIC notifications.
/// </summary>
public static class HsnCatalog
{
    public static IReadOnlyList<HsnEntry> Entries { get; } = new List<HsnEntry>
    {
        new("0401", "Fresh milk and pasteurised milk", "Dairy", 0m),
        new("0402", "Milk powder and condensed milk", "Dairy", 5m),
        new("0405", "Butter, ghee and other fats from milk", "Dairy", 12m),
        new("0406", "Cheese and curd", "Dairy", 12m),
        new("0902", "Tea, whether or not flavoured", "Beverages", 5m),
        new("0901", "Coffee, roasted or unroasted", "Beverages", 5m),
        new("1006", "Rice (branded and packaged)", "Cereals", 5m),
        new("1101", "Wheat or meslin flour", "Cereals", 0m),
        new("1701", "Cane or beet sugar", "Food", 5m),
        new("1704", "Sugar confectionery (toffees, candies)", "Food", 18m),
        new("1806", "Chocolate and cocoa preparations", "Food", 18m),
        new("1905", "Bread, pastry, biscuits and cakes", "Food", 18m),
        new("2106", "Food preparations not elsewhere specified", "Food", 18m),
        new("2201", "Drinking water packed in bottles", "Beverages", 18m),
        new("2202", "Aerated waters with added sugar", "Beverages", 28m, 12m),
        new("2402", "Cigarettes and tobacco products", "Tobacco", 28m, 36m),
        new("2523", "Portland cement, aluminous cement", "Construction", 28m),
        new("2710", "Petroleum oils and lubricating preparations", "Fuel", 18m),
        new("3004", "Medicaments for retail sale", "Pharma", 12m),
        new("3005", "Wadding, gauze and bandages", "Pharma", 12m),
        new("3401", "Soap and organic surface-active products", "Personal care", 18m),
        new("3304", "Beauty and skin-care preparations", "Personal care", 18m),
        new("3402", "Detergents and cleaning preparations", "Household", 18m),
        new("3923", "Plastic articles for packing of goods", "Packaging", 18m),
        new("4202", "Trunks, suitcases, handbags", "Luggage", 18m),
        new("4901", "Printed books, brochures and leaflets", "Publishing", 0m),
        new("5208", "Woven fabrics of cotton", "Textiles", 5m),
        new("6109", "T-shirts, singlets and vests (knitted)", "Apparel", 5m),
        new("6203", "Men's suits, trousers and shorts", "Apparel", 12m),
        new("6403", "Footwear with leather uppers", "Footwear", 18m),
        new("7213", "Bars and rods of iron or steel", "Metals", 18m),
        new("7308", "Structures and parts of structures of iron/steel", "Metals", 18m),
        new("7615", "Aluminium kitchenware and tableware", "Household", 18m),
        new("8471", "Computers, laptops and data-processing machines", "Electronics", 18m),
        new("8517", "Telephones, smartphones and routers", "Electronics", 18m),
        new("8518", "Wireless earbuds, headphones, earphones and speakers", "Electronics", 18m),
        new("8528", "Monitors and television receivers", "Electronics", 18m),
        new("8504", "Electrical transformers and static converters", "Electronics", 18m),
        new("8703", "Motor cars and vehicles for transport of persons", "Automobile", 28m, 15m),
        new("8711", "Motorcycles and mopeds", "Automobile", 28m),
        new("8712", "Bicycles and other cycles", "Automobile", 12m),
        new("9403", "Other furniture and parts thereof", "Furniture", 18m),
        new("9503", "Toys, puzzles and scale models", "Toys", 12m),
        new("9983", "Professional, technical and business services (SAC)", "Services", 18m),
        new("9985", "Supporting services (SAC)", "Services", 18m),
        new("9963", "Restaurant and food and beverage serving services (SAC)", "Services", 5m),
        new("9972", "Real estate services (SAC)", "Services", 18m),
        new("9983-A", "Information technology software services (SAC)", "Services", 18m),
    };

    /// <summary>Full-text-ish search across code, description and category.</summary>
    public static IReadOnlyList<HsnEntry> Search(string? term, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Entries.Take(limit).ToList();
        }

        var needle = term.Trim();
        return Entries
            .Where(e => e.HsnCode.Contains(needle, StringComparison.OrdinalIgnoreCase)
                        || e.Description.Contains(needle, StringComparison.OrdinalIgnoreCase)
                        || e.Category.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Common words that carry no product meaning. Ignoring them stops questions
    /// like "what is the GST rate for X" from matching on "the"/"for"/"rate".
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "which", "who", "when", "where", "how", "why", "is", "are", "the", "a", "an",
        "for", "of", "on", "in", "to", "and", "or", "gst", "rate", "rates", "tax", "taxes",
        "apply", "applies", "applicable", "under", "calculate", "calculation", "calc", "how",
        "much", "please", "tell", "me", "about", "price", "prices", "cost", "hsn", "sac",
        "code", "good", "goods", "item", "items", "service", "services", "sale", "selling",
        "buy", "buying", "on", "at", "with", "give", "find", "lookup", "search",
    };

    /// <summary>Naive singularisation so "laptops" matches "laptop" and vice versa.</summary>
    private static string Stem(string word)
    {
        if (word.Length >= 4 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
        {
            return word[..^1];
        }
        return word;
    }

    private static HashSet<string> Tokenize(string text) =>
        text.Split(new[] { ' ', ',', '.', ';', ':', '/', '(', ')', '-', '&' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Stem)
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Best-effort lookup of a single entry from a free-text description.</summary>
    public static HsnEntry? Guess(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var queryWords = Tokenize(description);
        if (queryWords.Count == 0)
        {
            return null;
        }

        HsnEntry? best = null;
        var bestScore = 0;

        foreach (var entry in Entries)
        {
            var entryWords = Tokenize(entry.Description + " " + entry.Category);
            var score = queryWords.Count(entryWords.Contains);

            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return bestScore > 0 ? best : null;
    }
}
