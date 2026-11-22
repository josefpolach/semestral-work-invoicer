using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class TotalsComponent(BillingDocument document) : IComponent
{
    public void Compose(IContainer container) => container.Column(column =>
    {
        var label = document.Kind == DocumentKind.CreditNote ? PdfTexts.CorrectionTotal : PdfTexts.TotalToPay;
        column.Item().AlignRight().Text($"{label}: {CzechFormat.FormatMoney(document.CalculateTotal())}").FontSize(14).Bold();
        if (document.Kind == DocumentKind.NonVatInvoice)
        {
            column.Item().Text(PdfTexts.NonPayer);
        }
    });
}
