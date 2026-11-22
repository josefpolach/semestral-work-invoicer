using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class DatesAndPaymentComponent(BillingDocument document) : IComponent
{
    public void Compose(IContainer container) => container.Column(column =>
    {
        AddDate(column, PdfTexts.IssueDate, document.IssueDate);
        var supplyDate = document switch
        {
            TaxInvoice invoice => invoice.TaxableSupplyDate,
            CreditNote credit => credit.TaxableSupplyDate,
            _ => null
        };
        AddDate(column, PdfTexts.SupplyDate, supplyDate);
        AddDate(column, PdfTexts.DueDate, document.DueDate);
        column.Item().Text(PdfTexts.Payment);
        if (document.Kind != DocumentKind.CreditNote)
        {
            AddBankDetails(column);
        }
    });

    private void AddBankDetails(ColumnDescriptor column)
    {
        var supplier = document.Supplier ?? throw new InvalidOperationException("Missing supplier snapshot.");
        var number = document.Number ?? throw new InvalidOperationException("Missing document number.");
        column.Item().Text($"{PdfTexts.Account}: {supplier.BankAccount}");
        column.Item().Text($"{PdfTexts.Iban}: {supplier.Iban}");
        column.Item().Text($"{PdfTexts.VariableSymbol}: {DocumentNumbering.ToVariableSymbol(number)}");
    }

    private static void AddDate(ColumnDescriptor column, string label, DateOnly? date)
    {
        if (date is DateOnly value)
        {
            column.Item().Text($"{label}: {CzechFormat.FormatDate(value)}");
        }
    }
}
