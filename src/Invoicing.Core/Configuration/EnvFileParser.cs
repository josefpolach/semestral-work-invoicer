namespace Invoicing.Core.Configuration;

/// <summary>Parses the application's KEY=value configuration file.</summary>
public static class EnvFileParser
{
    public static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var lineNumber = 0;
        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new EnvFileFormatException(lineNumber);
            }

            values[line[..separator].Trim()] = Unquote(line[(separator + 1)..].Trim());
        }

        return values;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && (value[0] is '"' or '\'') && value[^1] == value[0] ? value[1..^1] : value;
}
