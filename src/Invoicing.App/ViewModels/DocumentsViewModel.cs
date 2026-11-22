using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
using Invoicing.App.Texts;
using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;

namespace Invoicing.App.ViewModels;

/// <summary>Lists and filters documents and opens the document editor.</summary>
public partial class DocumentsViewModel : ObservableObject
{
    private const int DraftFilter = 1;
    private const int IssuedFilter = 2;
    private const int PaidFilter = 3;
    private const int OverdueFilter = 4;

    private readonly DocumentWorkspace _workspace;
    private readonly DocumentDelivery _delivery;
    private readonly IDialogService _dialogs;
    private readonly VatRates _vatRates;
    private IReadOnlyList<Document> _all = [];
    [ObservableProperty] private bool _hasSupplier;
    [ObservableProperty] private int _filterIndex;
    [ObservableProperty] private DocumentListItemViewModel? _selected;
    [ObservableProperty] private IReadOnlyList<DocumentListItemViewModel> _items = [];

    internal DocumentsViewModel(DocumentWorkspace workspace, DocumentDelivery delivery, IDialogService dialogs, VatRates rates)
    {
        _workspace = workspace;
        _delivery = delivery;
        _dialogs = dialogs;
        _vatRates = rates;
    }

    public IReadOnlyList<string> Filters => DocumentTexts.Filters;
    public string? NewToolTip => HasSupplier ? null : DocumentTexts.SupplierRequired;

    public async Task LoadAsync()
    {
        HasSupplier = await _workspace.GetSupplierAsync() is not null;
        _all = await _workspace.GetDocumentsAsync();
        Filter();
    }

    public void EnableNewDocument() => HasSupplier = true;

    partial void OnFilterIndexChanged(int value) => Filter();
    partial void OnSelectedChanged(DocumentListItemViewModel? value) => OpenCommand.NotifyCanExecuteChanged();

    partial void OnHasSupplierChanged(bool value)
    {
        NewDocumentCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(NewToolTip));
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task NewDocumentAsync() =>
        await ShowDocumentAsync(await _workspace.Workflow.CreateDraftAsync(DateOnly.FromDateTime(DateTime.Today)));

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task OpenAsync()
    {
        if (Selected is not null && await _workspace.FindDocumentAsync(Selected.Id) is Document document)
        {
            await ShowDocumentAsync(document);
        }
    }

    private bool CanCreate() => HasSupplier;
    private bool CanOpen() => Selected is not null;

    private async Task ShowDocumentAsync(Document document)
    {
        var editor = new DocumentEditor(document, _workspace, _vatRates);
        var detail = new DocumentDetailViewModel(editor, _delivery, _dialogs);
        await detail.LoadAsync();
        await _dialogs.ShowDocumentAsync(detail);
        await LoadAsync();
    }

    private void Filter()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Items = _all.Where(document => Matches(document, today))
            .Select(document => new DocumentListItemViewModel(document, today)).ToList();
    }

    private bool Matches(Document document, DateOnly today) => FilterIndex switch
    {
        DraftFilter => document.Status == DocumentStatus.Draft,
        IssuedFilter => document.Status == DocumentStatus.Issued,
        PaidFilter => document.Status == DocumentStatus.Paid,
        OverdueFilter => document.IsOverdue(today),
        _ => true
    };
}
