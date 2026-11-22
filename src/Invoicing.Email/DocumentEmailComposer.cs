using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Formatting;

namespace Invoicing.Email;

/// <summary>Renders e-mail content from the document snapshots.</summary>
public class DocumentEmailComposer(EmailTemplateStore store, EmailTemplateRenderer renderer)
{
    private const string SupplierName = "SupplierName";
    private const string VariableSymbol = "VariableSymbol";
    private const string DocumentNumber = "DocumentNumber";

    public EmailMessage Compose(Document document, string recipient, string fromAddress, EmailAttachment attachment)
    {
        var values = GetValues(document);
        if (document.Kind != DocumentKind.CreditNote)
        {
            values[VariableSymbol] = DocumentNumbering.ToVariableSymbol(values[DocumentNumber]);
        }

        return new(fromAddress, values[SupplierName], recipient,
            renderer.Render(store.Load(document.Kind, EmailTemplateStore.SubjectExtension), values, false).Trim(),
            renderer.Render(store.Load(document.Kind, EmailTemplateStore.HtmlExtension), values, true),
            renderer.Render(store.Load(document.Kind, EmailTemplateStore.TextExtension), values, false), attachment);
    }

    private static Dictionary<string, string> GetValues(Document document)
    {
        var supplier = document.Supplier ?? throw new InvalidOperationException("Missing supplier snapshot.");
        var customer = document.Customer ?? throw new InvalidOperationException("Missing customer snapshot.");
        var number = document.Number ?? throw new InvalidOperationException("Missing document number.");
        var dueDate = document.DueDate ?? throw new InvalidOperationException("Missing due date.");
        return new Dictionary<string, string>
        {
            [DocumentNumber] = number,
            [SupplierName] = supplier.Name,
            ["CustomerName"] = customer.Name,
            ["TotalAmount"] = CzechFormat.FormatMoney(document.CalculateTotal()),
            ["IssuedBy"] = supplier.IssuedBy,
            ["DueDate"] = CzechFormat.FormatDate(dueDate),
            ["BankAccount"] = supplier.BankAccount
        };
    }
}
