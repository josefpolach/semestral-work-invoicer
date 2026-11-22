using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Invoicing.App.Texts;
using Invoicing.App.ViewModels;
using Invoicing.App.Views;

namespace Invoicing.App.Services;

/// <summary>Opens modal windows and asks the desktop platform to choose an export file.</summary>
internal class DialogService(Window main) : IDialogService
{
    // The newest open dialog owns the next one.
    private readonly Stack<Window> _windows = new();
    private Window Owner => _windows.TryPeek(out var window) ? window : main;

    public async Task ShowMessageAsync(string message) => await OpenMessageAsync(message, false);
    public Task<bool> ConfirmAsync(string message) => OpenMessageAsync(message, true);

    public async Task<Stream?> PickPdfAsync(string filename)
    {
        var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = UiTexts.ExportPdf,
            SuggestedFileName = filename,
            DefaultExtension = "pdf",
            FileTypeChoices = [FilePickerFileTypes.Pdf],
            ShowOverwritePrompt = true
        });
        return file is null ? null : await file.OpenWriteAsync();
    }

    public async Task ShowDocumentAsync(DocumentDetailViewModel model)
    {
        var window = new DocumentDetailWindow { DataContext = model };
        window.Closing += (_, args) => args.Cancel = model.IsBusy;
        model.CloseRequested += window.Close;
        try
        {
            await ShowAsync(window);
        }
        finally
        {
            model.CloseRequested -= window.Close;
        }
    }

    public Task<bool> ShowEmailAsync(SendEmailViewModel model)
    {
        var window = new SendEmailWindow { DataContext = model };
        window.Closing += (_, args) => args.Cancel = model.IsSending;
        model.CloseRequested += result => window.Close(result);
        return ShowAsync(window);
    }

    private Task<bool> OpenMessageAsync(string message, bool confirm) =>
        ShowAsync(CreateMessageWindow(message, confirm));

    internal static MessageWindow CreateMessageWindow(string message, bool confirm)
    {
        var model = new MessageViewModel(message, confirm);
        var window = new MessageWindow { DataContext = model };
        model.CloseRequested += result => window.Close(result);
        return window;
    }

    private async Task<bool> ShowAsync(Window window)
    {
        var owner = Owner;
        _windows.Push(window);
        try
        {
            return await window.ShowDialog<bool>(owner);
        }
        finally
        {
            _windows.Pop();
        }
    }
}
