using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Invoicing.App.ViewModels;

/// <summary>Provides the explanation and corrective instructions for a startup error.</summary>
public partial class ErrorViewModel(string errors, string help) : ObservableObject
{
    public event Action? CloseRequested;
    public string Errors { get; } = errors;
    public string Help { get; } = help;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
