using Invoicing.Core.Documents;
using Invoicing.Pdf;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Pdf;

public class DocumentPdfGeneratorTests
{
    [Theory]
    [InlineData(DocumentKind.TaxInvoice, false, "Faktura-2026-0001.pdf")]
    [InlineData(DocumentKind.NonVatInvoice, false, "Faktura-2026-0001.pdf")]
    [InlineData(DocumentKind.NonVatInvoice, true, "Faktura-2026-0001.pdf")]
    [InlineData(DocumentKind.CreditNote, false, "Opravny-doklad-OD-2026-0001.pdf")]
    public void Generate_IssuedCzechDocuments_ProducesPdf(DocumentKind kind, bool cancel, string filename)
    {
        PdfSetup.Initialize();
        var document = TestData.Issued(kind);
        if (cancel)
        {
            document.Cancel();
        }

        var bytes = new DocumentPdfGenerator().Generate(document);
        Assert.True(bytes.AsSpan().StartsWith("%PDF"u8));
        Assert.Equal(filename, PdfFileNames.For(document));
    }

    [Fact]
    public void Generate_Draft_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new DocumentPdfGenerator().Generate(TestData.Invoice()));

    [Fact]
    public void Generate_InvoiceAboveQrAmountLimit_StillProducesPdf()
    {
        var document = new NonVatInvoice(TestData.Header(), [new("Sale", 1, "pc", 10_000_000m, 0, 0)]);
        document.Issue("2026-0001", TestData.Supplier(false));

        var bytes = new DocumentPdfGenerator().Generate(document);
        Assert.True(bytes.AsSpan().StartsWith("%PDF"u8));
    }
}
