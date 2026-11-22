using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class VatSummaryComponent(BillingDocument document) : IComponent
{
    private const uint ColumnCount = 4;
    private const int HeadingFontSize = 12;

    public void Compose(IContainer container) => container.Column(column =>
    {
        if (document is CreditNote credit)
        {
            column.Item().Text($"{PdfTexts.Original}: {credit.OriginalInvoice?.Number}");
            column.Item().Text($"{PdfTexts.Reason}: {credit.CorrectionReason}");
            AddSummary(column, PdfTexts.CorrectionSummary, credit.CalculateVatSummary());
        }
        else if (document is TaxInvoice invoice)
        {
            AddSummary(column, PdfTexts.VatSummary, invoice.CalculateVatSummary());
        }
    });

    private static void AddSummary(ColumnDescriptor column, string title, VatSummary summary) =>
        column.Item().PreventPageBreak().Table(table => AddTable(table, title, summary));

    private static void AddTable(TableDescriptor table, string title, VatSummary summary)
    {
        DefineColumnsAndHeader(table, title);
        foreach (var row in summary.Rows)
        {
            AddRow(table, row);
        }

        AddTotalRow(table, summary);
    }

    private static void DefineColumnsAndHeader(TableDescriptor table, string title)
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
            header.Cell().ColumnSpan(ColumnCount).Background(Colors.Grey.Lighten3).Padding(PdfTableStyle.CellPadding)
                .Text(title).FontSize(HeadingFontSize).Bold();
            foreach (var label in labels)
            {
                var cell = header.Cell().BorderBottom(PdfTableStyle.HeaderBorder).Padding(PdfTableStyle.CellPadding);
                if (label != PdfTexts.Rate)
                {
                    cell = cell.AlignRight();
                }

                cell.Text(label).Bold();
            }
        });
    }

    private static void AddRow(TableDescriptor table, VatSummaryRow row)
    {
        table.Cell().Padding(PdfTableStyle.CellPadding).Text(CzechFormat.FormatQuantity(row.VatRate) + " %");
        AmountCell(table).Text(CzechFormat.FormatMoney(row.TaxBase));
        AmountCell(table).Text(CzechFormat.FormatMoney(row.VatAmount));
        AmountCell(table).Text(CzechFormat.FormatMoney(row.Total));
    }

    private static void AddTotalRow(TableDescriptor table, VatSummary summary)
    {
        TotalCell(table).Text(PdfTexts.Total).Bold();
        TotalCell(table).AlignRight().Text(CzechFormat.FormatMoney(summary.TotalBase)).Bold();
        TotalCell(table).AlignRight().Text(CzechFormat.FormatMoney(summary.TotalVat)).Bold();
        TotalCell(table).AlignRight().Text(CzechFormat.FormatMoney(summary.Total)).Bold();
    }

    private static IContainer AmountCell(TableDescriptor table) =>
        table.Cell().Padding(PdfTableStyle.CellPadding).AlignRight();

    private static IContainer TotalCell(TableDescriptor table) =>
        table.Cell().BorderTop(PdfTableStyle.HeaderBorder).Padding(PdfTableStyle.CellPadding);
}
