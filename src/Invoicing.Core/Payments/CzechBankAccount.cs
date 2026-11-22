using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Invoicing.Core.Payments;

/// <summary>A Czech domestic account that can be converted to IBAN.</summary>
public class CzechBankAccount
{
    private const string Pattern = @"\A(?:([0-9]{1,6})-)?([0-9]{2,10})/([0-9]{4})\z";
    private const int PrefixWidth = 6;
    private const int AccountWidth = 10;
    private const int IbanModulus = 97;
    private const int IbanCheckBase = 98;
    // ISO 13616 treats C and Z as 12 and 35, followed by two zero check digits.
    private const string CountryCheckSuffix = "123500";

    private CzechBankAccount(string prefix, string number, string bankCode)
    {
        Prefix = prefix;
        Number = number;
        BankCode = bankCode;
    }

    public string Prefix { get; }
    public string Number { get; }
    public string BankCode { get; }

    public static bool TryParse(string? text, [NotNullWhen(true)] out CzechBankAccount? account)
    {
        var match = Regex.Match(text?.Trim() ?? string.Empty, Pattern);
        account = match.Success ? new(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value) : null;
        return account is not null;
    }

    public string ToIban()
    {
        var domestic = BankCode + Prefix.PadLeft(PrefixWidth, '0') + Number.PadLeft(AccountWidth, '0');
        // The rearranged IBAN is too long for a long, so compute its remainder one digit at a time.
        var remainder = 0;
        foreach (var digit in domestic + CountryCheckSuffix)
        {
            remainder = (remainder * 10 + digit - '0') % IbanModulus;
        }

        return "CZ" + (IbanCheckBase - remainder).ToString("00", CultureInfo.InvariantCulture) + domestic;
    }
}
