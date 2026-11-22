namespace Invoicing.Core.Configuration;

/// <summary>Identifies a malformed configuration line without exposing its contents.</summary>
public class EnvFileFormatException(int lineNumber) : FormatException($"Invalid environment file line {lineNumber}.")
{
    public int LineNumber { get; } = lineNumber;
}
