using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GSTGuru.Web.Gst;
using Microsoft.Extensions.Options;

namespace GSTGuru.Web.Features.Chat;

public sealed record ChatMessageDto(string Role, string Content)
{
    public static ChatMessageDto User(string content) => new("user", content);
    public static ChatMessageDto Assistant(string content) => new("assistant", content);
    public static ChatMessageDto System(string content) => new("system", content);
}

public sealed record ChatRequest
{
    [Required]
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public sealed record ChatResponse(
    string Reply,
    string Provider,
    string Model,
    string? Intent,
    double? Confidence,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> SuggestedQuestions,
    IReadOnlyList<string> Citations,
    string Disclaimer);

public sealed record TaxRateLookupRequest
{
    [Required]
    public string Description { get; set; } = string.Empty;
}

public sealed record TaxRateLookupResponse(
    string Description,
    string? HsnCode,
    decimal GstRate,
    decimal CessRate,
    double Confidence,
    string Reasoning,
    string Provider);
