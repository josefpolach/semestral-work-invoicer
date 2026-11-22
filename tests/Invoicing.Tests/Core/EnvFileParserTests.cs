using Invoicing.Core.Configuration;

namespace Invoicing.Tests.Core;

public class EnvFileParserTests
{
    [Fact]
    public void Parse_CommentsWhitespaceAndQuotes_ReturnsOnlySettings()
    {
        var values = EnvFileParser.Parse([" # Comment", "", " A = b=c ", "B=\" one \"", "C='two'"]);
        Assert.Equal(new Dictionary<string, string> { ["A"] = "b=c", ["B"] = " one ", ["C"] = "two" }, values);
    }

    [Theory]
    [InlineData(" = value")]
    [InlineData("bad")]
    public void Parse_MalformedLine_ReportsLineNumber(string line) =>
        Assert.Equal(2, Assert.Throws<EnvFileFormatException>(() => EnvFileParser.Parse(["", line])).LineNumber);
}
