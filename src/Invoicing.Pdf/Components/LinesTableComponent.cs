using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class LinesTableComponent(BillingDocument document) : IComponent
{
    private const int DescriptionColumnWeight = 3;

    private bool HasVat => document.Kind != DocumentKind.NonVatInvoice;

    public void Compose(IContainer container) => container.Table(table =>
    {
        var labels = GetLabels();
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(DescriptionColumnWeight);
            foreach (var label in labels.Skip(1))
            {
                columns.RelativeColumn();
            }
        });
        table.Header(header =>
        {
            foreach (var label in labels)
            {
                header.Cell().BorderBottom(PdfTableStyle.HeaderBorder).Padding(PdfTableStyle.CellPadding).Text(label).Bold();
            }
        });
        foreach (var line in document.Lines)
        {
            AddLine(table, line);
        }
    });

    private string[] GetLabels() => HasVat
        ? [PdfTexts.Description, PdfTexts.Quantity, PdfTexts.Unit, PdfTexts.NetUnitPrice, PdfTexts.Discount,
            PdfTexts.VatRate, PdfTexts.Base]
        : [PdfTexts.Description, PdfTexts.Quantity, PdfTexts.Unit, PdfTexts.UnitPrice, PdfTexts.Discount, PdfTexts.Total];

    private void AddLine(TableDescriptor table, InvoiceLine line)
    {
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(line.Description);
        table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight().Text(CzechFormat.FormatQuantity(line.Quantity));
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(line.Unit);
        table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight().Text(CzechFormat.FormatMoney(line.UnitPriceWithoutVat));
        table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight().Text(CzechFormat.FormatQuantity(line.DiscountPercent));
        if (HasVat)
        {
            table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight().Text(CzechFormat.FormatQuantity(line.VatRate) + " %");
        }

        table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight().Text(CzechFormat.FormatMoney(line.CalculateBase()));
    }
}
