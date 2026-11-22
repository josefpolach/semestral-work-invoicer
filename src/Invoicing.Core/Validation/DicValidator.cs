using System.Text.RegularExpressions;

namespace Invoicing.Core.Validation;

/// <summary>Checks the format of a Czech tax identification number.</summary>
public static class DicValidator
{
    private const string Pattern = @"\ACZ[0-9]{8,10}\z";

    public static bool IsValid(string? value) => value is not null && Regex.IsMatch(value, Pattern);
}
