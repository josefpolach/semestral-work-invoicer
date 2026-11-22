using Invoicing.App.Services;
using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;

namespace Invoicing.App.ViewModels;

/// <summary>Holds a document and the data needed to edit it.</summary>
internal class DocumentEditor(Document document, DocumentWorkspace workspace, VatRates rates)
{
    public Document Document { get; private set; } = document;
    public DocumentWorkspace Workspace { get; } = workspace;
    public VatRates Rates { get; } = rates;
    public Supplier? Supplier { get; private set; }
    public IReadOnlyList<Customer> Customers { get; private set; } = [];

    public async Task LoadAsync()
    {
        Supplier = await Workspace.Suppliers.GetAsync();
        Customers = await Workspace.Customers.GetAllAsync();
    }

    public async Task ReloadAsync()
    {
        if (Document.Id != 0 && await Workspace.Documents.GetByIdAsync(Document.Id) is Document current)
        {
            Document = current;
        }
    }
}
