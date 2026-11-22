using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Formatting;
using Invoicing.Core.Services;
using Invoicing.Data;
using Microsoft.Data.Sqlite;

namespace Invoicing.App.ViewModels;

/// <summary>Coordinates document editing, totals and lifecycle actions.</summary>
public partial class DocumentDetailViewModel : ObservableObject
{
    private readonly DocumentService _documents;
    private readonly DocumentDelivery _delivery;
    private readonly IDialogService _dialogs;
    private DocumentEmailRecord? _latestEmail;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IReadOnlyList<string> _messages = [];

    internal DocumentDetailViewModel(DocumentEditor editor, DocumentDelivery delivery, IDialogService dialogs)
    {
        Editor = editor;
        _documents = editor.Workflow;
        _delivery = delivery;
        _dialogs = dialogs;
        LinesEditor = new(editor.Rates, HasVat);
        LinesEditor.Changed += ApplyLines;
        Editor.DraftChanged += Refresh;
    }

    public event Action? CloseRequested;
    public DocumentEditor Editor { get; }
    public DocumentLinesViewModel LinesEditor { get; }
    private Document Document => Editor.Document;
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public string Title => $"{DocumentTexts.Kind(Document.Kind)} · {Document.Number ?? DocumentTexts.Draft}";
    public string Status => DocumentTexts.Status(Document, Today);
    public bool IsEditable => Document.IsEditable && !IsBusy;
    public string UnitPriceHeader => DocumentTexts.UnitPriceHeader(HasVat);
    public bool HasVat => Document.Kind != DocumentKind.NonVatInvoice;
    public bool IsCreditNote => Document.Kind == DocumentKind.CreditNote;
    public bool CanSelectCustomer => IsEditable && !IsCreditNote;
    public string CustomerName => Document.Customer?.Name ?? string.Empty;
    public string? OriginalNumber => (Document as CreditNote)?.OriginalInvoice?.Number;
    public string Base => LinesEditor.AreValid ? CzechFormat.FormatMoney(Document.CalculateTotalBase()) : DocumentTexts.UnavailableTotal;
    public string Total => LinesEditor.AreValid ? CzechFormat.FormatMoney(Document.CalculateTotal()) : DocumentTexts.UnavailableTotal;
    public string? SentInfo => _latestEmail is null ? null : EmailTexts.SentInfo(_latestEmail);
    public string? SendEmailToolTip => EmailTexts.Availability(_delivery.GetAvailability(Document).Reason);
    public IEnumerable<string> VatRows => GetVatRows().Select(DocumentTexts.VatRow);

    public async Task LoadAsync()
    {
        await Editor.LoadAsync();
        _latestEmail = await _delivery.GetLatestAsync(Document);
        LoadLines();
    }

    partial void OnIsBusyChanged(bool value) => Refresh();

    private bool CanSave() => IsEditable && Document.Customer is not null && LinesEditor.AreValid;
    private bool CanDelete() => IsEditable && Document.Id != 0;
    private bool CanMarkPaid() => !IsBusy && Document.CanMarkAsPaid;
    private bool CanCancelDocument() => !IsBusy && Document.CanCancel;
    private bool CanCreateCreditNote() => !IsBusy && Document is TaxInvoice invoice && invoice.CanCreateCreditNote;
    private bool CanExport() => !IsBusy && !Document.IsEditable;
    private bool CanSendEmail() => !IsBusy && _delivery.GetAvailability(Document).CanSend;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => RunDatabaseActionAsync(() => _documents.SaveDraftAsync(Document));

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task IssueAsync() => RunDatabaseActionAsync(IssueAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanMarkPaid))]
    private Task MarkPaidAsync() => RunDatabaseActionAsync(() => _documents.MarkAsPaidAsync(Document));

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task DeleteAsync() => RunDatabaseActionAsync(DeleteAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanCancelDocument))]
    private Task CancelDocumentAsync() => RunDatabaseActionAsync(CancelAfterConfirmationAsync);

    [RelayCommand(CanExecute = nameof(CanCreateCreditNote))]
    private Task CreateCreditNoteAsync() => RunDatabaseActionAsync(OpenCorrectionAsync);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportAsync() => RunBusyActionAsync(() => _delivery.ExportAsync(Document, _dialogs));

    [RelayCommand(CanExecute = nameof(CanSendEmail))]
    private Task SendEmailAsync() => RunBusyActionAsync(SendAndRefreshAsync);

    private async Task IssueAfterConfirmationAsync()
    {
        if (!await _dialogs.ConfirmAsync(UiTexts.ConfirmIssue))
        {
            return;
        }

        var results = await _documents.IssueAsync(Document);
        if (results.Count > 0)
        {
            await _dialogs.ShowMessageAsync(string.Join(Environment.NewLine, results.Select(ValidationTexts.Format)));
        }
    }

    private async Task DeleteAfterConfirmationAsync()
    {
        if (await _dialogs.ConfirmAsync(UiTexts.ConfirmDelete))
        {
            await _documents.DeleteDraftAsync(Document);
            RequestClose();
        }
    }

    private async Task CancelAfterConfirmationAsync()
    {
        if (await _dialogs.ConfirmAsync(UiTexts.ConfirmCancel))
        {
            await _documents.CancelAsync(Document);
        }
    }

    private async Task OpenCorrectionAsync()
    {
        if (Document is TaxInvoice invoice)
        {
            var correction = await _documents.CreateCreditNoteAsync(invoice, Today);
            var detail = new DocumentDetailViewModel(Editor.ForDocument(correction), _delivery, _dialogs);
            await detail.LoadAsync();
            await _dialogs.ShowDocumentAsync(detail);
        }
    }

    private async Task SendAndRefreshAsync()
    {
        if (await _delivery.SendAsync(Document, _latestEmail, _dialogs))
        {
            _latestEmail = await _delivery.GetLatestAsync(Document);
        }
    }

    private Task RunDatabaseActionAsync(Func<Task> action) => RunBusyActionAsync(async () =>
    {
        try
        {
            await action();
        }
        catch (SqliteException)
        {
            await RecoverDocumentAsync();
        }
        catch (DocumentStateChangedException)
        {
            await RecoverDocumentAsync();
        }
    });

    private async Task RunBusyActionAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RecoverDocumentAsync()
    {
        var exists = await Editor.ReloadAsync();
        await _dialogs.ShowMessageAsync(exists ? UiTexts.DatabaseWriteFailed : UiTexts.DocumentMissing);
        if (exists)
        {
            LoadLines();
        }
        else
        {
            RequestClose();
        }
    }

    private void RequestClose()
    {
        IsBusy = false;
        CloseRequested?.Invoke();
    }

    private void LoadLines()
    {
        LinesEditor.Load(Document.Lines);
        Refresh();
    }

    private void ApplyLines()
    {
        if (IsEditable)
        {
            Document.ReplaceLines(LinesEditor.ReadLines());
            Refresh();
        }
    }

    private void Refresh()
    {
        Messages = Document.IsEditable ? Document.Validate(Editor.Supplier, Editor.Rates)
            .Select(ValidationTexts.Format).Concat(LinesEditor.InputErrors()).ToList() : [];
        foreach (var property in new[] { nameof(Title), nameof(Status), nameof(IsEditable), nameof(CanSelectCustomer),
                     nameof(CustomerName), nameof(Base), nameof(Total), nameof(VatRows), nameof(SendEmailToolTip), nameof(SentInfo) })
        {
            OnPropertyChanged(property);
        }

        NotifyCommands();
    }

    private IReadOnlyList<VatSummaryRow> GetVatRows() => Document switch
    {
        TaxInvoice invoice when LinesEditor.AreValid => invoice.CalculateVatSummary().Rows,
        CreditNote credit when LinesEditor.AreValid => credit.CalculateVatSummary().Rows,
        _ => []
    };

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
        LinesEditor.SetEditable(IsEditable);
    }
}
