using GSTGuru.Web.Gst;
using Microsoft.Extensions.Options;

namespace GSTGuru.Web.Features.Chat;

public class AiOptions
{
    public const string SectionName = "Ai";
    public string Provider { get; set; } = "Offline";
    public string Model { get; set; } = "gst-guru-offline";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.2;
}

public interface IChatProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<string> CompleteAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default);
}

public sealed class ChatProviderFactory(IServiceProvider sp, IOptions<AiOptions> options)
{
    public IChatProvider Resolve()
    {
        var o = options.Value;
        var provider = o.Provider?.Trim().ToLowerInvariant();
        return provider switch
        {
            "openai" when !string.IsNullOrWhiteSpace(o.ApiKey) => sp.GetRequiredService<OpenAiChatProvider>(),
            "anthropic" when !string.IsNullOrWhiteSpace(o.ApiKey) => sp.GetRequiredService<AnthropicChatProvider>(),
            "gemini" when !string.IsNullOrWhiteSpace(o.ApiKey) => sp.GetRequiredService<GeminiChatProvider>(),
            _ => sp.GetRequiredService<OfflineChatProvider>(),
        };
    }
}
