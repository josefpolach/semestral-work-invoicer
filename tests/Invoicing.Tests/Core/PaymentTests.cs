using System.Globalization;
using Invoicing.Core.Documents;
using Invoicing.Core.Payments;
using Invoicing.Core.Validation;

namespace Invoicing.Tests.Core;

public class PaymentTests
{
    [Theory]
    [InlineData("19-2000145399/0800", "CZ6508000000192000145399")]
    [InlineData("2000145399/0800", "CZ7908000000002000145399")]
    [InlineData("123456789/0100", "CZ1801000000000123456789")]
    [InlineData("2400123456/2010", "CZ6020100000002400123456")]
    public void ToIban_ValidAccount_CalculatesCheckDigits(string value, string expected)
    {
        Assert.True(CzechBankAccount.TryParse(value, out var account));
        Assert.Equal(expected, account.ToIban());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1/0800")]
    [InlineData("2000145399/800")]
    [InlineData("12345678901/0800")]
    [InlineData("1234567-2000145399/0800")]
    [InlineData("abc/0800")]
    public void TryParse_MalformedAccount_ReturnsFalse(string value) => Assert.False(CzechBankAccount.TryParse(value, out _));

    [Theory]
    [InlineData("27074358", true)]
    [InlineData("00006947", true)]
    [InlineData("26168685", true)]
    [InlineData("27082440", true)]
    [InlineData("27074359", false)]
    [InlineData("12345678", false)]
    [InlineData("1234567", false)]
    [InlineData("123456789", false)]
    [InlineData("2707435A", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_CompanyNumbers_ChecksDigitsAndChecksum(string? value, bool expected) =>
        Assert.Equal(expected, IcoValidator.IsValid(value));

    [Fact]
    public void DocumentNumbering_FormatsBothSeriesAndVariableSymbol()
    {
        Assert.Equal("2026-0001", DocumentNumbering.Format(NumberSeries.Invoice, 2026, 1));
        Assert.Equal("OD-2026-0012", DocumentNumbering.Format(NumberSeries.CreditNote, 2026, 12));
        Assert.Equal(1, DocumentNumbering.NextSequenceNumber(null));
        Assert.Equal(42, DocumentNumbering.NextSequenceNumber(41));
        Assert.Equal("20260001", DocumentNumbering.ToVariableSymbol("2026-0001"));
        Assert.Equal(NumberSeries.Invoice, DocumentNumbering.GetSeries(DocumentKind.TaxInvoice));
        Assert.Equal(NumberSeries.Invoice, DocumentNumbering.GetSeries(DocumentKind.NonVatInvoice));
        Assert.Equal(NumberSeries.CreditNote, DocumentNumbering.GetSeries(DocumentKind.CreditNote));
    }

    [Fact]
    public void ToText_ValidPayment_UsesInvariantCultureAndNormalizesMessage()
    {
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
            var payment = new SpaydPayment("CZ6508000000192000145399", 3368.24m, "20260001",
                new(2026, 12, 6), "Faktura 2026-0001");
            Assert.Equal("SPD*1.0*ACC:CZ6508000000192000145399*AM:3368.24*CC:CZK*X-VS:20260001*DT:20261206*MSG:FAKTURA 2026-0001*",
                payment.ToText());
            var normalized = new SpaydPayment("CZ6508000000192000145399", 100, "1", new(2026, 12, 6), "Faktura č. *2026-0001");
            Assert.Contains("*AM:100.00*", normalized.ToText());
            Assert.Equal("MSG:FAKTURA C. 2026-0001", normalized.ToText().Split('*')[^2]);
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }

    [Fact]
    public void ToText_LongMessage_CutsToSixtyCharacters()
    {
        var payment = new SpaydPayment("CZ6508000000192000145399", 100, "1", new(2026, 12, 6), new('a', 70));
        Assert.Equal("MSG:" + new string('A', 60), payment.ToText().Split('*')[^2]);
    }

    [Theory]
    [InlineData("CZ6508000000192000145399", "10000000", "1")]
    [InlineData("CZ6508000000192000145399", "-1", "1")]
    [InlineData("CZ6508000000192000145399", "1", "A1")]
    [InlineData("CZ6508000000192000145399", "1", "12345678901")]
    [InlineData("CZ12345678901234567890123456789012345678901234567890", "1", "1")]
    public void ToText_InvalidInput_Throws(string iban, string amount, string symbol) =>
        Assert.Throws<ArgumentException>(() => new SpaydPayment(iban, Parse(amount), symbol, new(2026, 12, 6), "Invoice").ToText());

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
