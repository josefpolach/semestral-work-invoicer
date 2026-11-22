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

    public IReadOnlyList<string> Filters { get; } = ["Vše", "Koncepty", "Vystavené", "Zaplacené", "Po splatnosti"];
    public string? NewToolTip => HasSupplier ? null : DocumentTexts.SupplierRequired;

    public async Task LoadAsync()
    {
        HasSupplier = await _workspace.Suppliers.GetAsync() is not null;
        _all = await _workspace.Documents.GetAllAsync();
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
        if (Selected is not null && await _workspace.Documents.GetByIdAsync(Selected.Id) is Document document)
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
        1 => document.Status == DocumentStatus.Draft,
        2 => document.Status == DocumentStatus.Issued,
        3 => document.Status == DocumentStatus.Paid,
        4 => document.IsOverdue(today),
        _ => true
    };
}
