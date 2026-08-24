using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class AdminTestMenuWindow : Window
{
    private readonly UserProfileService _profileService =
        new();
    private readonly AssignmentService _assignmentService =
        new();
    private readonly JsonStorageService _storageService =
        new();

    private ComboBox? _profileComboBox;
    private StackPanel? _selectedProfileActionsPanel;
    private Button? _deleteSelectedUserButton;
    private TextBlock? _resultTextBlock;
    private Border? _resetAllProfilesPanel;
    private Border? _roleManagementPanel;
    private Border? _accountManagementPanel;
    private StackPanel? _resetAssignmentsPanel;
    private CheckBox? _administratorRoleCheckBox;
    private CheckBox? _leaderRoleCheckBox;
    private CheckBox? _testerRoleCheckBox;
    private TextBox? _additionalProjectRolesTextBox;
    private TextBlock? _roleSaveResultTextBlock;
    private TextBox? _newUserLoginTextBox;
    private TextBox? _globalResetPinTextBox;
    private readonly bool _canResetAllProfiles;
    private readonly bool _canManageRoles;
    private readonly bool _canCreateUsers;
    private readonly string _changedByLogin;

    public AdminTestMenuWindow()
        : this(
            "Administrator",
            "Administrator")
    {
    }

    public AdminTestMenuWindow(
        string highestSystemRole,
        string changedByLogin = "Administrator")
    {
        _changedByLogin =
            string.IsNullOrWhiteSpace(
                changedByLogin)
                ? "Administrator"
                : changedByLogin.Trim();

        _canResetAllProfiles =
            string.Equals(
                highestSystemRole,
                "Administrator",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                highestSystemRole,
                SystemRoleService.LeaderRole,
                StringComparison.OrdinalIgnoreCase);

        _canManageRoles =
            _canResetAllProfiles;

        _canCreateUsers =
            string.Equals(
                highestSystemRole,
                SystemRoleService.AdministratorRole,
                StringComparison.OrdinalIgnoreCase);

        AvaloniaXamlLoader.Load(
            this);

        _profileComboBox =
            this.FindControl<ComboBox>(
                "ProfileComboBox");

        _selectedProfileActionsPanel =
            this.FindControl<StackPanel>(
                "SelectedProfileActionsPanel");

        _deleteSelectedUserButton =
            this.FindControl<Button>(
                "DeleteSelectedUserButton");

        _resultTextBlock =
            this.FindControl<TextBlock>(
                "ResultTextBlock");

        _resetAllProfilesPanel =
            this.FindControl<Border>(
                "ResetAllProfilesPanel");

        _roleManagementPanel =
            this.FindControl<Border>(
                "RoleManagementPanel");

        _accountManagementPanel =
            this.FindControl<Border>(
                "AccountManagementPanel");

        _resetAssignmentsPanel =
            this.FindControl<StackPanel>(
                "ResetAssignmentsPanel");

        _administratorRoleCheckBox =
            this.FindControl<CheckBox>(
                "AdministratorRoleCheckBox");

        _leaderRoleCheckBox =
            this.FindControl<CheckBox>(
                "LeaderRoleCheckBox");

        _testerRoleCheckBox =
            this.FindControl<CheckBox>(
                "TesterRoleCheckBox");

        _additionalProjectRolesTextBox =
            this.FindControl<TextBox>(
                "AdditionalProjectRolesTextBox");

        _roleSaveResultTextBlock =
            this.FindControl<TextBlock>(
                "RoleSaveResultTextBlock");

        _newUserLoginTextBox =
            this.FindControl<TextBox>(
                "NewUserLoginTextBox");

        _globalResetPinTextBox =
            this.FindControl<TextBox>(
                "GlobalResetPinTextBox");

        if (_resetAllProfilesPanel is not null)
        {
            _resetAllProfilesPanel.IsVisible =
                _canResetAllProfiles;
        }

        if (_roleManagementPanel is not null)
        {
            _roleManagementPanel.IsVisible =
                _canManageRoles;
        }

        if (_accountManagementPanel is not null)
        {
            _accountManagementPanel.IsVisible =
                _canCreateUsers;
        }

        if (_resetAssignmentsPanel is not null)
        {
            _resetAssignmentsPanel.IsVisible =
                _canResetAllProfiles;
        }

        if (_profileComboBox is not null)
        {
            _profileComboBox.SelectionChanged +=
                (_, _) =>
                    UpdateRoleSelection();
        }

        Opened +=
            async (_, _) =>
            {
                await LoadProfilesAsync();
            };
    }

    private async Task LoadProfilesAsync()
    {
        if (_profileComboBox is null)
        {
            return;
        }

        var profiles =
            await _profileService.GetProfilesAsync();

        _profileComboBox.Items.Clear();

        _profileComboBox.Items.Add(
            new ComboBoxItem
            {
                Content =
                    "Brak — nie wybrano profilu"
            });

        foreach (var profile in profiles)
        {
            _profileComboBox.Items.Add(
                new ComboBoxItem
                {
                    Content =
                        CreateProfileLabel(
                            profile),

                    Tag =
                        profile
                });

        }

        _profileComboBox.SelectedIndex =
            0;

        if (_selectedProfileActionsPanel is not null)
        {
            _selectedProfileActionsPanel.IsVisible =
                false;
        }
    }

    private static string CreateProfileLabel(
        UserProfileModel profile)
    {
        var role =
            SystemRoleService.GetDisplayName(
                SystemRoleService.GetHighestRole(
                    profile.SystemRoles));

        return $"{profile.Login} — {role}";
    }

    private void UpdateRoleSelection()
    {
        if (_roleSaveResultTextBlock is not null)
        {
            _roleSaveResultTextBlock.IsVisible =
                false;
        }

        if (_profileComboBox?.SelectedItem is not
                ComboBoxItem selectedItem ||
            selectedItem.Tag is not
                UserProfileModel profile)
        {
            if (_selectedProfileActionsPanel is not null)
            {
                _selectedProfileActionsPanel.IsVisible =
                    false;
            }

            return;
        }

        if (_selectedProfileActionsPanel is not null)
        {
            _selectedProfileActionsPanel.IsVisible =
                true;
        }

        if (_deleteSelectedUserButton is not null)
        {
            var isCurrentProfile =
                string.Equals(
                    profile.Login,
                    _changedByLogin,
                    StringComparison.OrdinalIgnoreCase);

            _deleteSelectedUserButton.IsEnabled =
                !isCurrentProfile;

            ToolTip.SetTip(
                _deleteSelectedUserButton,
                isCurrentProfile
                    ? "Nie można usunąć aktualnie zalogowanego konta."
                    : "Usuń wybrany profil");
        }

        SetChecked(
            _administratorRoleCheckBox,
            profile.SystemRoles,
            SystemRoleService.AdministratorRole);

        var isDedicatedAdministrator =
            string.Equals(
                profile.Login,
                "admin",
                StringComparison.OrdinalIgnoreCase);

        if (_administratorRoleCheckBox is not null)
        {
            _administratorRoleCheckBox.IsEnabled =
                !isDedicatedAdministrator;

            if (isDedicatedAdministrator)
            {
                _administratorRoleCheckBox.IsChecked =
                    true;
            }
        }

        SetChecked(
            _leaderRoleCheckBox,
            profile.SystemRoles,
            SystemRoleService.LeaderRole);

        SetChecked(
            _testerRoleCheckBox,
            profile.SystemRoles,
            SystemRoleService.TesterRole);

        if (_additionalProjectRolesTextBox is not null)
        {
            _additionalProjectRolesTextBox.Text =
                string.Join(
                    ", ",
                    profile.ProjectRoles.Where(
                        role =>
                            !string.Equals(role, "NOVA", StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static void SetChecked(
        CheckBox? checkBox,
        System.Collections.Generic.IEnumerable<string> roles,
        string role)
    {
        if (checkBox is not null)
        {
            checkBox.IsChecked =
                roles.Contains(
                    role,
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    private async void SaveRolesButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canManageRoles ||
            _profileComboBox?.SelectedItem is not
                ComboBoxItem selectedItem ||
            selectedItem.Tag is not
                UserProfileModel profile)
        {
            return;
        }

        var systemRoles =
            new[]
            {
                (_administratorRoleCheckBox?.IsChecked == true,
                    SystemRoleService.AdministratorRole),
                (_leaderRoleCheckBox?.IsChecked == true,
                    SystemRoleService.LeaderRole),
                (_testerRoleCheckBox?.IsChecked == true,
                    SystemRoleService.TesterRole)
            }
            .Where(
                item =>
                    item.Item1)
            .Select(
                item =>
                    item.Item2)
            .ToArray();

        var projectRoles =
            (_additionalProjectRolesTextBox?.Text ?? string.Empty)
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();

        try
        {
            await _profileService.UpdateRolesAsync(
                profile.Id,
                systemRoles,
                projectRoles.ToArray());

            ShowResult(
                $"Role profilu „{profile.Login}” zostały zapisane.",
                true);

            ShowRoleSaveResult(
                "Zapisano",
                true);

        }
        catch (Exception)
        {
            ShowResult(
                $"Nie udało się zapisać ról profilu „{profile.Login}”.",
                false);

            ShowRoleSaveResult(
                "Nie udało się zapisać",
                false);
        }
    }

    private void ShowRoleSaveResult(
        string message,
        bool success)
    {
        if (_roleSaveResultTextBlock is null)
        {
            return;
        }

        _roleSaveResultTextBlock.Text =
            message;

        _roleSaveResultTextBlock.Foreground =
            success
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;

        _roleSaveResultTextBlock.IsVisible =
            true;
    }

    private async void ResetSelectedButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await ResetSelectedProfileAsync();
    }

    private async Task ResetSelectedProfileAsync()
    {
        if (_profileComboBox?.SelectedItem is not
                ComboBoxItem selectedItem ||
            selectedItem.Tag is not UserProfileModel profile)
        {
            ShowResult(
                "Najpierw wybierz profil.",
                false);

            return;
        }

        var login =
            selectedItem.Content
                ?.ToString()
                ?.Split(
                    " — ",
                    StringSplitOptions.None)[0]
            ?? "wybranego użytkownika";

        var confirmation =
            new ConfirmDeleteWindow(
                "Zresetować PIN użytkownika?",
                $"Profil „{login}” otrzyma PIN 000000 i przy następnym logowaniu będzie musiał ustawić nowy PIN.",
                "RESETUJ");

        if (!await confirmation.ShowDialog<bool>(
                this))
        {
            return;
        }

        try
        {
            await _profileService.ResetPinAsync(
                profile.Id);

            var message =
                $"PIN użytkownika „{login}” został prawidłowo zresetowany do 000000.";

            ShowResult(
                message,
                true);

            await ShowOperationResultAsync(
                true,
                "PIN został zresetowany",
                message);

            await LoadProfilesAsync();
        }
        catch (Exception)
        {
            var message =
                $"Nie udało się zresetować PIN-u użytkownika „{login}”. Spróbuj ponownie.";

            ShowResult(
                message,
                false);

            await ShowOperationResultAsync(
                false,
                "Reset PIN-u nie powiódł się",
                message);
        }
    }

    private async void ResetAllButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canResetAllProfiles)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Zresetować PIN wszystkim profilom?",
                "Ta operacja ustawi PIN 000000 każdemu użytkownikowi, również administratorowi. Przy kolejnym logowaniu każdy będzie musiał ustawić własny PIN.",
                "RESETUJ");

        if (!await confirmation.ShowDialog<bool>(
                this))
        {
            return;
        }

        try
        {
            var resetCount =
                await _profileService.ResetAllPinsAsync();

            var message =
                $"Prawidłowo zresetowano PIN dla {resetCount} profili.";

            ShowResult(
                message,
                true);

            await ShowOperationResultAsync(
                true,
                "PIN-y zostały zresetowane",
                message);

            await LoadProfilesAsync();
        }
        catch (Exception)
        {
            const string message =
                "Nie udało się zresetować PIN-ów użytkowników. Spróbuj ponownie.";

            ShowResult(
                message,
                false);

            await ShowOperationResultAsync(
                false,
                "Reset PIN-ów nie powiódł się",
                message);
        }
    }

    private async void AddUserButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canCreateUsers)
        {
            ShowResult(
                "Tworzenie kont jest dostępne wyłącznie dla Administratora.",
                false);

            return;
        }

        var login =
            _newUserLoginTextBox?.Text?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                login))
        {
            ShowResult(
                "Podaj login nowego użytkownika.",
                false);

            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Utworzyć nowe konto?",
                $"Zostanie utworzone konto „{login}”. Nowy profil otrzyma wyłącznie podstawowe uprawnienia.",
                "UTWÓRZ KONTO");

        var confirmed =
            await confirmation.ShowDialog<bool>(
                this);

        if (!confirmed)
        {
            return;
        }

        try
        {
            var profile =
                await _profileService.CreateUserAsync(
                    login);

            if (_newUserLoginTextBox is not null)
            {
                _newUserLoginTextBox.Text =
                    string.Empty;
            }

            var message =
                $"Utworzono konto „{profile.Login}” z PIN-em 000000.";

            ShowResult(
                message,
                true);

            await ShowOperationResultAsync(
                true,
                "Konto zostało utworzone",
                message);

            await LoadProfilesAsync();
        }
        catch (Exception exception)
        {
            ShowResult(
                exception.Message,
                false);
        }
    }

    private async void DeleteSelectedUserButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canManageRoles ||
            _profileComboBox?.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not UserProfileModel profile)
        {
            ShowResult(
                "Wybierz konto, które chcesz usunąć.",
                false);

            return;
        }

        var activeAssignments =
            await _assignmentService.GetActiveAssignmentsForUserAsync(
                profile.Login);

        if (activeAssignments.Length > 0)
        {
            ShowResult(
                "Najpierw wycofaj aktywne przypisania tego użytkownika.",
                false);

            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Usunąć konto użytkownika?",
                $"Profil „{profile.Login}” zostanie trwale usunięty.\nTej operacji nie można cofnąć.",
                "USUŃ KONTO");

        if (!await confirmation.ShowDialog<bool>(
                this))
        {
            return;
        }

        try
        {
            await _profileService.DeleteUserAsync(
                profile.Id,
                _changedByLogin);

            var message =
                $"Konto „{profile.Login}” zostało usunięte.";

            ShowResult(
                message,
                true);

            await ShowOperationResultAsync(
                true,
                "Konto zostało usunięte",
                message);

            await LoadProfilesAsync();
        }
        catch (Exception exception)
        {
            ShowResult(
                exception.Message,
                false);
        }
    }

    private async void ResetAssignmentsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canResetAllProfiles)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Wyzerować wszystkie aktywne przypisania?",
                "Wszystkie aktywne sesje przypisane użytkownikom zostaną wycofane, a przypadki ponownie staną się dostępne. Historia i raporty pozostaną bez zmian.",
                "ZERUJ PRZYPISANIA");

        if (!await confirmation.ShowDialog<bool>(
                this))
        {
            return;
        }

        try
        {
            var resetCount =
                await _assignmentService.WithdrawAllActiveAssignmentsAsync(
                    _changedByLogin);

            var message =
                resetCount == 0
                    ? "Nie znaleziono aktywnych przypisań do wyzerowania."
                    : $"Wyzerowano {resetCount} aktywnych przypisań. Przypadki zostały odblokowane.";

            ShowResult(
                message,
                true);

            await ShowOperationResultAsync(
                true,
                "Przypisania zostały wyzerowane",
                message);
        }
        catch (Exception)
        {
            const string message =
                "Nie udało się wyzerować aktywnych przypisań. Spróbuj ponownie.";

            ShowResult(
                message,
                false);

            await ShowOperationResultAsync(
                false,
                "Zerowanie przypisań nie powiodło się",
                message);
        }
    }

    private async void ResetAllAssignmentDataButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canResetAllProfiles)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Zresetować postęp wykonania testów?",
                "Operacja jest nieodwracalna. Usunie aktywne, ukończone i zarchiwizowane przypisania, ich historię oraz powiązane powiadomienia. Wszystkie przypadki otrzymają status niewykonany.",
                "RESETUJ POSTĘP");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        try
        {
            var removedAssignments =
                await _assignmentService.ResetAllAssignmentDataAsync();

            var testData =
                await _storageService.LoadAsync();

            foreach (var testCase in testData.TestCases)
            {
                testCase.Status = "None";
            }

            await _storageService.SaveAsync(testData);

            var message =
                $"Wyczyszczono {removedAssignments} sesji i przywrócono {testData.TestCases.Count} przypadków do stanu niewykonanego.";

            ShowResult(message, true);
            await ShowOperationResultAsync(
                true,
                "Postęp wykonania testów został zresetowany",
                message);
        }
        catch (Exception)
        {
            const string message =
                "Nie udało się zresetować postępu wykonania testów.";

            ShowResult(message, false);
            await ShowOperationResultAsync(
                false,
                "Reset postępu nie powiódł się",
                message);
        }
    }

    private async void GlobalTestResetButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canResetAllProfiles || _globalResetPinTextBox is null)
        {
            return;
        }

        var pin = _globalResetPinTextBox.Text?.Trim() ?? string.Empty;
        if (!UserProfileService.IsValidPin(pin))
        {
            ShowResult("Wpisz poprawny, 6-cyfrowy PIN aktualnie zalogowanego użytkownika.", false);
            _globalResetPinTextBox.Focus();
            _globalResetPinTextBox.SelectAll();
            return;
        }

        var authentication = await _profileService.AuthenticateAsync(_changedByLogin, pin);
        if (authentication.Status != AuthenticationStatus.Success)
        {
            ShowResult("PIN aktualnie zalogowanego użytkownika jest nieprawidłowy.", false);
            _globalResetPinTextBox.SelectAll();
            return;
        }

        var confirmation = new ConfirmDeleteWindow(
            "Wykonać globalny reset testowy?",
            "To operacja nieodwracalna. Wszystkie PIN-y zostaną ustawione na 000000, a przypisania, powiadomienia, sesje, statusy i komentarze zostaną wyzerowane. Konta, role i struktura przypadków pozostaną.",
            "RESETUJ ŚRODOWISKO");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        try
        {
            var removedAssignments = await _assignmentService.ResetAllAssignmentDataAsync();
            var testData = await _storageService.LoadAsync();
            foreach (var testCase in testData.TestCases)
            {
                testCase.Status = "None";
                testCase.Comment = string.Empty;
            }

            await _storageService.SaveAsync(testData);
            var resetProfiles = await _profileService.ResetAllProfilesForTestAsync();
            var removedSessions = SessionManager.DeleteAllLocalSessions();
            ApplicationAppearanceService.ResetAllProfilesToTestDefaults();
            _globalResetPinTextBox.Text = string.Empty;

            var message =
                $"Reset zakończony: {resetProfiles} kont, {testData.TestCases.Count} przypadków, " +
                $"{removedAssignments} przypisań i {removedSessions} sesji. Przy następnym logowaniu użyj PIN-u 000000.";

            ShowResult(message, true);
            await ShowOperationResultAsync(true, "Globalny reset zakończony", message);
        }
        catch (Exception exception)
        {
            ShowResult($"Globalny reset nie powiódł się: {exception.Message}", false);
            await ShowOperationResultAsync(
                false,
                "Globalny reset nie powiódł się",
                "Nie udało się przywrócić środowiska testowego. Żadne dalsze operacje nie zostały wykonane.");
        }
    }

    private async Task ShowOperationResultAsync(
        bool success,
        string title,
        string message)
    {
        var dialog =
            new OperationResultWindow(
                success,
                title,
                message);

        await dialog.ShowDialog(
            this);
    }

    private async void OpenRoleAndProjectEditorButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canManageRoles)
        {
            return;
        }

        var dialog = new RoleManagementWindow(_changedByLogin);
        await dialog.ShowDialog(this);
        await LoadProfilesAsync();
    }

    private void CloseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private async void OpenAssignmentArchiveButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var archiveWindow =
            new AssignmentArchiveWindow();

        await archiveWindow.ShowDialog(
            this);
    }

    private void ShowResult(
        string message,
        bool success)
    {
        if (_resultTextBlock is null)
        {
            return;
        }

        _resultTextBlock.Text =
            message;

        _resultTextBlock.Foreground =
            success
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;

        _resultTextBlock.IsVisible =
            true;
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Escape)
        {
            Close();

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Enter &&
            _newUserLoginTextBox?.IsFocused == true)
        {
            AddUserButton_OnClick(
                null,
                new RoutedEventArgs());

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Enter &&
            _additionalProjectRolesTextBox?.IsFocused == true)
        {
            SaveRolesButton_OnClick(
                null,
                new RoutedEventArgs());

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Enter &&
            _profileComboBox?.IsFocused == true &&
            _profileComboBox.IsDropDownOpen != true &&
            _profileComboBox.SelectedItem is ComboBoxItem selectedProfileItem &&
            selectedProfileItem.Tag is UserProfileModel)
        {
            _ =
                ResetSelectedProfileAsync();

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Delete &&
            e.Source is not TextBox &&
            _deleteSelectedUserButton?.IsEnabled == true &&
            _profileComboBox?.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Tag is UserProfileModel)
        {
            DeleteSelectedUserButton_OnClick(
                null,
                new RoutedEventArgs());

            e.Handled =
                true;
        }
    }
}
