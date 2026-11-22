using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Invoicing.App.ViewModels;

/// <summary>Provides the message, available buttons and result for a dialog.</summary>
public partial class MessageViewModel(string message, bool confirm) : ObservableObject
{
    public event Action<bool>? CloseRequested;
    public string Message { get; } = message;
    public bool Confirm { get; } = confirm;

    [RelayCommand]
    private void Accept() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Reject() => CloseRequested?.Invoke(false);
}
