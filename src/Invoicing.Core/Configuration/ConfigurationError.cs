namespace Invoicing.Core.Configuration;

public record ConfigurationError(string Key, ConfigurationProblem Problem);
