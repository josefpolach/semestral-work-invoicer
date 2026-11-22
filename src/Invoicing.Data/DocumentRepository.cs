using Dapper;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;
using Invoicing.Data.Mappings;
using Invoicing.Data.Rows;
using Microsoft.Data.Sqlite;

namespace Invoicing.Data;

/// <summary>Stores document lines, snapshots and sequence numbers together.</summary>
public class DocumentRepository(SqliteConnectionFactory factory) : IDocumentRepository
{
    public Task<IReadOnlyList<Document>> GetAllAsync() => ReadAsync(null);

    public async Task<Document?> GetByIdAsync(int id) => (await ReadAsync(id)).SingleOrDefault();

    public async Task SaveDraftAsync(Document document)
    {
        RequireDraft(document);
        await using var connection = await factory.OpenConnectionAsync();
        await using var transaction = connection.BeginTransaction();
        var id = await SaveAsync(transaction, document);
        await transaction.CommitAsync();
        AssignNewId(document, id);
    }

    public async Task IssueAsync(Document document, Supplier supplier)
    {
        RequireDraft(document);
        var issueDate = document.IssueDate ?? throw new InvalidOperationException("The issue date is required.");
        await using var connection = await factory.OpenConnectionAsync();
        // The immediate transaction serializes the sequence read and write.
        await using var transaction = connection.BeginTransaction();
        var number = await TakeNumberAsync(transaction, document.Kind, issueDate.Year);
        var issued = CopyDraft(document);
        issued.Issue(number, supplier);
        var id = await SaveAsync(transaction, issued);
        await transaction.CommitAsync();
        AssignNewId(document, id);
        document.Issue(number, supplier);
    }

    public async Task UpdateStatusAsync(Document document)
    {
        if (document.Status is not (DocumentStatus.Paid or DocumentStatus.Cancelled))
        {
            throw new InvalidOperationException("Only payment or cancellation can update an issued document.");
        }

        await using var connection = await factory.OpenConnectionAsync();
        EnsureChanged(await connection.ExecuteAsync(DocumentQueries.UpdateStatus,
            new { document.Id, Status = document.Status.ToString() }));
    }

    public async Task DeleteDraftAsync(Document document)
    {
        RequireDraft(document);
        await using var connection = await factory.OpenConnectionAsync();
        EnsureChanged(await connection.ExecuteAsync(DocumentQueries.DeleteDraft, new { document.Id }));
    }

    private async Task<IReadOnlyList<Document>> ReadAsync(int? id)
    {
        await using var connection = await factory.OpenConnectionAsync();
        await using var transaction = connection.BeginTransaction(deferred: true);
        var parameters = new { Id = id };
        var documents = await connection.QueryAsync<DocumentRow>(DocumentQueries.ReadDocuments, parameters, transaction);
        var lines = await connection.QueryAsync<DocumentLineRow>(DocumentQueries.ReadLines, parameters, transaction);
        await transaction.CommitAsync();
        var linesByDocument = lines.ToLookup(line => line.DocumentId);
        return documents.Select(row => DocumentMapper.Read(row, linesByDocument[row.Id])).ToList();
    }

    private static async Task<string> TakeNumberAsync(SqliteTransaction transaction, DocumentKind kind, int year)
    {
        var connection = GetConnection(transaction);
        var series = DocumentNumbering.GetSeries(kind);
        var last = await connection.QuerySingleOrDefaultAsync<int?>(DocumentQueries.ReadSequence,
            new { Series = series.ToString(), Year = year }, transaction);
        var next = DocumentNumbering.NextSequenceNumber(last);
        var number = DocumentNumbering.Format(series, year, next);
        if (series == NumberSeries.Invoice)
        {
            DocumentNumbering.ToVariableSymbol(number);
        }

        await connection.ExecuteAsync(DocumentQueries.WriteSequence,
            new { Series = series.ToString(), Year = year, LastNumber = next }, transaction);
        return number;
    }

    private static async Task<int> SaveAsync(SqliteTransaction transaction, Document document)
    {
        var connection = GetConnection(transaction);
        var row = DocumentMapper.Write(document);
        var id = row.Id;
        if (id == 0)
        {
            id = await connection.QuerySingleAsync<int>(DocumentQueries.Insert, row, transaction);
        }
        else
        {
            EnsureChanged(await connection.ExecuteAsync(DocumentQueries.Update, row, transaction));
        }

        await connection.ExecuteAsync(DocumentQueries.DeleteLines, new { Id = id }, transaction);
        var lines = document.Lines.Select((line, index) => DocumentLineMapper.Write(id, index + 1, line));
        await connection.ExecuteAsync(DocumentQueries.InsertLines, lines, transaction);
        return id;
    }

    private static Document CopyDraft(Document document)
    {
        var header = new DocumentHeader
        {
            Id = document.Id,
            Customer = document.Customer,
            IssueDate = document.IssueDate,
            DueDate = document.DueDate
        };
        return document switch
        {
            TaxInvoice invoice => new TaxInvoice(header, invoice.Lines, invoice.TaxableSupplyDate),
            NonVatInvoice invoice => new NonVatInvoice(header, invoice.Lines),
            CreditNote credit => CreditNote.Restore(header, credit.Lines, credit.TaxableSupplyDate,
                new(credit.OriginalInvoice, credit.CorrectionReason)),
            _ => throw new InvalidOperationException("Unknown document type.")
        };
    }

    private static SqliteConnection GetConnection(SqliteTransaction transaction) =>
        transaction.Connection ?? throw new InvalidOperationException("Missing transaction connection.");

    private static void AssignNewId(Document document, int id)
    {
        if (document.Id == 0)
        {
            document.AssignId(id);
        }
    }

    private static void RequireDraft(Document document)
    {
        if (!document.IsEditable)
        {
            throw new InvalidOperationException("Only draft documents can be written.");
        }
    }

    private static void EnsureChanged(int affected)
    {
        if (affected != 1)
        {
            throw new InvalidOperationException("The document no longer has the expected status.");
        }
    }
}
