using Invoicing.Core.Configuration;

namespace Invoicing.App.Startup;

internal static class EnvironmentReader
{
    private const string EnvFile = ".env";

    public static async Task<SettingsLoadResult> LoadAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, EnvFile);
        var lines = File.Exists(path) ? await File.ReadAllLinesAsync(path) : [];
        var values = EnvFileParser.Parse(lines);
        foreach (var key in SettingKeys.All)
        {
            if (Environment.GetEnvironmentVariable(key) is string value)
            {
                values[key] = value;
            }
        }

        return AppSettings.Load(values);
    }
}
