using Invoicing.Core.Documents;

namespace Invoicing.Pdf;

/// <summary>Provides ASCII attachment and export file names.</summary>
public static class PdfFileNames
{
    private const string CreditNotePrefix = "Opravny-doklad-";
    private const string InvoicePrefix = "Faktura-";
    private const string FileExtension = ".pdf";

    public static string For(Document document)
    {
        var number = document.Number ?? throw new InvalidOperationException("Missing document number.");
        return (document.Kind == DocumentKind.CreditNote ? CreditNotePrefix : InvoicePrefix) + number + FileExtension;
    }
}
