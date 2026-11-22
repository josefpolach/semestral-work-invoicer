using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class VatSummaryComponent(BillingDocument document) : IComponent
{
    public void Compose(IContainer container) => container.Column(column =>
    {
        if (document is CreditNote credit)
        {
            column.Item().Text($"{PdfTexts.Original}: {credit.OriginalInvoice?.Number}");
            column.Item().Text($"{PdfTexts.Reason}: {credit.CorrectionReason}");
            column.Item().Text(PdfTexts.CorrectionSummary).Bold();
            column.Item().Table(table => AddTable(table, credit.CalculateVatSummary()));
        }
        else if (document is TaxInvoice invoice)
        {
            column.Item().Text(PdfTexts.VatSummary).Bold();
            column.Item().Table(table => AddTable(table, invoice.CalculateVatSummary()));
        }
    });

    private static void AddTable(TableDescriptor table, VatSummary summary)
    {
        DefineColumnsAndHeader(table);
        foreach (var row in summary.Rows)
        {
            AddRow(table, row);
        }
    }

    private static void DefineColumnsAndHeader(TableDescriptor table)
    {
        string[] labels = [PdfTexts.Rate, PdfTexts.TaxBase, PdfTexts.Vat, PdfTexts.Total];
        table.ColumnsDefinition(columns =>
        {
            foreach (var label in labels)
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
    }

    private static void AddRow(TableDescriptor table, VatSummaryRow row)
    {
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(CzechFormat.FormatQuantity(row.VatRate) + " %");
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(CzechFormat.FormatMoney(row.TaxBase));
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(CzechFormat.FormatMoney(row.VatAmount));
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(CzechFormat.FormatMoney(row.Total));
    }
}
