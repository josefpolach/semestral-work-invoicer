using System.Globalization;
using Dapper;
using Invoicing.Core.Email;
using Invoicing.Core.Repositories;

namespace Invoicing.Data;

/// <summary>Stores provider acceptance references for document e-mails.</summary>
public class DocumentEmailRepository(SqliteConnectionFactory factory) : IDocumentEmailRepository
{
    private const string TimestampFormat = "O";
    private const string Insert = """
        INSERT INTO document_emails (document_id, sent_to, sent_at, provider_request_id)
        SELECT @DocumentId, @SentTo, @SentAt, @ProviderRequestId
        WHERE NOT EXISTS (
            SELECT 1 FROM document_emails WHERE document_id = @DocumentId AND provider_request_id = @ProviderRequestId
        )
        """;
    private const string Latest = """
        SELECT sent_to, sent_at, provider_request_id FROM document_emails
        WHERE document_id = @DocumentId ORDER BY julianday(sent_at) DESC, id DESC LIMIT 1
        """;

    public async Task AddAsync(int documentId, DocumentEmailRecord record)
    {
        await using var connection = await factory.OpenConnectionAsync();
        await connection.ExecuteAsync(Insert, new
        {
            DocumentId = documentId,
            record.SentTo,
            record.ProviderRequestId,
            SentAt = record.SentAt.ToString(TimestampFormat, CultureInfo.InvariantCulture)
        });
    }

    public async Task<DocumentEmailRecord?> GetLatestAsync(int documentId)
    {
        await using var connection = await factory.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<DocumentEmailRow>(Latest, new { DocumentId = documentId });
        return row is null ? null : new(row.SentTo,
            DateTimeOffset.ParseExact(row.SentAt, TimestampFormat, CultureInfo.InvariantCulture), row.ProviderRequestId);
    }
}
