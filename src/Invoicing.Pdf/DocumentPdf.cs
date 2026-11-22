using Invoicing.Core.Documents;
using Invoicing.Pdf.Components;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf;

internal class DocumentPdf(BillingDocument document) : IDocument
{
    private const int Margin = 36;
    private const int FontSize = 10;
    private const int SectionSpacing = 12;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(Margin);
            page.DefaultTextStyle(style => style.FontFamily(PdfSetup.FontFamily).FontSize(FontSize));
            page.Content().Column(ComposeContent);
            page.Footer().Component(new FooterComponent(document.Supplier?.IssuedBy ?? string.Empty));
        });
    }

    private void ComposeContent(ColumnDescriptor column)
    {
        column.Spacing(SectionSpacing);
        column.Item().Component(new HeaderComponent(document));
        column.Item().Component(new PartiesComponent(document));
        column.Item().Component(new DatesAndPaymentComponent(document));
        column.Item().Component(new LinesTableComponent(document));
        if (document.Kind != DocumentKind.NonVatInvoice)
        {
            column.Item().Component(new VatSummaryComponent(document));
        }

        column.Item().Component(new TotalsComponent(document));
        if (document.Kind != DocumentKind.CreditNote && document.Status != DocumentStatus.Cancelled)
        {
            column.Item().Component(new QrPaymentComponent(document));
        }
    }
}
