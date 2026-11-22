using Invoicing.App.Services;
using Invoicing.App.ViewModels;
using Invoicing.App.Views;
using Invoicing.Ares;
using Invoicing.Core.Configuration;
using Invoicing.Core.Services;
using Invoicing.Data;
using Invoicing.Email;
using Invoicing.Pdf;

namespace Invoicing.App;

/// <summary>Builds and owns the desktop application's long-lived services.</summary>
internal class CompositionRoot(AppSettings settings, string databasePath) : IDisposable
{
    private const int AresTimeoutSeconds = 10;
    private const int LettrTimeoutSeconds = 30;
    private readonly HttpClient _aresHttp = new() { Timeout = TimeSpan.FromSeconds(AresTimeoutSeconds) };
    private readonly HttpClient _lettrHttp = new() { Timeout = TimeSpan.FromSeconds(LettrTimeoutSeconds) };

    public async Task<MainWindow> CreateMainAsync()
    {
        var window = new MainWindow();
        var dialogs = new DialogService(window);
        var model = CreateMainModel(dialogs);
        await model.Supplier.LoadAsync();
        await model.Customers.LoadAsync();
        await model.Documents.LoadAsync();
        window.DataContext = model;
        return window;
    }

    private MainWindowViewModel CreateMainModel(IDialogService dialogs)
    {
        var database = new SqliteConnectionFactory(databasePath);
        var supplierRepository = new SupplierRepository(database);
        var customerRepository = new CustomerRepository(database);
        var lookup = new CompanyLookup(new AresCompanyRegistry(_aresHttp, settings.AresBaseUrl.AbsoluteUri), dialogs);
        var supplier = new SupplierViewModel(supplierRepository, lookup, dialogs, settings.Email);
        var customers = new CustomersViewModel(customerRepository, lookup);
        var documents = CreateDocuments(database, supplierRepository, customerRepository, dialogs);
        supplier.Saved += documents.EnableNewDocument;
        return new(supplier, customers, documents);
    }

    private DocumentsViewModel CreateDocuments(SqliteConnectionFactory database, SupplierRepository suppliers,
        CustomerRepository customers, IDialogService dialogs)
    {
        var documents = new DocumentRepository(database);
        var workflow = new DocumentService(suppliers, documents, settings.VatRates);
        var workspace = new DocumentWorkspace(suppliers, customers, documents, workflow);
        var delivery = CreateDelivery(new DocumentEmailRepository(database));
        return new(workspace, delivery, dialogs, settings.VatRates);
    }

    private DocumentDelivery CreateDelivery(DocumentEmailRepository emails)
    {
        var sender = new LettrEmailSender(_lettrHttp, settings.LettrBaseUrl.AbsoluteUri, settings.Email.ApiKey);
        return new(settings.Email, new DocumentPdfGenerator(), emails, sender);
    }

    public void Dispose()
    {
        _aresHttp.Dispose();
        _lettrHttp.Dispose();
    }
}
