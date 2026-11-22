namespace Invoicing.Core.Validation;

/// <summary>Checks a Czech company number and its check digit.</summary>
public static class IcoValidator
{
    private const int Length = 8;
    private const int Modulus = 11;
    private const int CheckDigitModulus = 10;

    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != Length || !value.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < Length - 1; index++)
        {
            sum += (value[index] - '0') * (Length - index);
        }

        return (Modulus - sum % Modulus) % CheckDigitModulus == value[^1] - '0';
    }
}
