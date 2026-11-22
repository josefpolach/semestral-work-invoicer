using Invoicing.App.ViewModels;

namespace Invoicing.App.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string message);
    Task<bool> ConfirmAsync(string message);
    Task<Stream?> PickPdfAsync(string filename);
    Task ShowDocumentAsync(DocumentDetailViewModel model);
    Task<bool> ShowEmailAsync(SendEmailViewModel model);
}
