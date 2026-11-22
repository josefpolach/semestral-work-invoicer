namespace Invoicing.App.ViewModels;

/// <summary>Holds the three main tabs.</summary>
public class MainWindowViewModel(SupplierViewModel supplier, CustomersViewModel customers, DocumentsViewModel documents)
{
    public SupplierViewModel Supplier { get; } = supplier;
    public CustomersViewModel Customers { get; } = customers;
    public DocumentsViewModel Documents { get; } = documents;
}
