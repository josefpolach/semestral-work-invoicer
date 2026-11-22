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
    private readonly HttpClient _aresHttp = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly HttpClient _lettrHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<MainWindow> CreateMainAsync()
    {
        var window = new MainWindow();
        var dialogs = new DialogService(window);
        var database = new SqliteConnectionFactory(databasePath);
        var supplierRepository = new SupplierRepository(database);
        var customerRepository = new CustomerRepository(database);
        var documentRepository = new DocumentRepository(database);
        var emailRepository = new DocumentEmailRepository(database);
        var lookup = new CompanyLookup(new AresCompanyRegistry(_aresHttp, settings.AresBaseUrl.AbsoluteUri), dialogs);
        var supplier = new SupplierViewModel(supplierRepository, lookup, dialogs, settings.Email);
        var customers = new CustomersViewModel(customerRepository, lookup);
        var documentService = new DocumentService(supplierRepository, documentRepository, settings.VatRates);
        var delivery = CreateDelivery(emailRepository);
        var workspace = new DocumentWorkspace(supplierRepository, customerRepository, documentRepository, documentService);
        var documents = new DocumentsViewModel(workspace, delivery, dialogs, settings.VatRates);
        supplier.Saved += documents.EnableNewDocument;
        window.DataContext = new MainWindowViewModel(supplier, customers, documents);
        await supplier.LoadAsync();
        await customers.LoadAsync();
        await documents.LoadAsync();
        return window;
    }

    private DocumentDelivery CreateDelivery(DocumentEmailRepository emails)
    {
        var sender = new LettrEmailSender(_lettrHttp, settings.LettrBaseUrl.AbsoluteUri, settings.Email.ApiKey);
        return new(settings.Email, new DocumentPdfGenerator(), emails,
            new(new EmailTemplateStore(), new EmailTemplateRenderer()), new(sender, emails));
    }

    public void Dispose()
    {
        _aresHttp.Dispose();
        _lettrHttp.Dispose();
    }
}
