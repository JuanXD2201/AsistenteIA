using System.Collections.ObjectModel;
using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsistenteIA.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly IHistoryRepository _history;

    public ObservableCollection<HistoryEntry> Items { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public HistoryViewModel(IHistoryRepository history) => _history = history;

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ClearMessage();
        try
        {
            var entries = await _history.GetAllAsync(SearchText);
            Items.Clear();
            foreach (var entry in entries) Items.Add(entry);
            IsEmpty = Items.Count == 0;
        }
        catch (Exception)
        {
            ShowError("No se pudo leer el historial local.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static Task OpenAsync(HistoryEntry? entry) =>
        entry is null ? Task.CompletedTask : Shell.Current.GoToAsync($"detail?id={entry.Id}");
}
