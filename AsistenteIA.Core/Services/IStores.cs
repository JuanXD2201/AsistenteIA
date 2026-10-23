namespace AsistenteIA.Core.Services;

/// <summary>Almacén de secretos (SecureStorage en MAUI).</summary>
public interface ISecretStore
{
    Task<string?> GetAsync(string name);
    Task SetAsync(string name, string value);
    void Remove(string name);
}

/// <summary>Ajustes no sensibles (Preferences en MAUI).</summary>
public interface ISettingsStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Remove(string key);
}
