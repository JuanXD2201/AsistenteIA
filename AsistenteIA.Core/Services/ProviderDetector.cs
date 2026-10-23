using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

/// <summary>
/// Detección local por prefijo conocido. Nunca envía la clave a ningún servicio.
/// Si no hay señal fiable devuelve null y la UI pide al usuario elegir el proveedor.
/// </summary>
public static class ProviderDetector
{
    public static AiProviderInfo? Detect(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var key = apiKey.Trim();

        // Claves de Google AI Studio: "AIza" + 35 caracteres.
        if (key.StartsWith("AIza", StringComparison.Ordinal) && key.Length == 39)
            return ProviderCatalog.Gemini;

        // Formato "AQ." observado en una clave real de Google AI Studio aceptada por el endpoint de Gemini.
        if (key.StartsWith("AQ.", StringComparison.Ordinal) && key.Length >= 30)
            return ProviderCatalog.Gemini;

        // Claves de Anthropic usan "sk-ant-": no hay adaptador, no se confunde con OpenAI.
        if (key.StartsWith("sk-ant-", StringComparison.Ordinal)) return null;

        // OpenAI: "sk-" / "sk-proj-". Otros servicios también usan "sk-", pero el prefijo
        // es la única señal local; el usuario puede corregirlo con el selector.
        if (key.StartsWith("sk-", StringComparison.Ordinal) && key.Length >= 20)
            return ProviderCatalog.OpenAi;

        // Pollinations y el resto: formato no distinguible de forma fiable.
        return null;
    }
}
