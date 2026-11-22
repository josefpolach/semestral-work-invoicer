using System.Globalization;
using System.Text.RegularExpressions;

namespace Invoicing.Core.Formatting;

/// <summary>Parses decimal input with either decimal separator and optional space grouping.</summary>
public static class CzechNumberParser
{
    private const string Pattern = @"\A[+-]?(?:[0-9]+|[0-9]{1,3}(?:[ \u00a0][0-9]{3})+)?(?:[.,][0-9]*)?\z";

    public static bool TryParseDecimal(string? text, out decimal value)
    {
        text = text?.Trim() ?? string.Empty;
        value = 0;
        return Regex.IsMatch(text, Pattern) && decimal.TryParse(
            text.Replace(" ", "").Replace("\u00a0", "").Replace(',', '.'),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
