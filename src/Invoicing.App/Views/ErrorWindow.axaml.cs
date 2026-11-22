using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Invoicing.App.Views;

/// <summary>Displays the error window.</summary>
public partial class ErrorWindow : Window
{
    public ErrorWindow() => InitializeComponent();

    private void CloseWindow(object? sender, RoutedEventArgs args) => Close();
}
