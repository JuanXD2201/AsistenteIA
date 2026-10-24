using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

public static class ProviderCatalog
{
    public static readonly AiProviderInfo Pollinations = new(
        "pollinations", "Pollinations", "https://gen.pollinations.ai/v1/", "openai",
        "https://enter.pollinations.ai/", ModelsRequireAuth: false);

    public static readonly AiProviderInfo Gemini = new(
        "gemini", "Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai/",
        "gemini-3.1-flash-lite", "https://aistudio.google.com/apikey", ModelsRequireAuth: true);

    public static IReadOnlyList<AiProviderInfo> All { get; } = new[] { Gemini, Pollinations };

    public static AiProviderInfo? FindById(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
