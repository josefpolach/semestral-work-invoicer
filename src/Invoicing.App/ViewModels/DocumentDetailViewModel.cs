using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Formatting;
using Invoicing.Core.Parties;

namespace Invoicing.App.ViewModels;

/// <summary>Maintains a document form, its live totals and available actions.</summary>
public partial class DocumentDetailViewModel : ObservableObject
{
    private readonly DocumentEditor _editor;
    private readonly DocumentDelivery _delivery;
    private readonly IDialogService _dialogs;
    private DocumentEmailRecord? _latestEmail;
    private bool _loadingForm;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private InvoiceLineViewModel? _selectedLine;
    [ObservableProperty] private DateTime? _issueDate;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private DateTime? _supplyDate;
    [ObservableProperty] private string? _correctionReason;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IReadOnlyList<string> _messages = [];

    internal DocumentDetailViewModel(DocumentEditor editor, DocumentDelivery delivery, IDialogService dialogs)
    {
        _editor = editor;
        _delivery = delivery;
        _dialogs = dialogs;
    }

    public event Action? CloseRequested;
    private Document Document => _editor.Document;
    public string Title => $"{DocumentTexts.Kind(Document.Kind)} · {Document.Number ?? DocumentTexts.Draft}";
    public string Status => DocumentTexts.Status(Document, DateOnly.FromDateTime(DateTime.Today));
    public bool IsEditable => Document.IsEditable && !IsBusy;
    public string UnitPriceHeader => HasVat ? "Cena za jednotku bez DPH" : "Cena za jednotku";
    public bool HasVat => Document.Kind != DocumentKind.NonVatInvoice;
    public bool IsCreditNote => Document.Kind == DocumentKind.CreditNote;
    public bool CanSelectCustomer => IsEditable && !IsCreditNote;
    public string CustomerName => Document.Customer?.Name ?? string.Empty;
    public string? OriginalNumber => (Document as CreditNote)?.OriginalInvoice?.Number;
    public IReadOnlyList<Customer> Customers => _editor.Customers;
    public ObservableCollection<InvoiceLineViewModel> Lines { get; } = [];
    public string Base => Lines.All(line => line.IsValid) ? CzechFormat.FormatMoney(Document.CalculateTotalBase()) : "—";
    public string Total => Lines.All(line => line.IsValid) ? CzechFormat.FormatMoney(Document.CalculateTotal()) : "—";
    public string? SentInfo => _latestEmail is null ? null : $"Odesláno {EmailTexts.FormatDate(_latestEmail)} na {_latestEmail.SentTo}";
    public string? SendEmailToolTip => EmailTexts.Availability(EmailAvailability.Evaluate(_delivery.Settings, Document).Reason);
    public IEnumerable<string> VatRows => GetVatRows().Select(row =>
        $"{CzechFormat.FormatQuantity(row.VatRate)} % · Základ {CzechFormat.FormatMoney(row.TaxBase)} · DPH {CzechFormat.FormatMoney(row.VatAmount)}");

    public async Task LoadAsync()
    {
        await _editor.LoadAsync();
        _latestEmail = await _delivery.GetLatestAsync(Document);
        LoadForm();
    }

    private void LoadForm()
    {
        _loadingForm = true;
        IssueDate = ToCalendar(Document.IssueDate);
        DueDate = ToCalendar(Document.DueDate);
        SupplyDate = ToCalendar(Document switch
        {
            TaxInvoice invoice => invoice.TaxableSupplyDate,
            CreditNote credit => credit.TaxableSupplyDate,
            _ => null
        });
        CorrectionReason = (Document as CreditNote)?.CorrectionReason;
        SelectedCustomer = Customers.FirstOrDefault(customer => customer.Id == Document.Customer?.Id);
        foreach (var line in Lines)
        {
            line.PropertyChanged -= HandleLineChanged;
        }

        Lines.Clear();
        foreach (var line in Document.Lines)
        {
            AppendLine(line);
        }

        _loadingForm = false;
        Refresh();
    }

    partial void OnIssueDateChanged(DateTime? value) => UpdateDraft();
    partial void OnDueDateChanged(DateTime? value) => UpdateDraft();
    partial void OnSupplyDateChanged(DateTime? value) => UpdateDraft();
    partial void OnCorrectionReasonChanged(string? value) => UpdateDraft();
    partial void OnSelectedCustomerChanged(Customer? value) => UpdateDraft();
    partial void OnSelectedLineChanged(InvoiceLineViewModel? value) => RemoveLineCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => Refresh();

    private void UpdateDraft()
    {
        if (_loadingForm || !IsEditable)
        {
            return;
        }

        Document.IssueDate = ToDomain(IssueDate);
        Document.DueDate = ToDomain(DueDate);
        if (!IsCreditNote)
        {
            Document.Customer = SelectedCustomer;
        }

        if (Document is TaxInvoice invoice)
        {
            invoice.TaxableSupplyDate = ToDomain(SupplyDate);
        }
        else if (Document is CreditNote credit)
        {
            credit.TaxableSupplyDate = ToDomain(SupplyDate);
            credit.CorrectionReason = CorrectionReason?.Trim() ?? string.Empty;
        }

        Document.ReplaceLines(Lines.Select(line => line.ToLine()));
        Refresh();
    }

    private void Refresh()
    {
        Messages = Document.IsEditable ? Document.Validate(_editor.Supplier, _editor.Rates)
            .Select(ValidationTexts.Format).Concat(InputErrors()).ToList() : [];
        foreach (var property in new[] { nameof(Title), nameof(Status), nameof(IsEditable), nameof(CanSelectCustomer),
                     nameof(CustomerName), nameof(Base), nameof(Total), nameof(VatRows), nameof(SendEmailToolTip), nameof(SentInfo) })
        {
            OnPropertyChanged(property);
        }

        NotifyCommands();
    }

    private IEnumerable<string> InputErrors() => Lines.Select((line, index) => (line, index))
        .Where(item => !item.line.IsValid).Select(item => $"Řádek {item.index + 1}: {DocumentTexts.InvalidNumber}.");

    private IReadOnlyList<VatSummaryRow> GetVatRows() => Document switch
    {
        TaxInvoice invoice when Lines.All(line => line.IsValid) => invoice.CalculateVatSummary().Rows,
        CreditNote credit when Lines.All(line => line.IsValid) => credit.CalculateVatSummary().Rows,
        _ => []
    };

    private void AppendLine(InvoiceLine line)
    {
        var item = new InvoiceLineViewModel(line, HasVat ? _editor.Rates.All : []);
        item.PropertyChanged += HandleLineChanged;
        Lines.Add(item);
    }

    private void HandleLineChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(InvoiceLineViewModel.Description) or nameof(InvoiceLineViewModel.Quantity)
            or nameof(InvoiceLineViewModel.Unit) or nameof(InvoiceLineViewModel.UnitPrice)
            or nameof(InvoiceLineViewModel.Discount) or nameof(InvoiceLineViewModel.VatRate))
        {
            UpdateDraft();
        }
    }

    private bool CanEditLines() => IsEditable;
    private bool CanRemoveLine() => IsEditable && SelectedLine is not null;

    [RelayCommand(CanExecute = nameof(CanEditLines))]
    private void AddLine()
    {
        AppendLine(new(string.Empty, 1, "ks", 0, HasVat ? _editor.Rates.Standard : 0, 0));
        SelectedLine = Lines.Last();
        UpdateDraft();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveLine))]
    private void RemoveLine()
    {
        if (SelectedLine is InvoiceLineViewModel line)
        {
            line.PropertyChanged -= HandleLineChanged;
            Lines.Remove(line);
            SelectedLine = null;
            UpdateDraft();
        }
    }

    private static DateTime? ToCalendar(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);
    private static DateOnly? ToDomain(DateTime? date) => date is DateTime value ? DateOnly.FromDateTime(value) : null;
}
