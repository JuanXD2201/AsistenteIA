using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

public interface IAiClient
{
    Task<AiResult<string>> SendChatAsync(AiProviderInfo provider, string apiKey, string model, string prompt, int? maxTokens = null, CancellationToken ct = default);

    Task<AiResult<IReadOnlyList<AiModel>>> ListModelsAsync(AiProviderInfo provider, string? apiKey, CancellationToken ct = default);
}
