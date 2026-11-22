using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
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
            // The command forwards unexpected startup errors to the dispatcher handler.
            var startup = new AsyncRelayCommand(() => StartAsync(desktop));
            Dispatcher.UIThread.Post(() => startup.Execute(null));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await LoadSettingsAndOpenAsync(desktop);
        }
        catch (EnvFileFormatException error)
        {
            ShowError(desktop, [StartupTexts.InvalidLine(error.LineNumber)], StartupTexts.ConfigurationHelp);
        }
        catch (IOException)
        {
            ShowError(desktop, [StartupTexts.EnvReadFailed], StartupTexts.ConfigurationHelp);
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(desktop, [StartupTexts.AccessDenied], StartupTexts.ConfigurationHelp);
        }
        catch (SqliteException)
        {
            ShowError(desktop, [StartupTexts.DatabaseOpenFailed], StartupTexts.DatabaseHelp);
        }
    }

    private static async Task LoadSettingsAndOpenAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var result = await EnvironmentReader.LoadAsync();
        if (result.Settings is AppSettings settings)
        {
            await OpenMainAsync(desktop, settings);
            return;
        }

        ShowError(desktop, result.Errors.Select(StartupTexts.Configuration), StartupTexts.ConfigurationHelp);
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

        await ShowMainAsync(desktop, settings, database);
    }

    private static async Task ShowMainAsync(IClassicDesktopStyleApplicationLifetime desktop, AppSettings settings, string database)
    {
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
        var model = new ErrorViewModel(string.Join(Environment.NewLine, errors), help);
        var window = new ErrorWindow { DataContext = model };
        model.CloseRequested += window.Close;
        desktop.MainWindow = window;
        window.Closed += (_, _) => desktop.Shutdown(1);
        window.Show();
    }

    private static void ShowUnexpectedError(Exception error)
    {
        Console.Error.WriteLine(error);
        var window = DialogService.CreateMessageWindow(UiTexts.UnexpectedError(error.Message), confirm: false);
        var desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop is not null && desktop.MainWindow is null)
        {
            window.Closed += (_, _) => desktop.Shutdown(1);
        }

        var owner = desktop?.Windows.LastOrDefault(candidate => candidate.IsActive);
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
