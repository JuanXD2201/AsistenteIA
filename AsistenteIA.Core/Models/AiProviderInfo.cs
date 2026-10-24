namespace AsistenteIA.Core.Models;

/// <summary>Descriptor de un proveedor con API con el esquema Chat Completions.</summary>
public sealed record AiProviderInfo(
    string Id,
    string Name,
    string BaseUrl,
    string DefaultModel,
    string KeyUrl,
    bool ModelsRequireAuth);

public sealed record AiModel(string Id, string? Title = null, bool PaidOnly = false);
