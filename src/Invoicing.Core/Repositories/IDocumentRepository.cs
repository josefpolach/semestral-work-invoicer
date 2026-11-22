using Invoicing.Core.Documents;
using Invoicing.Core.Parties;

namespace Invoicing.Core.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(int id);
    Task SaveDraftAsync(Document document);
    Task IssueAsync(Document document, Supplier supplier);
    Task UpdateStatusAsync(Document document);
    Task DeleteDraftAsync(Document document);
}
