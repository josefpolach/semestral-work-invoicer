using Invoicing.Core.Payments;

namespace Invoicing.Tests.Core;

public class CzechBankAccountTests
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
}
