using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Invoicing.App.Views;

/// <summary>Displays the message window.</summary>
public partial class MessageWindow : Window
{
    public MessageWindow() => InitializeComponent();

    private void ClosePositive(object? sender, RoutedEventArgs args) => Close(true);
    private void CloseNegative(object? sender, RoutedEventArgs args) => Close(false);
}
