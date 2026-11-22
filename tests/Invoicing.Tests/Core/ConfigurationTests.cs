using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class ConfigurationTests
{
    [Fact]
    public void Parse_IgnoresCommentsAndUnquotesValues()
    {
        var values = EnvFileParser.Parse([" # Comment", "", " A = b=c ", "B=\" one \"", "C='two'"]);
        Assert.Equal("b=c", values["A"]);
        Assert.Equal(" one ", values["B"]);
        Assert.Equal("two", values["C"]);
        Assert.Equal(2, Assert.Throws<EnvFileFormatException>(() => EnvFileParser.Parse(["", " = value"])).LineNumber);
        Assert.Throws<EnvFileFormatException>(() => EnvFileParser.Parse(["bad"]));
    }

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

    [Theory]
    [InlineData("test-secret", "from@example.com", true)]
    [InlineData("", "from@example.com", false)]
    [InlineData("test-secret", "invalid", false)]
    public void Load_OptionalEmailSettings_ReportAvailability(string key, string address, bool enabled)
    {
        var values = ValidValues();
        values[SettingKeys.LettrApiKey] = key;
        values[SettingKeys.EmailFromAddress] = address;
        var settings = Assert.IsType<AppSettings>(AppSettings.Load(values).Settings);
        Assert.Equal(enabled, settings.Email.IsEnabled);
        Assert.DoesNotContain("test-secret", settings.Email.ToString());
        if (!enabled)
        {
            var expected = key.Length == 0
                ? new ConfigurationError(SettingKeys.LettrApiKey, ConfigurationProblem.Missing)
                : new ConfigurationError(SettingKeys.EmailFromAddress, ConfigurationProblem.Invalid);
            Assert.Equal(expected, Assert.Single(settings.Email.Problems));
        }
    }

    [Fact]
    public void Evaluate_ConfigurationAndDocumentStatus_ExplainsAvailability()
    {
        var enabled = new EmailSettings("test", "from@example.com");
        var disabled = new EmailSettings(null, null);
        var draft = TestData.Invoice();
        var invoice = TestData.Issued(DocumentKind.NonVatInvoice);
        var tax = TestData.Issued(DocumentKind.TaxInvoice);
        Assert.Equal(EmailUnavailableReason.NotConfigured, EmailAvailability.Evaluate(disabled, draft).Reason);
        Assert.Equal(EmailUnavailableReason.DocumentIsDraft, EmailAvailability.Evaluate(enabled, draft).Reason);
        Assert.True(EmailAvailability.Evaluate(enabled, invoice).CanSend);
        invoice.Cancel();
        tax.MarkAsPaid();
        Assert.Equal(EmailUnavailableReason.DocumentIsCancelled, EmailAvailability.Evaluate(enabled, invoice).Reason);
        Assert.True(EmailAvailability.Evaluate(enabled, tax).CanSend);
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
