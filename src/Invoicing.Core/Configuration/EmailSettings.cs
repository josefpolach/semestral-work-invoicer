using Invoicing.Core.Validation;

namespace Invoicing.Core.Configuration;

/// <summary>Optional e-mail credentials and any problems that disable sending.</summary>
public class EmailSettings
{
    public EmailSettings(string? apiKey, string? fromAddress)
    {
        ApiKey = apiKey?.Trim() ?? string.Empty;
        FromAddress = fromAddress?.Trim() ?? string.Empty;
        var problems = new List<ConfigurationError>();
        if (ApiKey.Length == 0)
        {
            problems.Add(new(SettingKeys.LettrApiKey, ConfigurationProblem.Missing));
        }

        if (!EmailAddressValidator.IsValid(FromAddress))
        {
            problems.Add(new(SettingKeys.EmailFromAddress,
                FromAddress.Length == 0 ? ConfigurationProblem.Missing : ConfigurationProblem.Invalid));
        }

        Problems = problems.AsReadOnly();
    }

    public string ApiKey { get; }
    public string FromAddress { get; }
    public bool IsEnabled => Problems.Count == 0;
    public IReadOnlyList<ConfigurationError> Problems { get; }

    public override string ToString() => nameof(EmailSettings);
}
