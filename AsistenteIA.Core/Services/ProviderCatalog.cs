using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

public static class ProviderCatalog
{
    public static readonly AiProviderInfo Pollinations = new(
        "pollinations", "Pollinations", "https://gen.pollinations.ai/v1/", "openai",
        "https://enter.pollinations.ai/", ModelsRequireAuth: false);

    public static readonly AiProviderInfo OpenAi = new(
        "openai", "OpenAI", "https://api.openai.com/v1/", "gpt-4o-mini",
        "https://platform.openai.com/api-keys", ModelsRequireAuth: true);

    public static readonly AiProviderInfo Gemini = new(
        "gemini", "Google Gemini (endpoint compatible OpenAI)", "https://generativelanguage.googleapis.com/v1beta/openai/",
        "gemini-3.1-flash-lite", "https://aistudio.google.com/apikey", ModelsRequireAuth: true);

    public static IReadOnlyList<AiProviderInfo> All { get; } = new[] { Pollinations, OpenAi, Gemini };

    public static AiProviderInfo? FindById(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
