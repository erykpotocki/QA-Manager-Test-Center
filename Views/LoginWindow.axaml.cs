using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using QARegressionManager.Services;
using Avalonia.Threading;
using QARegressionManager.Models;

namespace QARegressionManager.Views;

public partial class LoginWindow : Window
{
    private readonly UserProfileService _profileService =
        new();
    private readonly AssignmentService _assignmentService =
        new();

    private UserProfileModel? _authenticatedProfile;
    private bool _legacyTestProfilesCleaned;
    private bool _isBusy;

    public event Action<UserProfileModel>? Authenticated;

    public LoginWindow()
    {
        InitializeComponent();

        LocalizationService.LanguageChanged +=
            LocalizationService_OnLanguageChanged;
        Closed += (_, _) =>
            LocalizationService.LanguageChanged -=
                LocalizationService_OnLanguageChanged;

        UpdateLanguagePicker();
        UpdateThemeButton();

        Opened +=
            LoginWindow_OnOpened;
    }

    private void EnglishLanguageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LocalizationService.SaveAndApply(LocalizationService.English);
        LanguagePickerButton.Flyout?.Hide();
    }

    private void PolishLanguageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LocalizationService.SaveAndApply(LocalizationService.Polish);
        LanguagePickerButton.Flyout?.Hide();
    }

    private void ThemeToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var current = ApplicationAppearanceService.Current;
        ApplicationAppearanceService.SaveAndApply(
            new ApplicationAppearanceSettings
            {
                Theme = string.Equals(current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)
                    ? "Light"
                    : "Dark",
                FontFamily = current.FontFamily,
                TextSize = current.TextSize,
                UseSemiBoldText = current.UseSemiBoldText
            });

        UpdateThemeButton();
    }

    private void UpdateThemeButton()
    {
        var isDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
        ThemeIconTextBlock.Text = isDark ? "☀" : "☾";
        ToolTip.SetTip(
            ThemeToggleButton,
            LocalizationService.T(isDark ? "Theme.SwitchToLight" : "Theme.SwitchToDark"));
    }

    private void LocalizationService_OnLanguageChanged(
        object? sender,
        EventArgs e)
    {
        UpdateLanguagePicker();
        UpdateThemeButton();
    }

    private void UpdateLanguagePicker()
    {
        var isPolish = LocalizationService.IsPolish;
        CurrentPolishFlag.IsVisible = isPolish;
        CurrentEnglishFlag.IsVisible = !isPolish;
        ToolTip.SetTip(
            LanguagePickerButton,
            isPolish
                ? "Aktualny język: polski"
                : "Current language: English");
    }

    private async void LoginWindow_OnOpened(
        object? sender,
        EventArgs e)
    {
        Dispatcher.UIThread.Post(
            FocusLoginInput,
            DispatcherPriority.Loaded);

        if (_legacyTestProfilesCleaned)
        {
            return;
        }

        _legacyTestProfilesCleaned =
            true;

        SetBusy(
            true);

        try
        {
            await _profileService.EnsureDemoProfilesAsync();
        }
        catch
        {
            ShowError(
                LocalizationService.T("Login.Error.Cleanup"));
        }
        finally
        {
            SetBusy(
                false);
        }
    }

    public void FocusLoginInput()
    {
        Activate();

        LoginTextBox.Focus();
    }

    private async void LoginButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await TryLoginAsync();
    }

    private void LoginTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            _isBusy)
        {
            return;
        }

        e.Handled =
            true;

        PinTextBox.Focus();
    }

    private async void PinTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            _isBusy)
        {
            return;
        }

        e.Handled =
            true;

        await TryLoginAsync();
    }

    private async Task TryLoginAsync()
    {
        if (_isBusy)
        {
            return;
        }

        SetBusy(
            true);

        HideError();

        try
        {
            var login =
                LoginTextBox.Text
                    ?.Trim()
                ?? string.Empty;

            var pin =
                PinTextBox.Text
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError(
                    LocalizationService.T("Login.Error.LoginRequired"));

                return;
            }

            if (!UserProfileService.IsValidPin(pin))
            {
                ShowError(
                    LocalizationService.T("Login.Error.PinFormat"));

                SelectPinForCorrection();

                return;
            }

            var result =
                await _profileService.AuthenticateAsync(
                    login,
                    pin);

            if (result.Status ==
                AuthenticationStatus.PinWasReset)
            {
                ShowError(
                    LocalizationService.T("Login.Error.PinReset"));

                SelectPinForCorrection();

                return;
            }

            if (result.Status !=
                    AuthenticationStatus.Success ||
                result.Profile is null)
            {
                ShowError(
                    LocalizationService.T("Login.Error.InvalidCredentials"));

                SelectPinForCorrection();

                return;
            }

            _authenticatedProfile =
                result.Profile;

            if (_authenticatedProfile.RequiresPinChange)
            {
                ShowChangePinPanel();
                return;
            }

            CompleteAuthentication(
                _authenticatedProfile);
        }
        catch
        {
            ShowError(
                LocalizationService.T("Login.Error.ProfileRead"));
        }
        finally
        {
            SetBusy(
                false);
        }
    }

    private async void ChangePinButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await TryChangePinAsync();
    }

    private async void ConfirmPinTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            _isBusy)
        {
            return;
        }

        e.Handled =
            true;

        await TryChangePinAsync();
    }

    private void NewPinTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            _isBusy)
        {
            return;
        }

        e.Handled =
            true;

        ConfirmPinTextBox.Focus();
    }

    private async Task TryChangePinAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (_authenticatedProfile is null)
        {
            ShowLoginPanel();
            return;
        }

        HideError();

        var newPin =
            NewPinTextBox.Text
            ?? string.Empty;

        var confirmedPin =
            ConfirmPinTextBox.Text
            ?? string.Empty;

        if (!UserProfileService.IsValidPin(newPin))
        {
            ShowError(
                LocalizationService.T("Login.Error.NewPinFormat"));

            return;
        }

        if (newPin != confirmedPin)
        {
            ShowError(
                LocalizationService.T("Login.Error.PinMismatch"));

            return;
        }

        SetBusy(
            true);

        try
        {
            await _profileService.ChangePinAsync(
                _authenticatedProfile.Id,
                newPin);

            _authenticatedProfile.RequiresPinChange =
                false;

            CompleteAuthentication(
                _authenticatedProfile);
        }
        catch (ArgumentException)
        {
            ShowError(
                LocalizationService.T("Login.Error.NewPinFormat"));
        }
        catch
        {
            ShowError(
                LocalizationService.T("Login.Error.PinSave"));
        }
        finally
        {
            SetBusy(
                false);
        }
    }

    private void CompleteAuthentication(
        UserProfileModel profile)
    {
        Authenticated?.Invoke(
            profile);
    }

    private void SelectPinForCorrection()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                PinTextBox.Focus();
                PinTextBox.SelectAll();
            },
            DispatcherPriority.Input);
    }

    private void ShowChangePinPanel()
    {
        LoginPanel.IsVisible =
            false;

        ChangePinPanel.IsVisible =
            true;

        ScreenDescriptionTextBlock.Text =
            string.Format(
                LocalizationService.T("Login.Welcome"),
                _authenticatedProfile?.Login);

        NewPinTextBox.Focus();
    }

    private void ShowLoginPanel()
    {
        _authenticatedProfile =
            null;

        LoginPanel.IsVisible =
            true;

        ChangePinPanel.IsVisible =
            false;

        ScreenDescriptionTextBlock.Text =
            LocalizationService.T("Login.Description");

        PinTextBox.Text =
            string.Empty;

        PinTextBox.Focus();
    }

    private void SetBusy(
        bool isBusy)
    {
        _isBusy =
            isBusy;

        LoginButton.IsEnabled =
            !isBusy;

        ChangePinButton.IsEnabled =
            !isBusy;
    }

    private void ShowError(
        string message)
    {
        ErrorTextBlock.Text =
            message;

        ErrorTextBlock.IsVisible =
            true;
    }

    private void HideError()
    {
        ErrorTextBlock.IsVisible =
            false;
    }
}
