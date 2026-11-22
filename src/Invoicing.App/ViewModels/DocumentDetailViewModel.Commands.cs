using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Microsoft.Data.Sqlite;

namespace Invoicing.App.ViewModels;

public partial class DocumentDetailViewModel
{
    private bool CanSave() => IsEditable && Document.Customer is not null && Lines.All(line => line.IsValid);
    private bool CanDelete() => IsEditable && Document.Id != 0;
    private bool CanMarkPaid() => !IsBusy && Document.CanMarkAsPaid;
    private bool CanCancelDocument() => !IsBusy && Document.CanCancel;
    private bool CanCreateCreditNote() => !IsBusy && Document is TaxInvoice { CanCreateCreditNote: true };
    private bool CanExport() => !IsBusy && !Document.IsEditable;
    private bool CanSendEmail() => !IsBusy && EmailAvailability.Evaluate(_delivery.Settings, Document).CanSend;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => RunDatabaseActionAsync(() => _editor.Workspace.Workflow.SaveDraftAsync(Document));

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task IssueAsync() => RunDatabaseActionAsync(IssueAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanMarkPaid))]
    private Task MarkPaidAsync() => RunDatabaseActionAsync(() => _editor.Workspace.Workflow.MarkAsPaidAsync(Document));

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task DeleteAsync() => RunDatabaseActionAsync(DeleteAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanCancelDocument))]
    private Task CancelDocumentAsync() => RunDatabaseActionAsync(CancelAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanCreateCreditNote))]
    private Task CreateCreditNoteAsync() => RunDatabaseActionAsync(OpenCorrectionAsync);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        IsBusy = true;
        try
        {
            var pdf = await _delivery.PrepareAsync(Document);
            if (await SaveFileAsync(pdf))
            {
                await _dialogs.ShowMessageAsync("PDF bylo uloženo.");
            }
        }
        catch (IOException)
        {
            await _dialogs.ShowMessageAsync("PDF se nepodařilo uložit.");
        }
        catch (UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync("Do vybraného souboru nelze zapisovat.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendEmail))]
    private Task SendEmailAsync() => RunDatabaseActionAsync(OpenEmailAsync);

    private async Task IssueAfterConfirmationAsync()
    {
        if (!await _dialogs.ConfirmAsync("Vystavit doklad? Po vystavení už nepůjde upravit."))
        {
            return;
        }

        var results = await _editor.Workspace.Workflow.IssueAsync(Document);
        if (results.Count > 0)
        {
            await _dialogs.ShowMessageAsync(string.Join(Environment.NewLine, results.Select(ValidationTexts.Format)));
        }
    }

    private async Task DeleteAfterConfirmationAsync()
    {
        if (await _dialogs.ConfirmAsync("Opravdu chcete smazat tento koncept?"))
        {
            await _editor.Workspace.Workflow.DeleteDraftAsync(Document);
            CloseRequested?.Invoke();
        }
    }

    private async Task CancelAfterConfirmationAsync()
    {
        if (await _dialogs.ConfirmAsync("Opravdu chcete doklad stornovat? Tuto akci nelze vrátit."))
        {
            await _editor.Workspace.Workflow.CancelAsync(Document);
        }
    }

    private async Task OpenCorrectionAsync()
    {
        if (Document is TaxInvoice invoice)
        {
            var correction = await _editor.Workspace.Workflow.CreateCreditNoteAsync(invoice, DateOnly.FromDateTime(DateTime.Today));
            var editor = new DocumentEditor(correction, _editor.Workspace, _editor.Rates);
            var detail = new DocumentDetailViewModel(editor, _delivery, _dialogs);
            await detail.LoadAsync();
            await _dialogs.ShowDocumentAsync(detail);
        }
    }

    private async Task<bool> SaveFileAsync(EmailAttachment pdf)
    {
        await using var stream = await _dialogs.PickPdfAsync(pdf.FileName);
        if (stream is null)
        {
            return false;
        }

        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await stream.WriteAsync(pdf.Content);
        return true;
    }

    private async Task OpenEmailAsync()
    {
        var pdf = await _delivery.PrepareAsync(Document);
        var model = new SendEmailViewModel(Document, _delivery.Compose(Document, pdf), _delivery.Mail, _latestEmail);
        if (await _dialogs.ShowEmailAsync(model))
        {
            _latestEmail = await _delivery.GetLatestAsync(Document);
            await _dialogs.ShowMessageAsync($"E-mail byl odeslán na {model.Recipient?.Trim()}.");
        }
    }

    private async Task RunDatabaseActionAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (SqliteException)
        {
            await HandleDatabaseFailureAsync();
        }
        catch (InvalidOperationException) when (Document.Id != 0)
        {
            await HandleDatabaseFailureAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task HandleDatabaseFailureAsync()
    {
        await _editor.ReloadAsync();
        LoadForm();
        await _dialogs.ShowMessageAsync("Operaci se nepodařilo uložit do databáze. Zkuste to znovu.");
    }

    private void NotifyCommands()
    {
        SaveCommand.NotifyCanExecuteChanged();
        IssueCommand.NotifyCanExecuteChanged();
        MarkPaidCommand.NotifyCanExecuteChanged();
        CancelDocumentCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        CreateCreditNoteCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        SendEmailCommand.NotifyCanExecuteChanged();
        AddLineCommand.NotifyCanExecuteChanged();
        RemoveLineCommand.NotifyCanExecuteChanged();
    }
}
