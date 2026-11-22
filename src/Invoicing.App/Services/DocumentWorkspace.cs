using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;
using Invoicing.Core.Services;

namespace Invoicing.App.Services;

/// <summary>Loads the records needed by the document list and editor.</summary>
internal class DocumentWorkspace(ISupplierRepository suppliers, ICustomerRepository customers,
    IDocumentRepository documents, DocumentService workflow)
{
    public DocumentService Workflow { get; } = workflow;

    public Task<Supplier?> GetSupplierAsync() => suppliers.GetAsync();
    public Task<IReadOnlyList<Customer>> GetCustomersAsync() => customers.GetAllAsync();
    public Task<IReadOnlyList<Document>> GetDocumentsAsync() => documents.GetAllAsync();
    public Task<Document?> FindDocumentAsync(int id) => documents.GetByIdAsync(id);
}
