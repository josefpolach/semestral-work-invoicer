using Invoicing.Core.Documents;

namespace Invoicing.Tests.Core;

public class DocumentNumberingTests
{
    [Theory]
    [InlineData(NumberSeries.Invoice, 1, "2026-0001")]
    [InlineData(NumberSeries.CreditNote, 12, "OD-2026-0012")]
    public void Format_KnownSeries_UsesExpectedPrefix(NumberSeries series, int sequence, string expected) =>
        Assert.Equal(expected, DocumentNumbering.Format(series, 2026, sequence));

    [Theory]
    [InlineData(null, 1)]
    [InlineData(41, 42)]
    public void NextSequenceNumber_NewOrExistingYear_ReturnsNextNumber(int? lastNumber, int expected) =>
        Assert.Equal(expected, DocumentNumbering.NextSequenceNumber(lastNumber));

    [Fact]
    public void ToVariableSymbol_InvoiceNumber_RemovesSeparator() =>
        Assert.Equal("20260001", DocumentNumbering.ToVariableSymbol("2026-0001"));

    [Theory]
    [InlineData(DocumentKind.TaxInvoice, NumberSeries.Invoice)]
    [InlineData(DocumentKind.NonVatInvoice, NumberSeries.Invoice)]
    [InlineData(DocumentKind.CreditNote, NumberSeries.CreditNote)]
    public void GetSeries_DocumentKind_SelectsCorrectSeries(DocumentKind kind, NumberSeries expected) =>
        Assert.Equal(expected, DocumentNumbering.GetSeries(kind));
}
