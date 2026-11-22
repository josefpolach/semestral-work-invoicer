namespace Invoicing.Core.Configuration;

/// <summary>Names shared by environment variables and the configuration file.</summary>
public static class SettingKeys
{
    public const string VatRateStandard = "VAT_RATE_STANDARD";
    public const string VatRateReduced = "VAT_RATE_REDUCED";
    public const string DatabasePath = "DATABASE_PATH";
    public const string AresBaseUrl = "ARES_BASE_URL";
    public const string LettrBaseUrl = "LETTR_BASE_URL";
    public const string LettrApiKey = "LETTR_API_KEY";
    public const string EmailFromAddress = "EMAIL_FROM_ADDRESS";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        VatRateStandard, VatRateReduced, DatabasePath, AresBaseUrl, LettrBaseUrl, LettrApiKey, EmailFromAddress
    });
}
