using System.Globalization;

namespace Invoicing.Core.Configuration;

/// <summary>Validated settings required at startup.</summary>
public class AppSettings
{
    private AppSettings(VatRates vatRates, string databasePath, Uri aresBaseUrl, Uri lettrBaseUrl)
    {
        VatRates = vatRates;
        DatabasePath = databasePath;
        AresBaseUrl = aresBaseUrl;
        LettrBaseUrl = lettrBaseUrl;
    }

    public VatRates VatRates { get; }
    public string DatabasePath { get; }
    public Uri AresBaseUrl { get; }
    public Uri LettrBaseUrl { get; }
    public EmailSettings Email { get; private init; } = new(null, null);

    public static SettingsLoadResult Load(IReadOnlyDictionary<string, string> values)
    {
        var errors = new List<ConfigurationError>();
        var standard = ReadRate(values.GetValueOrDefault(SettingKeys.VatRateStandard), SettingKeys.VatRateStandard, errors);
        var reduced = ReadRate(values.GetValueOrDefault(SettingKeys.VatRateReduced), SettingKeys.VatRateReduced, errors);
        var database = ReadDatabasePath(values, errors);

        var ares = ReadUrl(values.GetValueOrDefault(SettingKeys.AresBaseUrl), SettingKeys.AresBaseUrl, errors);
        var lettr = ReadUrl(values.GetValueOrDefault(SettingKeys.LettrBaseUrl), SettingKeys.LettrBaseUrl, errors);
        if (errors.Count > 0 || database is null || ares is null || lettr is null)
        {
            return new(null, errors.AsReadOnly());
        }

        var settings = new AppSettings(new(standard, reduced), database, ares, lettr)
        {
            Email = new(values.GetValueOrDefault(SettingKeys.LettrApiKey),
                values.GetValueOrDefault(SettingKeys.EmailFromAddress))
        };
        return new(settings, errors.AsReadOnly());
    }

    private static string? ReadDatabasePath(IReadOnlyDictionary<string, string> values, List<ConfigurationError> errors)
    {
        var database = values.GetValueOrDefault(SettingKeys.DatabasePath)?.Trim();
        if (string.IsNullOrWhiteSpace(database))
        {
            errors.Add(new(SettingKeys.DatabasePath, ConfigurationProblem.Missing));
        }

        return database;
    }

    private static decimal ReadRate(string? value, string key, List<ConfigurationError> errors)
    {
        if (decimal.TryParse(value?.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate)
            && rate is > 0 and < 100)
        {
            return rate;
        }

        AddProblem(value, key, errors);
        return 0;
    }

    private static Uri? ReadUrl(string? value, string key, List<ConfigurationError> errors)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var url) && url.Scheme is "https" or "http")
        {
            return url;
        }

        AddProblem(value, key, errors);
        return null;
    }

    private static void AddProblem(string? value, string key, List<ConfigurationError> errors) =>
        errors.Add(new(key, string.IsNullOrWhiteSpace(value) ? ConfigurationProblem.Missing : ConfigurationProblem.Invalid));
}
