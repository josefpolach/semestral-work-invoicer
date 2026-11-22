using Invoicing.Core.Configuration;
using Invoicing.Data;

namespace Invoicing.App.Texts;

internal static class StartupTexts
{
    public const string Title = "Aplikaci nelze spustit";
    public const string ConfigurationHelp = "Upravte soubor .env ve složce aplikace a spusťte aplikaci znovu.";
    public const string DatabaseHelp = "Spusťte migrace v kořenové složce projektu: docker compose run --rm flyway";

    public static string Configuration(ConfigurationError error) => error.Problem == ConfigurationProblem.Missing
        ? $"Chybí povinná hodnota {error.Key} v souboru .env."
        : $"Hodnota {error.Key} v souboru .env je neplatná.";

    public static string Database(DatabaseStatus status, string path) => status switch
    {
        DatabaseStatus.FileMissing => $"Databáze nebyla nalezena: {path}",
        DatabaseStatus.NotMigrated => "Databáze není připravena (chybí tabulka flyway_schema_history).",
        DatabaseStatus.OutdatedSchema => $"Databáze má starou verzi schématu. Je potřeba verze {DatabaseVersionChecker.RequiredSchemaVersion}.",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
