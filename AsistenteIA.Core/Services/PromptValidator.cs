using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

public static class PromptValidator
{
    public const int MaxLength = 4000;

    public static (bool IsValid, AiErrorKind Error, string Message) Validate(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return (false, AiErrorKind.EmptyPrompt, "Escribe una consulta antes de enviar.");
        if (prompt.Trim().Length > MaxLength)
            return (false, AiErrorKind.PromptTooLong, $"La consulta supera {MaxLength} caracteres.");
        return (true, AiErrorKind.None, string.Empty);
    }
}
