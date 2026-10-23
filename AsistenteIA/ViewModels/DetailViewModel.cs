using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsistenteIA.ViewModels;

public partial class DetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IHistoryRepository _history;
    private HistoryEntry? _entry;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _answer = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = QueryViewModel.Categories[0];

    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private string _createdAtText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSource))]
    private string _sourceText = string.Empty;

    public bool HasSource => !string.IsNullOrEmpty(SourceText);

    [ObservableProperty]
    private bool _isConfirmingDelete;

    [ObservableProperty]
    private bool _found;

    public string[] Categories => QueryViewModel.Categories;

    public DetailViewModel(IHistoryRepository history) => _history = history;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id))
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        IsBusy = true;
        ClearMessage();
        try
        {
            _entry = await _history.GetAsync(id);
            Found = _entry is not null;
            if (_entry is null)
            {
                ShowError("El registro ya no existe.");
                return;
            }
            Prompt = _entry.Prompt;
            Answer = _entry.Answer;
            SelectedCategory = _entry.Category;
            IsVerified = _entry.IsVerified;
            CreatedAtText = _entry.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            SourceText = string.IsNullOrEmpty(_entry.Provider) ? string.Empty : $"{_entry.Provider} · {_entry.Model}";
        }
        catch (Exception)
        {
            ShowError("No se pudo leer el registro.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (_entry is null) return;
        _entry.Answer = Answer;
        _entry.Category = SelectedCategory;
        _entry.IsVerified = IsVerified;
        try
        {
            if (await _history.UpdateAsync(_entry)) ShowSuccess("Cambios guardados.");
            else ShowError("El registro ya no existe.");
        }
        catch (Exception)
        {
            ShowError("No se pudieron guardar los cambios.");
        }
    }

    [RelayCommand]
    private void AskDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        if (_entry is null) return;
        try
        {
            await _history.DeleteAsync(_entry.Id);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            IsConfirmingDelete = false;
            ShowError("No se pudo eliminar el registro.");
        }
    }
}
