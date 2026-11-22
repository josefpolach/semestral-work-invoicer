using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Services;

/// <summary>Coordinates document workflow and persistence.</summary>
public class DocumentService(ISupplierRepository suppliers, IDocumentRepository documents, VatRates vatRates)
{
    public async Task<Document> CreateDraftAsync(DateOnly today)
    {
        var supplier = await RequireSupplierAsync();
        var header = DocumentHeader.NewDraft(null, today, today.AddDays(supplier.DefaultDueDays));
        return supplier.IsVatPayer ? new TaxInvoice(header, [], today) : new NonVatInvoice(header, []);
    }

    public Task SaveDraftAsync(Document document) => documents.SaveDraftAsync(document);

    public async Task<IReadOnlyList<ValidationMessage>> IssueAsync(Document document)
    {
        var supplier = await suppliers.GetAsync();
        var messages = document.Validate(supplier, vatRates);
        if (supplier is not null && messages.All(message => message.Severity != ValidationSeverity.Error))
        {
            await documents.IssueAsync(document, supplier);
        }

        return messages;
    }

    public async Task MarkAsPaidAsync(Document document)
    {
        document.MarkAsPaid();
        await documents.UpdateStatusAsync(document);
    }

    public async Task CancelAsync(Document document)
    {
        document.Cancel();
        await documents.UpdateStatusAsync(document);
    }

    public async Task<CreditNote> CreateCreditNoteAsync(TaxInvoice invoice, DateOnly today)
    {
        var supplier = await RequireSupplierAsync();
        return invoice.CreateCreditNote(today, supplier.DefaultDueDays);
    }

    public Task DeleteDraftAsync(Document document) => document.IsEditable
        ? documents.DeleteDraftAsync(document)
        : throw new InvalidOperationException("Only drafts can be deleted.");

    private async Task<Supplier> RequireSupplierAsync() =>
        await suppliers.GetAsync() ?? throw new InvalidOperationException("A supplier must be configured first.");
}
