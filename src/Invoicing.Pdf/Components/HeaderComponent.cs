using Invoicing.Core.Documents;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class HeaderComponent(BillingDocument document) : IComponent
{
    public void Compose(IContainer container) => container.Column(column =>
    {
        column.Item().Text($"{PdfTexts.Title(document.Kind)} {PdfTexts.Number} {document.Number}").FontSize(22).Bold();
        if (document.Status == DocumentStatus.Cancelled)
        {
            column.Item().Text(PdfTexts.Cancelled).FontSize(16).Bold();
        }
    });
}
