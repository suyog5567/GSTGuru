using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using GSTGuru.Web.Gst;
using Microsoft.Extensions.Options;

namespace GSTGuru.Web.Features.Chat;

public sealed class OfflineChatProvider(IOptions<AiOptions> options) : IChatProvider
{
    public string Name => "Offline (rule-based)";
    public bool IsConfigured => true;

    public Task<string> CompleteAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default)
    {
        var user = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;

        // The classifier endpoint sends a different system prompt; answer it with
        // a structured HSN/rate payload instead of a conversational reply.
        var system = messages.FirstOrDefault(m => m.Role == "system")?.Content ?? string.Empty;
        if (system.Contains("HSN/SAC classifier", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(BuildTaxRateJson(ExtractClassificationTarget(user, system)));
        }

        var answer = GenerateResponse(user);

        var json = JsonSerializer.Serialize(new
        {
            answer,
            key_points = new[]
            {
                "Answers use a curated GST knowledge base and the built-in GST calculator.",
                "Demonstrates prompt engineering concepts: intent detection + retrieval.",
                "Designed to show LLM-style flow even without external API keys."
            },
            suggested_questions = GetSuggestions(user),
            citations = new[]
            {
                "GST Council / CBIC (indicative summary)",
                "GST Act, 2017 (core provisions, indicative)",
                "CBIC HSN notifications (for indicative rates)"
            },
            disclaimer = "For educational/demo purposes. Consult a qualified chartered accountant for compliance.",
            provider = Name,
            model = options.Value.Model,
            intent = DetectIntent(user),
            confidence = 0.9
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return Task.FromResult(json);
    }

    private static string ExtractClassificationTarget(string user, string system)
    {
        const string userMarker = "for:";
        var idx = user.IndexOf(userMarker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            return user[(idx + userMarker.Length)..].Trim();
        }

        const string sysMarker = "Description:";
        var sysIdx = system.IndexOf(sysMarker, StringComparison.OrdinalIgnoreCase);
        if (sysIdx >= 0)
        {
            var text = system[(sysIdx + sysMarker.Length)..].Trim();
            var stop = text.IndexOf('.', StringComparison.Ordinal);
            return stop > 0 ? text[..stop].Trim() : text;
        }

        return user.Trim();
    }

    private static string BuildTaxRateJson(string description)
    {
        var entry = HsnCatalog.Guess(description);
        var payload = entry is null
            ? new
            {
                hsnCode = (string?)null,
                gstRate = 18m,
                cessRate = 0m,
                confidence = 0.4,
                reasoning = "No confident catalogue match; 18% is the most common slab. Verify against the latest CBIC notification."
            }
            : new
            {
                hsnCode = (string?)entry.HsnCode,
                gstRate = entry.GstRate,
                cessRate = entry.CessRate,
                confidence = 0.85,
                reasoning = $"Closest catalogue match: {entry.Description} ({entry.Category})."
            };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string DetectIntent(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("gst") && (t.Contains("%") || t.Contains("rate") || t.Contains("hsn") || t.Contains("slab")))
            return "rate_lookup";
        if (t.Contains("calculate") || t.Contains("compute") || t.Contains("tax") && (t.Contains("amount") || t.Contains("value")))
            return "calculate";
        if (t.Contains("gstr") || t.Contains("return") || t.Contains("filing"))
            return "returns";
        if (t.Contains("register") || t.Contains("threshold") || t.Contains("turnover"))
            return "registration";
        if (t.Contains("input tax credit") || t.Contains("itc"))
            return "itc";
        if (t.Contains("e-invoic") || t.Contains("eway"))
            return "compliance";
        if (t.Contains("explain invoice") || t.Contains("invoice"))
            return "invoicing";
        if (t.Contains("hello") || t.Contains("hi") || t.Contains("help"))
            return "greeting";
        return "general";
    }

    private string GenerateResponse(string text)
    {
        var t = text.ToLowerInvariant();

        // Calculation example: "gst on 5000 at 18%"
        var percent = ExtractPercent(t);
        var amount = ExtractAmount(t);
        if (amount > 0 && percent >= 0)
        {
            var taxable = amount;
            var cgst = GstCalculator.Round2(taxable * (percent / 2m) / 100m);
            var sgst = GstCalculator.Round2(taxable * (percent / 2m) / 100m);
            var igst = GstCalculator.Round2(taxable * percent / 100m);
            var isInter = t.Contains("igst") || t.Contains("inter");
            var tax = isInter ? igst : cgst + sgst;
            var total = GstCalculator.Round2(taxable + tax);
            return $"Quick calculation (demo):\n" +
                   $"- Amount: ₹{taxable:0.00}\n" +
                   $"- GST rate: {percent}% {(isInter ? "(IGST - inter-state)" : "(CGST+SGST - intra-state)")}\n" +
                   $"- Tax: ₹{tax:0.00} ({(isInter ? $"IGST ₹{igst:0.00}" : $"CGST ₹{cgst:0.00} + SGST ₹{sgst:0.00}")})\n" +
                   $"- Total: ₹{total:0.00}\n\n" +
                   $"Note: This is a simplified calculation for educational purposes.";
        }

        if (t.Contains("hsn") || t.Contains("rate"))
        {
            var entry = HsnCatalog.Guess(text);
            if (entry is not null)
            {
                return $"Based on your description, here is a close indicative match:\n\n" +
                       $"- HSN/SAC: {entry.HsnCode}\n" +
                       $"- Description: {entry.Description}\n" +
                       $"- Category: {entry.Category}\n" +
                       $"- Indicative GST rate: {entry.GstRate}%\n" +
                       (entry.CessRate > 0 ? $"- Indicative compensation cess: {entry.CessRate}%\n" : string.Empty) +
                       $"\nAlways verify the current rate against the latest CBIC HSN notification.";
            }
            return "Please share the item description or HSN code. I can suggest an indicative GST slab.";
        }

        if (t.Contains("threshold") || t.Contains("turnover"))
        {
            return "GST registration thresholds (indicative):\n" +
                   "- Goods: ₹40 lakhs (₹20 lakhs for special category states)\n" +
                   "- Services: ₹20 lakhs (₹10 lakhs for special category states)\n" +
                   "- Composition scheme: up to ₹1.5 crore (conditions apply)\n" +
                   "\nCheck the latest thresholds applicable to your business.";
        }

        if (t.Contains("itc") || t.Contains("input tax credit"))
        {
            return "Input Tax Credit (ITC) basics:\n" +
                   "- Available on goods/services used for business purposes\n" +
                   "- Must have valid tax invoice\n" +
                   "- Must be reflected in GSTR-2B/related filings\n" +
                   "- Certain items blocked (e.g. motor vehicles, food, gifts under specific cases)\n" +
                   "- ITC claimed within prescribed time limits\n\nThis is a summary; refer to Section 16/17 of GST Act.";
        }

        if (t.Contains("gstr"))
        {
            return "Common GST returns (indicative):\n" +
                   "- GSTR-1: Outward supplies (monthly/QRMP)\n" +
                   "- GSTR-3B: Summary return for payment of tax (monthly/QRMP)\n" +
                   "- GSTR-2B: Auto-drafted ITC statement\n" +
                   "- GSTR-4: Composition scheme\n" +
                   "- GSTR-9: Annual return\n" +
                   "- GSTR-1 IFF: Optional for QRMP taxpayers\n" +
                   "\nFiling frequencies depend on turnover and scheme.";
        }

        if (t.Contains("e-invoice") || t.Contains("einvoice"))
        {
            return "E-invoicing (indicative):\n" +
                   "- Mandatory for notified taxpayers (thresholds revised periodically)\n" +
                   "- Requires IRN generation via Invoice Registration Portal (IRP)\n" +
                   "- QR code and IRN must be on invoice\n" +
                   "- Affects B2B invoices in general; exemptions apply\n\nAlways check current notification.";
        }

        if (t.Contains("intra") && t.Contains("inter"))
        {
            return "Place of supply determines tax type:\n" +
                   "- Intra-state (same state): CGST + SGST\n" +
                   "- Inter-state (different states/UTs): IGST\n" +
                   "- Exports: Zero-rated (generally IGST 0 with LUT)\n" +
                   "- Imports: IGST + customs duty";
        }

        if (t.Contains("hsn code"))
        {
            return "HSN (Harmonised System of Nomenclature):\n" +
                   "- 2-digit chapter, 4-digit heading, 6-digit sub-heading standard\n" +
                   "- Used to classify goods and determine GST rate\n" +
                   "- SAC used for services\n" +
                   "- Correct HSN prevents mismatches and scrutiny\n\nI can guess HSN if you describe the item.";
        }

        if (t.Contains("hello") || t.Contains("hi") || t.Contains("help") || t.Contains("who"))
        {
            return "Hi! I'm GST Guru AI Assistant.\n\n" +
                   "I can help with:\n" +
                   "- GST rate lookups and HSN suggestions\n" +
                   "- Quick tax calculations (CGST/SGST/IGST)\n" +
                   "- Registration thresholds, ITC, returns (GSTR), e-invoicing\n" +
                   "- Place of supply, invoicing basics\n\n" +
                   "Just ask a question. For rate-specific answers, share the product/service description.";
        }

        return "I can explain GST concepts and do quick calculations.\n\n" +
               "Try asking:\n" +
               "- \"GST on ₹5000 at 18% intra-state\"\n" +
               "- \"HSN for a laptop\"\n" +
               "- \"ITC eligibility basics\"\n" +
               "- \"Registration threshold for services\"\n" +
               "- \"Difference between intra and inter state GST\"\n\n" +
               "What would you like to know?";
    }

    private static string[] GetSuggestions(string user)
    {
        var t = user.ToLowerInvariant();
        if (t.Contains("hsn") || t.Contains("laptop") || t.Contains("mobile"))
            return new[] { "GST rate for computers", "HSN for smartphones", "Difference between HSN and SAC" };
        if (t.Contains("gst") && t.Contains("calculate"))
            return new[] { "GST on ₹10000 at 12%", "CGST vs SGST vs IGST", "Round off in GST invoices" };
        if (t.Contains("gstr"))
            return new[] { "GSTR-1 due dates", "QRMP scheme explained", "Difference GSTR-1 vs GSTR-3B" };
        return new[] { "GST slabs 2026", "E-invoicing threshold", "Input Tax Credit conditions", "Place of supply rules" };
    }

    private static decimal ExtractAmount(string t)
    {
        // Match ₹5000, Rs 5000, 5000, 5,000
        var patterns = new[]
        {
            @"₹\s*(\d+(?:,\d{3})*(?:\.\d+)?)",
            @"rs\.?\s*(\d+(?:,\d{3})*(?:\.\d+)?)",
            @"amount\s*(\d+(?:,\d{3})*(?:\.\d+)?)",
            @"value\s*(\d+(?:,\d{3})*(?:\.\d+)?)",
            @"(\d+(?:,\d{3})+(?:\.\d+)?)",
            @"(\d+\.\d+)",
            @"(\d{4,})"  // larger numbers likely amount
        };
        foreach (var p in patterns)
        {
            var m = System.Text.RegularExpressions.Regex.Match(t, p);
            if (m.Success)
            {
                var val = m.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    if (d > 0) return d;
                }
            }
        }
        return 0m;
    }

    private static decimal ExtractPercent(string t)
    {
        var m = System.Text.RegularExpressions.Regex.Match(t, @"(\d{1,3}(?:\.\d+)?)\s*%");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p))
        {
            return Math.Min(100, Math.Max(0, p));
        }
        return 18m; // default
    }
}

public sealed class OpenAiChatProvider(HttpClient http, IOptions<AiOptions> options) : IChatProvider
{
    public string Name => "OpenAI";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default)
    {
        var o = options.Value;
        var baseUrl = string.IsNullOrWhiteSpace(o.BaseUrl) ? "https://api.openai.com/v1" : o.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(o.Model) ? "gpt-4o-mini" : o.Model;

        var body = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = o.Temperature,
            max_tokens = o.MaxTokens,
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        var content = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenAI API error {(int)res.StatusCode}: {content}");
        }

        var doc = JsonNode.Parse(content);
        var reply = doc?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";
        return reply;
    }
}

public sealed class AnthropicChatProvider(HttpClient http, IOptions<AiOptions> options) : IChatProvider
{
    public string Name => "Anthropic";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default)
    {
        var o = options.Value;
        var baseUrl = string.IsNullOrWhiteSpace(o.BaseUrl) ? "https://api.anthropic.com" : o.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(o.Model) ? "claude-3-5-haiku-latest" : o.Model;

        var systemMsg = messages.FirstOrDefault(m => m.Role == "system")?.Content;
        var userMsgs = messages.Where(m => m.Role != "system").Select(m => new { role = m.Role, content = m.Content }).ToList();

        var body = new
        {
            model,
            max_tokens = o.MaxTokens,
            temperature = o.Temperature,
            system = systemMsg,
            messages = userMsgs,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
        req.Headers.Add("x-api-key", o.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        var content = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Anthropic API error {(int)res.StatusCode}: {content}");
        }

        var doc = JsonNode.Parse(content);
        var text = doc?["content"]?[0]?["text"]?.ToString() ?? "{}";
        return text;
    }
}

public sealed class GeminiChatProvider(HttpClient http, IOptions<AiOptions> options) : IChatProvider
{
    public string Name => "Gemini";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default)
    {
        var o = options.Value;
        var baseUrl = string.IsNullOrWhiteSpace(o.BaseUrl) ? "https://generativelanguage.googleapis.com" : o.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(o.Model) ? "gemini-2.0-flash" : o.Model;

        var system = messages.FirstOrDefault(m => m.Role == "system")?.Content;
        var contents = messages
            .Where(m => m.Role != "system")
            .Select(m => new { role = m.Role == "assistant" ? "model" : "user", parts = new[] { new { text = m.Content } } })
            .ToList();

        var body = new
        {
            contents,
            systemInstruction = system is null ? null : new { parts = new[] { new { text = system } } },
            generationConfig = new { temperature = o.Temperature, maxOutputTokens = o.MaxTokens }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(o.ApiKey!)}");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        var content = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API error {(int)res.StatusCode}: {content}");
        }

        var doc = JsonNode.Parse(content);
        var reply = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "{}";
        return reply;
    }
}
