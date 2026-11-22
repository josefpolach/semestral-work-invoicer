using Invoicing.Core.Validation;

namespace Invoicing.Tests.Core;

public class IcoValidatorTests
{
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
}
