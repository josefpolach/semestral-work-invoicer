namespace Invoicing.App.ViewModels;

/// <summary>Provides text and the type of buttons for a message window.</summary>
public class MessageViewModel(string message, bool confirm)
{
    public string Message { get; } = message;
    public bool Confirm { get; } = confirm;
}
