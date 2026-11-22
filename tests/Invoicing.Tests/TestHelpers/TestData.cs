using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;

namespace Invoicing.Tests.TestHelpers;

internal static class TestData
{
    public static DateOnly Today => new(2026, 11, 22);
    public static VatRates Rates => new(21, 12);
    public static InvoiceLine[] Lines =>
    [
        new("Consulting", 2, "h", 1000m, 21m, 0m),
        new("Service", 1, "h", 500.50m, 21m, 10m),
        new("Goods", 3, "pc", 120m, 12m, 0m)
    ];

    public static Supplier Supplier(bool vat = true) => new()
    {
        Name = "Žluťoučký kůň",
        Street = "Říční 10",
        City = "Praha",
        PostalCode = "14000",
        Ico = "27074358",
        Dic = "CZ27074358",
        IsVatPayer = vat,
        RegisterEntry = "Zápis v rejstříku",
        BankAccount = "19-2000145399/0800",
        IssuedBy = "Jiří Šťastný",
        DefaultDueDays = 14
    };

    public static Customer Customer() => new()
    {
        Name = "Odběratel Žluťoučký kůň",
        Street = "Říční 15",
        City = "Brno",
        PostalCode = "60200",
        Ico = "00006947",
        Dic = "CZ00006947",
        Email = "customer@example.com"
    };

    public static DocumentHeader Header(Customer? customer = null) =>
        DocumentHeader.NewDraft(customer ?? Customer(), Today, Today.AddDays(14));

    public static TaxInvoice Invoice(Customer? customer = null) => new(Header(customer), Lines, Today);

    public static Document Issued(DocumentKind kind)
    {
        var invoice = Invoice();
        invoice.AssignId(1);
        invoice.Issue("2026-0001", Supplier());
        if (kind == DocumentKind.TaxInvoice)
        {
            return invoice;
        }

        if (kind == DocumentKind.CreditNote)
        {
            var note = invoice.CreateCreditNote(Today, 14);
            note.CorrectionReason = "Oprava množství";
            note.Issue("OD-2026-0001", Supplier());
            return note;
        }

        var nonVat = new NonVatInvoice(Header(), Lines);
        nonVat.Issue("2026-0001", Supplier(false));
        return nonVat;
    }
}
