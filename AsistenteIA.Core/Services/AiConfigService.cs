using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

/// <summary>Configuración del proveedor: clave en almacén seguro, resto en ajustes locales.</summary>
public sealed class AiConfigService
{
    private const string KeyName = "ai_api_key";
    private const string ProviderKey = "ai_provider";
    private const string ModelKey = "ai_model";
    private const string ValidatedKey = "ai_validated";

    private readonly ISecretStore _secrets;
    private readonly ISettingsStore _settings;

    public AiConfigService(ISecretStore secrets, ISettingsStore settings)
    {
        _secrets = secrets;
        _settings = settings;
    }

    public AiProviderInfo? Provider => ProviderCatalog.FindById(_settings.Get(ProviderKey));

    public string? Model => _settings.Get(ModelKey);

    public bool IsValidated => _settings.Get(ValidatedKey) == "1";

    public Task<string?> GetKeyAsync() => _secrets.GetAsync(KeyName);

    public async Task<bool> HasKeyAsync() => !string.IsNullOrWhiteSpace(await GetKeyAsync());

    public async Task<bool> IsReadyAsync() => Provider is not null && IsValidated && await HasKeyAsync();

    public async Task SaveAsync(string apiKey, AiProviderInfo provider, string? model, bool validated)
    {
        await _secrets.SetAsync(KeyName, apiKey.Trim());
        _settings.Set(ProviderKey, provider.Id);
        _settings.Set(ModelKey, string.IsNullOrWhiteSpace(model) ? provider.DefaultModel : model);
        _settings.Set(ValidatedKey, validated ? "1" : "0");
    }

    public void SetValidated(bool validated) => _settings.Set(ValidatedKey, validated ? "1" : "0");

    public void Clear()
    {
        _secrets.Remove(KeyName);
        _settings.Remove(ProviderKey);
        _settings.Remove(ModelKey);
        _settings.Remove(ValidatedKey);
    }
}
