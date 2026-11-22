using QuestPDF.Fluent;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf;

/// <summary>Renders an issued document as PDF bytes.</summary>
public class DocumentPdfGenerator
{
    public byte[] Generate(BillingDocument document)
    {
        if (document.IsEditable)
        {
            throw new InvalidOperationException("Draft documents cannot be exported.");
        }

        PdfSetup.Initialize();
        return new DocumentPdf(document).GeneratePdf();
    }
}
