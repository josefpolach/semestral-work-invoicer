using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Startup;
using Invoicing.App.Texts;
using Invoicing.App.ViewModels;
using Invoicing.App.Views;
using Invoicing.Core.Configuration;
using Invoicing.Data;
using Invoicing.Pdf;
using Microsoft.Data.Sqlite;

namespace Invoicing.App;

/// <summary>Loads settings and opens the desktop application's main window.</summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var culture = CultureInfo.GetCultureInfo("cs-CZ");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Dispatcher.UIThread.UnhandledException += (_, args) =>
            {
                ShowUnexpectedError(args.Exception);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) => Console.Error.WriteLine(args.ExceptionObject);
            var startup = new AsyncRelayCommand(() => StartAsync(desktop));
            Dispatcher.UIThread.Post(() => startup.Execute(null));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var result = await EnvironmentReader.LoadAsync();
            if (result.Settings is AppSettings settings)
            {
                await OpenMainAsync(desktop, settings);
            }
            else
            {
                ShowError(desktop, result.Errors.Select(StartupTexts.Configuration), StartupTexts.ConfigurationHelp);
            }
        }
        catch (EnvFileFormatException error)
        {
            ShowError(desktop, [$"Soubor .env obsahuje neplatný řádek {error.LineNumber}."], StartupTexts.ConfigurationHelp);
        }
        catch (IOException)
        {
            ShowError(desktop, ["Soubor .env se nepodařilo přečíst."], StartupTexts.ConfigurationHelp);
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(desktop, ["K souboru .env nebo k databázi chybí oprávnění."], StartupTexts.ConfigurationHelp);
        }
        catch (SqliteException)
        {
            ShowError(desktop, ["Databázi se nepodařilo otevřít."], StartupTexts.DatabaseHelp);
        }
    }

    private static async Task OpenMainAsync(IClassicDesktopStyleApplicationLifetime desktop, AppSettings settings)
    {
        var database = Path.GetFullPath(settings.DatabasePath, AppContext.BaseDirectory);
        var status = await new DatabaseVersionChecker(database).CheckAsync();
        if (status != DatabaseStatus.Ready)
        {
            ShowError(desktop, [StartupTexts.Database(status, database)], StartupTexts.DatabaseHelp);
            return;
        }

        PdfSetup.Initialize();
        var root = new CompositionRoot(settings, database);
        var window = await root.CreateMainAsync();
        desktop.MainWindow = window;
        window.Closed += (_, _) =>
        {
            root.Dispose();
            desktop.Shutdown();
        };
        window.Show();
    }

    private static void ShowError(IClassicDesktopStyleApplicationLifetime desktop, IEnumerable<string> errors, string help)
    {
        var window = new ErrorWindow { DataContext = new ErrorViewModel(string.Join(Environment.NewLine, errors), help) };
        desktop.MainWindow = window;
        window.Closed += (_, _) => desktop.Shutdown(1);
        window.Show();
    }

    private static void ShowUnexpectedError(Exception error)
    {
        Console.Error.WriteLine(error);
        var window = new MessageWindow
        {
            DataContext = new MessageViewModel($"Nastala neočekávaná chyba: {error.Message}", false)
        };
        var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows
            .LastOrDefault(candidate => candidate.IsActive);
        if (owner is null)
        {
            window.Show();
        }
        else
        {
            window.Show(owner);
        }
    }
}
