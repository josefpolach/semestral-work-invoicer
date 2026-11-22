using System.Globalization;

namespace Invoicing.Core.Formatting;

/// <summary>Culture-independent Czech display formats.</summary>
public static class CzechFormat
{
    private static readonly NumberFormatInfo _numberFormat = new()
    {
        NumberGroupSeparator = "\u00a0",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3],
        NegativeSign = "-",
        NumberNegativePattern = 1
    };

    public static string FormatMoney(decimal value) => FormatNumber(value) + "\u00a0Kč";
    public static string FormatNumber(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("N2", _numberFormat);
    public static string FormatQuantity(decimal value) => value.ToString("#,##0.###", _numberFormat);
    public static string FormatDate(DateOnly value) => value.ToString("d. M. yyyy", CultureInfo.InvariantCulture);
}
