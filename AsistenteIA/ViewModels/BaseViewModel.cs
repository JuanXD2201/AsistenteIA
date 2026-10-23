using CommunityToolkit.Mvvm.ComponentModel;

namespace AsistenteIA.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    public bool IsNotBusy => !IsBusy;

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    protected void ShowError(string text)
    {
        IsError = true;
        Message = text;
    }

    protected void ShowSuccess(string text)
    {
        IsError = false;
        Message = text;
    }

    protected void ClearMessage()
    {
        IsError = false;
        Message = string.Empty;
    }
}
