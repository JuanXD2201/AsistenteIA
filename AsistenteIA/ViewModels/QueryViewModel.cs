using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsistenteIA.ViewModels;

public partial class QueryViewModel : BaseViewModel
{
    public static readonly string[] Categories = { "General", "Estudio", "Programación", "Trabajo", "Otro" };

    private readonly AiService _ai;
    private readonly AiConfigService _config;
    private readonly IHistoryRepository _history;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAnswer))]
    private string _answer = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = Categories[0];

    [ObservableProperty]
    private bool _isConfigured;

    [ObservableProperty]
    private bool _needsConfiguration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isSaved;

    public bool CanSave => !IsSaved;

    [ObservableProperty]
    private int _promptLength;

    public bool HasAnswer => !string.IsNullOrEmpty(Answer);

    public int MaxLength => PromptValidator.MaxLength;

    public QueryViewModel(AiService ai, AiConfigService config, IHistoryRepository history)
    {
        _ai = ai;
        _config = config;
        _history = history;
    }

    partial void OnPromptChanged(string value) => PromptLength = value?.Length ?? 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsConfigured = await _config.IsReadyAsync();
        NeedsConfiguration = !IsConfigured;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsBusy) return;
        ClearMessage();
        IsSaved = false;

        var (valid, _, text) = PromptValidator.Validate(Prompt);
        if (!valid)
        {
            ShowError(text);
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        Answer = string.Empty;
        try
        {
            var result = await _ai.AskAsync(Prompt, _cts.Token);
            if (result.Success)
            {
                Answer = result.Value ?? string.Empty;
            }
            else if (result.Error != AiErrorKind.Cancelled)
            {
                ShowError(result.Message);
                NeedsConfiguration = result.Error is AiErrorKind.MissingKey or AiErrorKind.MissingProvider or AiErrorKind.Unauthorized;
            }
        }
        finally
        {
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!HasAnswer || IsSaved) return;
        try
        {
            await _history.AddAsync(new HistoryEntry
            {
                Prompt = Prompt.Trim(),
                Answer = Answer,
                Category = SelectedCategory,
                CreatedAt = DateTime.UtcNow,
                Provider = _config.Provider?.Name ?? string.Empty,
                Model = _config.Model ?? string.Empty
            });
            IsSaved = true;
            ShowSuccess("Guardado en el historial.");
        }
        catch (Exception)
        {
            ShowError("No se pudo guardar en el historial local.");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Prompt = string.Empty;
        Answer = string.Empty;
        IsSaved = false;
        ClearMessage();
    }

    [RelayCommand]
    private static Task GoToSettingsAsync() => Shell.Current.GoToAsync("//settings");
}
