using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using BillingDocument = Invoicing.Core.Documents.Document;

namespace Invoicing.Pdf.Components;

internal class PartiesComponent(BillingDocument document) : IComponent
{
    private const int PartySpacing = 16;

    public void Compose(IContainer container) => container.Row(row =>
    {
        row.Spacing(PartySpacing);
        row.RelativeItem().Column(column => AddParty(column, PdfTexts.Supplier, document.Supplier,
            document.Kind != DocumentKind.NonVatInvoice));
        row.RelativeItem().Column(column => AddParty(column, PdfTexts.Customer, document.Customer, true));
    });

    private static void AddParty(ColumnDescriptor column, string title, Party? party, bool showDic)
    {
        if (party is null)
        {
            throw new InvalidOperationException("Missing party snapshot.");
        }

        column.Item().Text(title).Bold();
        column.Item().Text(party.Name);
        column.Item().Text(party.Street);
        column.Item().Text($"{party.PostalCode} {party.City}");
        AddIdentifiers(column, party, showDic);
        if (party is Supplier supplier)
        {
            column.Item().Text(supplier.RegisterEntry);
        }
    }

    private static void AddIdentifiers(ColumnDescriptor column, Party party, bool showDic)
    {
        if (!string.IsNullOrWhiteSpace(party.Ico))
        {
            column.Item().Text($"{PdfTexts.CompanyNumber}: {party.Ico}");
        }

        if (showDic && !string.IsNullOrWhiteSpace(party.Dic))
        {
            column.Item().Text($"{PdfTexts.TaxNumber}: {party.Dic}");
        }
    }
}
