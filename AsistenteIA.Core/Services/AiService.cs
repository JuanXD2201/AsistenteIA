using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

/// <summary>Orquesta validación de prompt, configuración y llamada al proveedor.</summary>
public sealed class AiService
{
    private readonly IAiClient _client;
    private readonly AiConfigService _config;

    public AiService(IAiClient client, AiConfigService config)
    {
        _client = client;
        _config = config;
    }

    public async Task<AiResult<string>> AskAsync(string? prompt, CancellationToken ct = default)
    {
        var (valid, error, message) = PromptValidator.Validate(prompt);
        if (!valid) return AiResult<string>.Fail(error, message);

        var provider = _config.Provider;
        if (provider is null)
            return AiResult<string>.Fail(AiErrorKind.MissingProvider, "Configura un proveedor en Configuración.");
        var key = await _config.GetKeyAsync();
        if (string.IsNullOrWhiteSpace(key))
            return AiResult<string>.Fail(AiErrorKind.MissingKey, "Falta la API key. Ve a Configuración.");

        var model = _config.Model ?? provider.DefaultModel;
        var result = await _client.SendChatAsync(provider, key, model, prompt!.Trim(), null, ct);
        if (!result.Success && result.Error == AiErrorKind.Unauthorized) _config.SetValidated(false);
        return result;
    }

    /// <summary>Valida la clave con una solicitud mínima (1 token) al proveedor elegido por el usuario.</summary>
    public Task<AiResult<string>> ValidateAsync(AiProviderInfo provider, string apiKey, string? model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Task.FromResult(AiResult<string>.Fail(AiErrorKind.MissingKey, "Pega una API key primero."));
        var chosen = string.IsNullOrWhiteSpace(model) ? provider.DefaultModel : model;
        return _client.SendChatAsync(provider, apiKey.Trim(), chosen, "ping", 1, ct);
    }

    public Task<AiResult<IReadOnlyList<AiModel>>> ListModelsAsync(AiProviderInfo provider, string? apiKey, CancellationToken ct = default) =>
        _client.ListModelsAsync(provider, provider.ModelsRequireAuth ? apiKey?.Trim() : null, ct);
}
