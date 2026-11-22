using Invoicing.Core.Documents;
using Invoicing.Core.Payments;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class QrPaymentComponent(BillingDocument document) : IComponent
{
    private const float WidthInCentimeters = 3.5f;

    public void Compose(IContainer container)
    {
        var amount = document.CalculateTotal();
        if (amount > SpaydPayment.MaxAmount)
        {
            container.Text(PdfTexts.QrTooLarge);
            return;
        }

        var png = CreateImage(amount);
        container.Column(column =>
        {
            column.Item().Text(PdfTexts.QrPayment).Bold();
            column.Item().Width(WidthInCentimeters, Unit.Centimetre).Image(png);
        });
    }

    private byte[] CreateImage(decimal amount)
    {
        var number = document.Number ?? throw new InvalidOperationException("Missing document number.");
        var iban = document.Supplier?.Iban ?? throw new InvalidOperationException("Missing IBAN.");
        var due = document.DueDate ?? throw new InvalidOperationException("Missing due date.");
        var payment = new SpaydPayment(iban, amount, DocumentNumbering.ToVariableSymbol(number), due,
            $"{PdfTexts.Invoice} {number}");
        return new QrCodeImageGenerator().CreatePng(payment.ToText());
    }
}
