using System.Globalization;
using Invoicing.Core.Payments;

namespace Invoicing.Tests.Core;

public class SpaydPaymentTests
{
    [Fact]
    public void ToText_CzechCulture_UsesInvariantPaymentFields()
    {
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
            var payment = new SpaydPayment("CZ6508000000192000145399", 3368.24m, "20260001",
                new(2026, 12, 6), "Faktura 2026-0001");
            Assert.Equal("SPD*1.0*ACC:CZ6508000000192000145399*AM:3368.24*CC:CZK*X-VS:20260001*DT:20261206*MSG:FAKTURA 2026-0001*",
                payment.ToText());
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }

    [Fact]
    public void ToText_WholeAmount_WritesTwoDecimalPlaces()
    {
        var payment = new SpaydPayment("CZ6508000000192000145399", 100, "1", new(2026, 12, 6), "Faktura");
        Assert.Contains("*AM:100.00*", payment.ToText());
    }

    [Theory]
    [InlineData("Faktura č. 2026-0001")]
    [InlineData("Faktura č. *2026-0001")]
    public void ToText_SpecialCharactersInMessage_WritesCleanSpaydField(string message)
    {
        var payment = new SpaydPayment("CZ6508000000192000145399", 100, "1", new(2026, 12, 6), message);
        var fields = payment.ToText().Split('*');
        Assert.Equal("MSG:FAKTURA C. 2026-0001", fields[^2]);
        Assert.Equal(9, fields.Length);
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
