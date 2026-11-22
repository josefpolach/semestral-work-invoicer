using Invoicing.Core.Repositories;
using Invoicing.Core.Services;

namespace Invoicing.App.Services;

/// <summary>Groups document editing dependencies shared by the list and detail windows.</summary>
internal class DocumentWorkspace(ISupplierRepository suppliers, ICustomerRepository customers,
    IDocumentRepository documents, DocumentService workflow)
{
    public ISupplierRepository Suppliers { get; } = suppliers;
    public ICustomerRepository Customers { get; } = customers;
    public IDocumentRepository Documents { get; } = documents;
    public DocumentService Workflow { get; } = workflow;
}
