using System.Globalization;
using System.Text;

namespace Invoicing.Core.Payments;

/// <summary>Builds the Czech QR Platba payment descriptor.</summary>
public class SpaydPayment(string iban, decimal amount, string variableSymbol, DateOnly dueDate, string message)
{
    public const decimal MaxAmount = 9_999_999.99m;
    private const int MaxAccountLength = 46;
    private const int MaxSymbolLength = 10;
    private const int MaxMessageLength = 60;

    public string ToText()
    {
        if (iban.Length is 0 or > MaxAccountLength || !iban.All(char.IsAsciiLetterOrDigit))
        {
            throw new ArgumentException("Invalid payment account.");
        }

        if (amount is < 0 or > MaxAmount)
        {
            throw new ArgumentException("Invalid payment amount.");
        }

        if (variableSymbol.Length is 0 or > MaxSymbolLength || !variableSymbol.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Invalid variable symbol.");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"SPD*1.0*ACC:{iban}*AM:{Math.Round(amount, 2, MidpointRounding.AwayFromZero):0.00}*CC:CZK*X-VS:{variableSymbol}*DT:{dueDate:yyyyMMdd}*MSG:{NormalizeMessage(message)}*");
    }

    private static string NormalizeMessage(string value)
    {
        // Form D separates combining marks from their letters so the ASCII filter can drop them.
        var characters = value.Normalize(NormalizationForm.FormD).ToUpperInvariant()
            .Where(character => character is >= ' ' and <= '~' and not '*');
        var trimmed = new string(characters.ToArray()).Trim();
        return trimmed.Length <= MaxMessageLength ? trimmed : trimmed[..MaxMessageLength];
    }
}
