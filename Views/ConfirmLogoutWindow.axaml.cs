using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ConfirmLogoutWindow : Window
{
    public ConfirmLogoutWindow()
        : this(string.Empty)
    {
    }

    public ConfirmLogoutWindow(
        string login)
    {
        InitializeComponent();

        MessageTextBlock.Text =
            string.IsNullOrWhiteSpace(
                login)
                ? LocalizationService.T("Logout.GenericDescription")
                : LocalizationService.Format("Logout.UserDescription", login);
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            false);
    }

    private void LogoutButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            true);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            Close(
                true);

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Escape)
        {
            Close(
                false);

            e.Handled =
                true;
        }
    }
}
