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
                var loginTheme =
                    loginWindow.SelectedTheme;

                var keepLoginTheme =
                    loginWindow.ThemeWasChangedByUser;

                ApplicationAppearanceService.LoadAndApplyForProfile(
                    profile.Login);

                if (keepLoginTheme)
                {
                    var profileAppearance =
                        ApplicationAppearanceService.Current;

                    ApplicationAppearanceService.SaveAndApply(
                        new ApplicationAppearanceSettings
                        {
                            Theme = loginTheme,
                            AccentColor = profileAppearance.AccentColor,
                            FontFamily = profileAppearance.FontFamily,
                            TextSize = profileAppearance.TextSize,
                            UseSemiBoldText = profileAppearance.UseSemiBoldText
                        });
                }

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

                mainWindow.WindowState =
                    WindowState.Maximized;

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
