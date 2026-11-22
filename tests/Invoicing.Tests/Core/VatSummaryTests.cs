using System.Globalization;
using Invoicing.Core.Documents;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class VatSummaryTests
{
    [Fact]
    public void Calculate_TwoRates_SortsRowsAndSums()
    {
        var summary = VatSummary.Calculate(TestData.Lines);
        Assert.Equal(new[] { new VatSummaryRow(21, 2450.45m, 514.59m), new VatSummaryRow(12, 360m, 43.20m) }, summary.Rows);
        Assert.Equal(2810.45m, summary.TotalBase);
        Assert.Equal(557.79m, summary.TotalVat);
        Assert.Equal(3368.24m, summary.Total);
    }

    [Theory]
    [InlineData("0.5", 1, "0.11")]
    [InlineData("2.5", 1, "0.53")]
    [InlineData("-2.5", 1, "-0.53")]
    [InlineData("0.5", 3, "0.32")]
    public void Calculate_Midpoints_RoundsVatAfterGrouping(string quantity, int count, string expected)
    {
        var lines = Enumerable.Repeat(new InvoiceLine("Test", Parse(quantity), "pc", 1, 21, 0), count);
        Assert.Equal(Parse(expected), VatSummary.Calculate(lines).TotalVat);
    }

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
