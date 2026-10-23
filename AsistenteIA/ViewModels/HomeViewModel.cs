using AsistenteIA.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsistenteIA.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly AiConfigService _config;
    private readonly IHistoryRepository _history;

    [ObservableProperty]
    private string _statusText = "Comprobando configuración…";

    [ObservableProperty]
    private bool _isReady;

    [ObservableProperty]
    private int _historyCount;

    public HomeViewModel(AiConfigService config, IHistoryRepository history)
    {
        _config = config;
        _history = history;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsReady = await _config.IsReadyAsync();
        var provider = _config.Provider;
        StatusText = IsReady
            ? $"IA lista: {provider?.Name} · modelo {_config.Model}"
            : "IA sin configurar. Ve a Configuración, pega tu API key y prueba la conexión.";
        try
        {
            HistoryCount = (await _history.GetAllAsync()).Count;
        }
        catch (Exception ex)
        {
            // Solo tipo y mensaje de la excepción: nunca claves ni contenido del usuario.
            System.Diagnostics.Debug.WriteLine($"AsistenteIA: lectura de historial falló: {ex.GetType().Name}: {ex.Message}");
            HistoryCount = 0;
            ShowError("No se pudo leer el historial local.");
        }
    }

    [RelayCommand]
    private static Task GoToQueryAsync() => Shell.Current.GoToAsync("//query");

    [RelayCommand]
    private static Task GoToHistoryAsync() => Shell.Current.GoToAsync("//history");

    [RelayCommand]
    private static Task GoToSettingsAsync() => Shell.Current.GoToAsync("//settings");
}
