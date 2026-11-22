using Invoicing.Core.Email;

namespace Invoicing.Core.Repositories;

public interface IDocumentEmailRepository
{
    Task AddAsync(int documentId, DocumentEmailRecord record);
    Task<DocumentEmailRecord?> GetLatestAsync(int documentId);
}
