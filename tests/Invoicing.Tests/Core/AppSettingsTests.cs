using Invoicing.Core.Configuration;

namespace Invoicing.Tests.Core;

public class AppSettingsTests
{
    [Theory]
    [InlineData(SettingKeys.VatRateStandard)]
    [InlineData(SettingKeys.VatRateReduced)]
    [InlineData(SettingKeys.DatabasePath)]
    [InlineData(SettingKeys.AresBaseUrl)]
    [InlineData(SettingKeys.LettrBaseUrl)]
    public void Load_MissingRequiredKey_ReportsMissing(string key)
    {
        var values = ValidValues();
        values.Remove(key);
        var result = AppSettings.Load(values);
        Assert.Null(result.Settings);
        Assert.Equal(new ConfigurationError(key, ConfigurationProblem.Missing), Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData(SettingKeys.VatRateStandard, "abc")]
    [InlineData(SettingKeys.VatRateStandard, "0")]
    [InlineData(SettingKeys.VatRateStandard, "100")]
    [InlineData(SettingKeys.VatRateReduced, "21,5")]
    [InlineData(SettingKeys.AresBaseUrl, "example.com")]
    [InlineData(SettingKeys.LettrBaseUrl, "ftp://example.com")]
    public void Load_InvalidRequiredKey_ReportsInvalid(string key, string value)
    {
        var values = ValidValues();
        values[key] = value;
        Assert.Equal(new ConfigurationError(key, ConfigurationProblem.Invalid), Assert.Single(AppSettings.Load(values).Errors));
    }

    [Fact]
    public void Load_ValidRequiredValues_ReturnsSettings()
    {
        var result = AppSettings.Load(ValidValues());
        var settings = Assert.IsType<AppSettings>(result.Settings);
        Assert.Equal(21m, settings.VatRates.Standard);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Load_ValidEmailSettings_EnablesEmail()
    {
        var values = ValidValues();
        values[SettingKeys.LettrApiKey] = "test-secret";
        values[SettingKeys.EmailFromAddress] = "from@example.com";
        var settings = Assert.IsType<AppSettings>(AppSettings.Load(values).Settings);
        Assert.True(settings.Email.IsEnabled);
    }

    [Theory]
    [InlineData("", "from@example.com", SettingKeys.LettrApiKey, ConfigurationProblem.Missing)]
    [InlineData("test-secret", "invalid", SettingKeys.EmailFromAddress, ConfigurationProblem.Invalid)]
    public void Load_InvalidOptionalSettings_DisablesEmail(string key, string address, string problemKey, ConfigurationProblem problem)
    {
        var values = ValidValues();
        values[SettingKeys.LettrApiKey] = key;
        values[SettingKeys.EmailFromAddress] = address;
        var settings = Assert.IsType<AppSettings>(AppSettings.Load(values).Settings);
        Assert.False(settings.Email.IsEnabled);
        Assert.Equal(new ConfigurationError(problemKey, problem), Assert.Single(settings.Email.Problems));
    }

    private static Dictionary<string, string> ValidValues() => new()
    {
        [SettingKeys.VatRateStandard] = "21",
        [SettingKeys.VatRateReduced] = "12",
        [SettingKeys.DatabasePath] = "data/db",
        [SettingKeys.AresBaseUrl] = "https://example.com/ares",
        [SettingKeys.LettrBaseUrl] = "https://example.com/api"
    };
}
