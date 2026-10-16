using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace GSTGuru.Web.Features.Chat;

public sealed class ChatService(ChatProviderFactory factory, IOptions<AiOptions> options)
{
    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken ct = default)
    {
        var provider = factory.Resolve();
        var systemPrompt = BuildSystemPrompt();

        var messages = new List<ChatMessageDto> { ChatMessageDto.System(systemPrompt) };
        messages.AddRange(request.Messages);

        var raw = await provider.CompleteAsync(messages, ct);
        var parsed = TryParseStructured(raw);

        return new ChatResponse(
            Reply: parsed.Answer ?? raw,
            Provider: provider.Name,
            Model: options.Value.Model,
            Intent: parsed.Intent,
            Confidence: parsed.Confidence,
            KeyPoints: parsed.KeyPoints ?? new List<string>(),
            SuggestedQuestions: parsed.SuggestedQuestions ?? new List<string>(),
            Citations: parsed.Citations ?? new List<string> { "GST Guru offline reference" },
            Disclaimer: parsed.Disclaimer ?? "For educational/demo purposes. Verify with latest CBIC notifications.");
    }

    public async Task<TaxRateLookupResponse> LookupTaxRateAsync(TaxRateLookupRequest request, CancellationToken ct = default)
    {
        var provider = factory.Resolve();
        var systemPrompt = BuildTaxRatePrompt(request.Description);
        var messages = new List<ChatMessageDto>
        {
            ChatMessageDto.System(systemPrompt),
            ChatMessageDto.User($"Return structured JSON for: {request.Description}")
        };

        var raw = await provider.CompleteAsync(messages, ct);
        var node = TryParseNode(raw);

        return new TaxRateLookupResponse(
            Description: request.Description,
            HsnCode: node?["hsnCode"]?.GetValue<string>() ?? node?["hsn_code"]?.GetValue<string>(),
            GstRate: node?["gstRate"]?.GetValue<decimal>() ?? node?["gst_rate"]?.GetValue<decimal>() ?? 0m,
            CessRate: node?["cessRate"]?.GetValue<decimal>() ?? node?["cess_rate"]?.GetValue<decimal>() ?? 0m,
            Confidence: node?["confidence"]?.GetValue<double>() ?? 0.8,
            Reasoning: node?["reasoning"]?.GetValue<string>() ?? "Closest indicative match from catalogue/LLM.",
            Provider: provider.Name);
    }

    private static StructuredResult TryParseStructured(string raw)
    {
        try
        {
            var cleaned = Clean(raw);
            var doc = JsonNode.Parse(cleaned);
            if (doc is null) return new();
            return new StructuredResult
            {
                Answer = doc["answer"]?.GetValue<string>() ?? doc["reply"]?.GetValue<string>(),
                Intent = doc["intent"]?.GetValue<string>(),
                Confidence = doc["confidence"]?.GetValue<double>(),
                KeyPoints = doc["key_points"]?.AsArray()?.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList(),
                SuggestedQuestions = doc["suggested_questions"]?.AsArray()?.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList(),
                Citations = doc["citations"]?.AsArray()?.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList(),
                Disclaimer = doc["disclaimer"]?.GetValue<string>()
            };
        }
        catch
        {
            return new();
        }
    }

    private static JsonNode? TryParseNode(string raw)
    {
        try
        {
            var cleaned = Clean(raw);
            return JsonNode.Parse(cleaned);
        }
        catch
        {
            return null;
        }
    }

    private static string Clean(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("```"))
        {
            var first = s.IndexOf('\n');
            var last = s.LastIndexOf("```");
            if (first >= 0 && last > first)
            {
                s = s[(first + 1)..last].Trim();
            }
        }
        return s;
    }

    private static string BuildSystemPrompt()
    {
        return "You are GST Guru, an AI assistant helping with Indian GST concepts and tax calculations. " +
               "Respond in clear, structured, JSON when possible with keys: answer, key_points (array), suggested_questions (array), citations (array), disclaimer, intent, confidence (0-1). " +
               "Use only verifiably indicative information. Do not give unqualified legal advice. " +
               "Be precise about: CGST/SGST vs IGST (intra/inter state), slabs (0,5,12,18,28), cess where applicable, HSN/SAC, GSTR basics, ITC basics, thresholds, e-invoicing basics. " +
               "When asked for a calculation, show breakdown clearly (taxable, tax components, total). " +
               "If uncertain about current notification details, state it and give indicative values.";
    }

    private static string BuildTaxRatePrompt(string description)
    {
        return "You are an HSN/SAC classifier for Indian GST. Given an item/service description, return STRICT JSON with keys: hsnCode (string or null), gstRate (number, e.g. 18), cessRate (number, e.g. 0), confidence (0-1), reasoning (short). " +
               "Pick the closest match from common slabs and typical HSNs. Rates must be one of 0,0.25,3,5,12,18,28. " +
               $"Description: {description}. " +
               "If unsure, lower confidence and suggest verification.";
    }

    private sealed record StructuredResult
    {
        public string? Answer { get; init; }
        public string? Intent { get; init; }
        public double? Confidence { get; init; }
        public List<string>? KeyPoints { get; init; }
        public List<string>? SuggestedQuestions { get; init; }
        public List<string>? Citations { get; init; }
        public string? Disclaimer { get; init; }
    }
}
