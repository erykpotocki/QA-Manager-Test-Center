using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QARegressionManager.Services;
using QARegressionManager.Views;

namespace QARegressionManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LocalizationService.LoadAndApply();
        ApplicationAppearanceService.LoadAndApply();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode =
                ShutdownMode.OnMainWindowClose;

            desktop.MainWindow =
                CreateLoginWindow(
                    desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static LoginWindow CreateLoginWindow(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginWindow =
            new LoginWindow();

        loginWindow.Authenticated +=
            profile =>
            {
                ApplicationAppearanceService.LoadAndApplyForProfile(
                    profile.Login);

                var mainWindow =
                    new MainWindow(
                        profile);

                mainWindow.LogoutRequested +=
                    () =>
                        ShowLoginWindow(
                            desktop,
                            mainWindow);

                desktop.MainWindow =
                    mainWindow;

                WindowPlacementService.PlaceNearPreviousWindow(
                    loginWindow,
                    mainWindow);

                mainWindow.Show();
                loginWindow.Close();
            };

        return loginWindow;
    }

    private static void ShowLoginWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow currentWindow)
    {
        ApplicationAppearanceService.LoadAndApplyLocal();

        var loginWindow =
            CreateLoginWindow(
                desktop);

        desktop.MainWindow =
            loginWindow;

        WindowPlacementService.PlaceNearPreviousWindow(
            currentWindow,
            loginWindow);

        loginWindow.Show();
        currentWindow.Close();

        Dispatcher.UIThread.Post(
            loginWindow.FocusLoginInput,
            DispatcherPriority.Loaded);
    }
}
