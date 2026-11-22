using System.Globalization;
using Invoicing.Core.Documents;

namespace Invoicing.Tests.Core;

public class InvoiceLineTests
{
    [Theory]
    [InlineData("2", "1000", "15", "1700.00")]
    [InlineData("1", "99.99", "10", "89.99")]
    [InlineData("3", "33.335", "0", "100.01")]
    [InlineData("-2.5", "10.01", "0", "-25.03")]
    public void CalculateBase_Amounts_RoundsAwayFromZero(string quantity, string price, string discount, string expected)
    {
        var line = new InvoiceLine("Test", Parse(quantity), "h", Parse(price), 21, Parse(discount));
        Assert.Equal(Parse(expected), line.CalculateBase());
    }

    [Fact]
    public void WithNegatedQuantity_Line_OnlyChangesQuantity()
    {
        var line = new InvoiceLine("Test", 2m, "h", 12.5m, 21m, 5m);
        var negated = line.WithNegatedQuantity();
        Assert.Equal(-line.Quantity, negated.Quantity);
        Assert.Equal((line.Description, line.Unit, line.UnitPriceWithoutVat, line.VatRate, line.DiscountPercent),
            (negated.Description, negated.Unit, negated.UnitPriceWithoutVat, negated.VatRate, negated.DiscountPercent));
    }

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
