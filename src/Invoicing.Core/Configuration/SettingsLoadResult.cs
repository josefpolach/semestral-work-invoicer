namespace Invoicing.Core.Configuration;

public record SettingsLoadResult(AppSettings? Settings, IReadOnlyList<ConfigurationError> Errors);
