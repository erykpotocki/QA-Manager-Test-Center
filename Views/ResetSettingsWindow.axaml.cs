using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ResetSettingsWindow : Window
{
    private readonly string _changedByLogin;
    private readonly UserProfileService _profileService = new();
    private readonly AssignmentService _assignmentService = new();
    private readonly JsonStorageService _storageService = new();

    public bool GlobalResetCompleted { get; private set; }

    public ResetSettingsWindow() : this("Administrator")
    {
    }

    public ResetSettingsWindow(string changedByLogin)
    {
        _changedByLogin = string.IsNullOrWhiteSpace(changedByLogin)
            ? "Administrator"
            : changedByLogin.Trim();
        InitializeComponent();
    }

    private async void ResetAllPinsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var confirmation = new ConfirmDeleteWindow(
            "Zresetować PIN wszystkim profilom?",
            "Każdy użytkownik otrzyma PIN 000000 i przy kolejnym logowaniu ustawi własny PIN.",
            "RESETUJ");
        if (!await confirmation.ShowDialog<bool>(this)) return;

        try
        {
            var count = await _profileService.ResetAllPinsAsync();
            await ShowSuccessAsync("PIN-y zostały zresetowane", $"Zresetowano PIN dla {count} profili.");
        }
        catch (Exception exception)
        {
            ShowResult($"Nie udało się zresetować PIN-ów: {exception.Message}", false);
        }
    }

    private async void ResetAssignmentsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var confirmation = new ConfirmDeleteWindow(
            "Wyzerować aktywne przypisania?",
            "Aktywne sesje zostaną wycofane, a ich przypadki ponownie odblokowane. Historia i raporty pozostaną.",
            "ZERUJ PRZYPISANIA");
        if (!await confirmation.ShowDialog<bool>(this)) return;

        try
        {
            var count = await _assignmentService.WithdrawAllActiveAssignmentsAsync(_changedByLogin);
            await ShowSuccessAsync("Przypisania zostały wyzerowane", $"Wycofano {count} aktywnych przypisań.");
        }
        catch (Exception exception)
        {
            ShowResult($"Nie udało się wyzerować przypisań: {exception.Message}", false);
        }
    }

    private async void ResetProgressButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var confirmation = new ConfirmDeleteWindow(
            "Zresetować postęp wykonania testów?",
            "Operacja usunie przypisania, historię sesji i powiadomienia oraz wyzeruje statusy przypadków.",
            "RESETUJ POSTĘP");
        if (!await confirmation.ShowDialog<bool>(this)) return;

        try
        {
            var removed = await _assignmentService.ResetAllAssignmentDataAsync();
            var data = await _storageService.LoadAsync();
            foreach (var testCase in data.TestCases) testCase.Status = "None";
            await _storageService.SaveAsync(data);
            await ShowSuccessAsync("Postęp testów został zresetowany",
                $"Usunięto {removed} sesji i wyzerowano {data.TestCases.Count} przypadków.");
        }
        catch (Exception exception)
        {
            ShowResult($"Nie udało się zresetować postępu: {exception.Message}", false);
        }
    }

    private async void GlobalResetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var pin = GlobalResetPinTextBox.Text?.Trim() ?? string.Empty;
        if (!UserProfileService.IsValidPin(pin))
        {
            ShowResult("Wpisz poprawny, 6-cyfrowy PIN aktualnie zalogowanego użytkownika.", false);
            GlobalResetPinTextBox.Focus();
            return;
        }

        var authentication = await _profileService.AuthenticateAsync(_changedByLogin, pin);
        if (authentication.Status != AuthenticationStatus.Success)
        {
            ShowResult("PIN aktualnie zalogowanego użytkownika jest nieprawidłowy.", false);
            GlobalResetPinTextBox.SelectAll();
            return;
        }

        var confirmation = new ConfirmDeleteWindow(
            "Wykonać globalny reset testowy?",
            "PIN-y, wygląd, przypisania, sesje, powiadomienia, statusy i komentarze zostaną przywrócone do wartości testowych.",
            "RESETUJ ŚRODOWISKO");
        if (!await confirmation.ShowDialog<bool>(this)) return;

        var busyWindow = new BusyOperationWindow(
            "Resetowanie danych testowych",
            "Przywracanie kont, wyglądu, przypisań, sesji i statusów. Po zakończeniu nastąpi wylogowanie.",
            async () =>
            {
                await _assignmentService.ResetAllAssignmentDataAsync();
                var data = await _storageService.LoadAsync();
                foreach (var testCase in data.TestCases)
                {
                    testCase.Status = "None";
                    testCase.Comment = string.Empty;
                }
                await _storageService.SaveAsync(data);
                await _profileService.ResetAllProfilesForTestAsync();
                SessionManager.DeleteAllLocalSessions();
                ApplicationAppearanceService.ResetAllProfilesToTestDefaults();
            });

        await busyWindow.ShowDialog(this);
        if (busyWindow.OperationException is Exception exception)
        {
            ShowResult($"Globalny reset nie powiódł się: {exception.Message}", false);
            return;
        }

        GlobalResetCompleted = true;
        Close();
    }

    private async Task ShowSuccessAsync(string title, string message)
    {
        ShowResult("✓ Operacja zakończona", true);
        await new OperationResultWindow(true, title, message).ShowDialog(this);
    }

    private void ShowResult(string message, bool success)
    {
        ResultTextBlock.Text = message;
        ResultTextBlock.Foreground = success ? Brushes.SeaGreen : Brushes.IndianRed;
        ResultTextBlock.IsVisible = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
