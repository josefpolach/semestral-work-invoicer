using CommunityToolkit.Mvvm.ComponentModel;
using Invoicing.App.Services;
using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Core.Services;

namespace Invoicing.App.ViewModels;

/// <summary>Loads and edits the header fields of a document.</summary>
public partial class DocumentEditor : ObservableObject
{
    private readonly DocumentWorkspace _workspace;
    private bool _loading;

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private DateTime? _issueDate;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private DateTime? _supplyDate;
    [ObservableProperty] private string? _correctionReason;

    internal DocumentEditor(Document document, DocumentWorkspace workspace, VatRates rates)
    {
        Document = document;
        _workspace = workspace;
        Rates = rates;
    }

    internal event Action? DraftChanged;
    internal Document Document { get; private set; }
    internal DocumentService Workflow => _workspace.Workflow;
    internal VatRates Rates { get; }
    internal Supplier? Supplier { get; private set; }
    public IReadOnlyList<Customer> Customers { get; private set; } = [];

    internal async Task LoadAsync()
    {
        Supplier = await _workspace.GetSupplierAsync();
        Customers = await _workspace.GetCustomersAsync();
        LoadHeader();
    }

    internal async Task<bool> ReloadAsync()
    {
        if (Document.Id != 0)
        {
            var current = await _workspace.FindDocumentAsync(Document.Id);
            if (current is null)
            {
                return false;
            }

            Document = current;
        }

        LoadHeader();
        return true;
    }

    internal DocumentEditor ForDocument(Document document) => new(document, _workspace, Rates);

    partial void OnIssueDateChanged(DateTime? value) => UpdateDraft();
    partial void OnDueDateChanged(DateTime? value) => UpdateDraft();
    partial void OnSupplyDateChanged(DateTime? value) => UpdateDraft();
    partial void OnCorrectionReasonChanged(string? value) => UpdateDraft();
    partial void OnSelectedCustomerChanged(Customer? value) => UpdateDraft();

    private void LoadHeader()
    {
        _loading = true;
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
        _loading = false;
    }

    private void UpdateDraft()
    {
        if (_loading || !Document.IsEditable)
        {
            return;
        }

        Document.IssueDate = ToDomain(IssueDate);
        Document.DueDate = ToDomain(DueDate);
        if (Document is not CreditNote)
        {
            Document.Customer = SelectedCustomer;
        }

        ApplySupplyDateAndReason();
        DraftChanged?.Invoke();
    }

    private void ApplySupplyDateAndReason()
    {
        if (Document is TaxInvoice invoice)
        {
            invoice.TaxableSupplyDate = ToDomain(SupplyDate);
        }
        else if (Document is CreditNote credit)
        {
            credit.TaxableSupplyDate = ToDomain(SupplyDate);
            credit.CorrectionReason = CorrectionReason?.Trim() ?? string.Empty;
        }
    }

    private static DateTime? ToCalendar(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);
    private static DateOnly? ToDomain(DateTime? date) => date is DateTime value ? DateOnly.FromDateTime(value) : null;
}
