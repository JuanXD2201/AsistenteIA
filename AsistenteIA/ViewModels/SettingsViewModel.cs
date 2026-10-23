using System.Collections.ObjectModel;
using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsistenteIA.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AiService _ai;
    private readonly AiConfigService _config;
    private CancellationTokenSource? _cts;

    public IReadOnlyList<AiProviderInfo> Providers => ProviderCatalog.All;

    public ObservableCollection<string> Models { get; } = new();

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProvider))]
    [NotifyPropertyChangedFor(nameof(KeyUrl))]
    private AiProviderInfo? _selectedProvider;

    [ObservableProperty]
    private string? _selectedModel;

    [ObservableProperty]
    private string _validationText = "Sin validar";

    [ObservableProperty]
    private bool _isValidated;

    [ObservableProperty]
    private bool _hasSavedKey;

    [ObservableProperty]
    private bool _isConfirmingDelete;

    public bool HasProvider => SelectedProvider is not null;

    public string KeyUrl => SelectedProvider?.KeyUrl ?? string.Empty;

    public SettingsViewModel(AiService ai, AiConfigService config)
    {
        _ai = ai;
        _config = config;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasSavedKey = await _config.HasKeyAsync();
        SelectedProvider = _config.Provider;
        IsValidated = HasSavedKey && _config.IsValidated;
        ValidationText = !HasSavedKey ? "Sin clave guardada" : IsValidated ? "Clave validada" : "Clave guardada sin validar";
        if (SelectedProvider is not null && SelectedModel is null)
        {
            SelectedModel = _config.Model;
            if (SelectedModel is not null) Models.Add(SelectedModel);
        }
    }

    /// <summary>Clave tecleada o, si el campo está vacío, la ya guardada en SecureStorage.</summary>
    private async Task<string?> ResolveKeyAsync() =>
        string.IsNullOrWhiteSpace(ApiKey) ? await _config.GetKeyAsync() : ApiKey.Trim();

    [RelayCommand]
    private void DetectProvider()
    {
        ClearMessage();
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ShowError("Pega una API key para detectar el proveedor.");
            return;
        }

        var detected = ProviderDetector.Detect(ApiKey);
        if (detected is null)
        {
            ShowError("No se pudo identificar el proveedor con certeza: algunos formatos de clave son indistinguibles. Elígelo en la lista.");
            return;
        }

        SelectedProvider = detected;
        SelectedModel = detected.DefaultModel;
        Models.Clear();
        ShowSuccess($"Proveedor detectado por el formato de la clave: {detected.Name}. Pruébalo antes de guardar.");
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        if (!RequireProvider(out var provider) || IsBusy) return;
        var key = await ResolveKeyAsync();
        if (provider.ModelsRequireAuth && string.IsNullOrWhiteSpace(key))
        {
            ShowError("Este proveedor exige la API key para listar modelos.");
            return;
        }

        IsBusy = true;
        ClearMessage();
        try
        {
            var result = await _ai.ListModelsAsync(provider, key);
            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            Models.Clear();
            foreach (var m in result.Value!) Models.Add(m.Id);
            if (SelectedModel is null || !Models.Contains(SelectedModel))
                SelectedModel = Models.Contains(provider.DefaultModel) ? provider.DefaultModel : Models[0];
            ShowSuccess($"{Models.Count} modelos disponibles.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestAsync()
    {
        if (!RequireProvider(out var provider) || IsBusy) return;
        var key = await ResolveKeyAsync();
        await ValidateAndMaybeSaveAsync(provider, key, save: false);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!RequireProvider(out var provider) || IsBusy) return;
        var key = await ResolveKeyAsync();
        await ValidateAndMaybeSaveAsync(provider, key, save: true);
    }

    private async Task ValidateAndMaybeSaveAsync(AiProviderInfo provider, string? key, bool save)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            ShowError("Pega una API key primero.");
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ClearMessage();
        try
        {
            var result = await _ai.ValidateAsync(provider, key, SelectedModel, _cts.Token);
            IsValidated = result.Success;
            ValidationText = result.Success ? "Conexión correcta" : "Validación fallida";

            if (save)
            {
                await _config.SaveAsync(key, provider, SelectedModel, result.Success);
                HasSavedKey = true;
                ApiKey = string.Empty;
            }

            if (result.Success)
                ShowSuccess(save ? "Clave validada y guardada de forma segura." : "Conexión correcta con el proveedor.");
            else
                ShowError(save ? $"Clave guardada, pero no validada: {result.Message}" : result.Message);
        }
        finally
        {
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void ChangeKey()
    {
        ApiKey = string.Empty;
        IsValidated = false;
        ValidationText = "Escribe la nueva clave y pulsa Guardar";
        ShowSuccess("La clave anterior se reemplaza al guardar.");
    }

    [RelayCommand]
    private void AskDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private void ConfirmDelete()
    {
        _config.Clear();
        ApiKey = string.Empty;
        SelectedProvider = null;
        SelectedModel = null;
        Models.Clear();
        HasSavedKey = false;
        IsValidated = false;
        IsConfirmingDelete = false;
        ValidationText = "Sin clave guardada";
        ShowSuccess("Clave eliminada del dispositivo.");
    }

    [RelayCommand]
    private void CancelRequest() => _cts?.Cancel();

    private bool RequireProvider(out AiProviderInfo provider)
    {
        provider = SelectedProvider!;
        if (SelectedProvider is not null) return true;
        ShowError("Elige un proveedor o usa Detectar proveedor.");
        return false;
    }
}
