using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;

namespace Invoicing.App.Texts;

internal static class DocumentTexts
{
    public const string Draft = "Koncept";
    public const string SupplierRequired = "Nejdříve vyplňte údaje dodavatele.";
    public const string InvalidNumber = "Neplatné číslo";

    public static string Kind(DocumentKind kind) => kind switch
    {
        DocumentKind.TaxInvoice => "Faktura – daňový doklad",
        DocumentKind.NonVatInvoice => "Faktura",
        DocumentKind.CreditNote => "Opravný daňový doklad",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string Status(Document document, DateOnly today) => document.Status switch
    {
        DocumentStatus.Draft => Draft,
        DocumentStatus.Issued when document.IsOverdue(today) => "Po splatnosti",
        DocumentStatus.Issued => "Vystaveno",
        DocumentStatus.Paid => "Zaplaceno",
        DocumentStatus.Cancelled => "Stornováno",
        _ => throw new ArgumentOutOfRangeException(nameof(document))
    };

    public static string Date(DateOnly? date) => date is DateOnly value ? CzechFormat.FormatDate(value) : string.Empty;
}
