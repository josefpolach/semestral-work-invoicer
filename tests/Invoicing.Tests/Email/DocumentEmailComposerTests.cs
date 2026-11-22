using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Email;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Email;

public class DocumentEmailComposerTests
{
    [Theory]
    [InlineData(DocumentKind.TaxInvoice)]
    [InlineData(DocumentKind.NonVatInvoice)]
    [InlineData(DocumentKind.CreditNote)]
    public void Compose_EachDocumentKind_ReplacesEveryPlaceholder(DocumentKind kind)
    {
        var message = Compose(kind);
        Assert.DoesNotContain("{{", message.Subject + message.HtmlBody + message.TextBody);
    }

    [Theory]
    [InlineData(DocumentKind.TaxInvoice, "Faktura 2026-0001 – ")]
    [InlineData(DocumentKind.NonVatInvoice, "Faktura 2026-0001 – ")]
    [InlineData(DocumentKind.CreditNote, "Opravný daňový doklad OD-2026-0001 – ")]
    public void Compose_EachDocumentKind_UsesExpectedSubject(DocumentKind kind, string prefix) =>
        Assert.StartsWith(prefix, Compose(kind).Subject);

    [Fact]
    public void Compose_TaxInvoice_FormatsCzechTotal() =>
        Assert.Contains("3\u00a0368,24\u00a0Kč", Compose(DocumentKind.TaxInvoice).TextBody);

    [Fact]
    public void Compose_CreditNote_OmitsVariableSymbol() =>
        Assert.DoesNotContain("Variabilní symbol", Compose(DocumentKind.CreditNote).TextBody);

    private static EmailMessage Compose(DocumentKind kind)
    {
        var composer = new DocumentEmailComposer(new(), new());
        return composer.Compose(TestData.Issued(kind), "to@example.com", "from@example.com",
            new("test.pdf", "application/pdf", []));
    }
}
