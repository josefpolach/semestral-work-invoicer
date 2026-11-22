using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Invoicing.Pdf.Components;

internal class FooterComponent(string issuer) : IComponent
{
    public void Compose(IContainer container) => container.Row(row =>
    {
        row.RelativeItem().Text($"{PdfTexts.IssuedBy}: {issuer}");
        row.AutoItem().AlignRight().Text(text =>
        {
            text.Span(PdfTexts.Page);
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    });
}
