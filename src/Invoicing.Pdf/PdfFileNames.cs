using Invoicing.Core.Documents;

namespace Invoicing.Pdf;

/// <summary>Provides ASCII attachment and export file names.</summary>
public static class PdfFileNames
{
    public static string For(Document document)
    {
        var number = document.Number ?? throw new InvalidOperationException("Missing document number.");
        return (document.Kind == DocumentKind.CreditNote ? "Opravny-doklad-" : "Faktura-") + number + ".pdf";
    }
}
