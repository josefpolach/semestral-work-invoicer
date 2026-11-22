namespace Invoicing.App.ViewModels;

/// <summary>Provides the explanation and corrective instructions for a startup error.</summary>
public class ErrorViewModel(string errors, string help)
{
    public string Errors { get; } = errors;
    public string Help { get; } = help;
}
