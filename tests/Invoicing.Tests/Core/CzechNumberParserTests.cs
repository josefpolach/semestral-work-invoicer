using System.Globalization;
using Invoicing.Core.Formatting;

namespace Invoicing.Tests.Core;

public class CzechNumberParserTests
{
    [Theory]
    [InlineData("1,5", "1.5")]
    [InlineData("1.5", "1.5")]
    [InlineData("1 000,50", "1000.50")]
    [InlineData("1\u00a0000,50", "1000.50")]
    public void TryParseDecimal_ValidText_ReturnsAmount(string text, string expected)
    {
        Assert.True(CzechNumberParser.TryParseDecimal(text, out var value));
        Assert.Equal(Parse(expected), value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseDecimal_InvalidText_ReturnsFalse(string text) =>
        Assert.False(CzechNumberParser.TryParseDecimal(text, out _));

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
