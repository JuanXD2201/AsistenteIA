using AsistenteIA.Core.Services;

namespace AsistenteIA.Services;

/// <summary>La API key se guarda solo en SecureStorage (Keystore en Android, DPAPI en Windows).</summary>
public sealed class MauiSecretStore : ISecretStore
{
    public async Task<string?> GetAsync(string name)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(name);
        }
        catch (Exception)
        {
            // Keystore corrupto o restaurado desde copia: se trata como "sin clave".
            SecureStorage.Default.Remove(name);
            return null;
        }
    }

    public Task SetAsync(string name, string value) => SecureStorage.Default.SetAsync(name, value);

    public void Remove(string name) => SecureStorage.Default.Remove(name);
}

/// <summary>Ajustes no sensibles (proveedor, modelo, estado de validación).</summary>
public sealed class MauiSettingsStore : ISettingsStore
{
    public string? Get(string key) => Preferences.Default.ContainsKey(key) ? Preferences.Default.Get(key, string.Empty) : null;

    public void Set(string key, string value) => Preferences.Default.Set(key, value);

    public void Remove(string key) => Preferences.Default.Remove(key);
}
