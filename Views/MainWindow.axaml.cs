using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class MainWindow : Window
{
    public event Action? LogoutRequested;

    private readonly SessionManager _sessionManager;
    private readonly AssignmentService _assignmentService =
        new();

    private readonly string _loggedInLogin;
    private readonly string _highestSystemRole;
    private readonly IReadOnlyList<string> _systemRoles;
    private readonly IReadOnlyList<string> _projectRoles;
    private readonly Dictionary<string, string> _projectRoleColors =
        new(StringComparer.OrdinalIgnoreCase);

    private SessionStateModel _sessionState =
        SessionManager.CreateNewSession();

    private bool _isDarkMode;
    private bool _startupSessionChecked;
    private bool _logoutConfirmationOpen;
    private TestAssignmentModel[] _activeAssignments =
        Array.Empty<TestAssignmentModel>();
    private readonly DispatcherTimer _assignmentGlowTimer =
        new()
        {
            Interval = TimeSpan.FromMilliseconds(1600)
        };
    private bool _assignmentGlowBright;
    private bool _isRefreshing;
    private double _refreshIndicatorAngle;
    private readonly RotateTransform _refreshIndicatorRotateTransform = new();
    private readonly DispatcherTimer _refreshIndicatorTimer =
        new()
        {
            Interval = TimeSpan.FromMilliseconds(55)
        };
    public MainWindow()
        : this(
            (UserProfileModel?)null)
    {
    }

    public MainWindow(
        string? testerName)
        : this(
            testerName,
            new[]
            {
                SystemRoleService.TesterRole
            },
            Array.Empty<string>())
    {
    }

    public MainWindow(
        UserProfileModel? profile)
        : this(
            profile?.Login,
            profile?.SystemRoles,
            profile?.ProjectRoles)
    {
    }

    private MainWindow(
        string? testerName,
        IEnumerable<string>? systemRoles,
        IEnumerable<string>? projectRoles)
    {
        InitializeComponent();

        RefreshIndicatorIcon.RenderTransform =
            _refreshIndicatorRotateTransform;

        _refreshIndicatorTimer.Tick +=
            (_, _) =>
            {
                _refreshIndicatorAngle =
                    (_refreshIndicatorAngle + 24) % 360;
                _refreshIndicatorRotateTransform.Angle =
                    _refreshIndicatorAngle;
            };

        _loggedInLogin =
            string.IsNullOrWhiteSpace(testerName)
                ? "nieznany"
                : testerName.Trim();

        _systemRoles =
            (systemRoles ?? new[]
            {
                SystemRoleService.TesterRole
            })
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _projectRoles =
            (projectRoles ?? Enumerable.Empty<string>())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _highestSystemRole =
            SystemRoleService.GetHighestRole(
                _systemRoles);

        _sessionManager =
            new SessionManager(
                _loggedInLogin);

        LoggedInUserTextBlock.Text =
            string.Format(
                LocalizationService.T("Common.LoggedIn"),
                _loggedInLogin);

        LocalizationService.LanguageChanged +=
            LocalizationService_OnLanguageChanged;

        Closed +=
            (_, _) =>
                LocalizationService.LanguageChanged -=
                    LocalizationService_OnLanguageChanged;

        BuildRoleBadges();

        ProjectComboBox.Items.Clear();
        ProjectComboBox.IsEnabled = false;
        StartTestButton.IsEnabled = false;

        _assignmentGlowTimer.Tick +=
            (_, _) =>
            {
                if (!ExecuteAssignedTestsButton.IsVisible)
                {
                    _assignmentGlowTimer.Stop();
                    return;
                }

                _assignmentGlowBright =
                    !_assignmentGlowBright;

                ExecuteAssignedTestsButton.Opacity =
                    _assignmentGlowBright
                        ? 1
                        : 0.9;

                ExecuteAssignedTestsButton.BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(
                            _assignmentGlowBright
                                ? "#A8CCF1"
                                : "#397FCA"));
            };

        _isDarkMode =
            Application.Current?.RequestedThemeVariant ==
            ThemeVariant.Dark;

        UpdateLanguagePicker();
        UpdateThemeButton();

        StartTestButton.Click +=
            StartTestButton_OnClick;

        Opened +=
            MainWindow_OnOpened;

        AddHandler(
            InputElement.KeyDownEvent,
            MainWindow_OnKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private async void MainWindow_OnOpened(
        object? sender,
        EventArgs e)
    {
        await ConfigureAvailableProjectsAsync();
        await RefreshAssignmentAndNotificationStateAsync();

        if (_startupSessionChecked)
        {
            return;
        }

        _startupSessionChecked =
            true;

        _sessionState =
            await _sessionManager.LoadAsync();

        if (string.Equals(
                _sessionState.SessionMode,
                "Assigned",
                StringComparison.OrdinalIgnoreCase))
        {
            var savedIds =
                _sessionState.AssignmentIds.ToHashSet();

            var resumableAssignments =
                savedIds.Count > 0
                    ? _activeAssignments
                        .Where(
                            assignment =>
                                savedIds.Contains(
                                    assignment.Id))
                        .ToArray()
                    : _activeAssignments
                        .Where(
                            assignment =>
                                string.Equals(
                                    assignment.ProjectName,
                                    _sessionState.ProjectKey,
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    assignment.ApplicationVersion,
                                    _sessionState.ApplicationVersion,
                                    StringComparison.OrdinalIgnoreCase))
                        .ToArray();

            var unavailableIds =
                savedIds
                    .Except(
                        resumableAssignments.Select(
                            assignment =>
                                assignment.Id))
                    .ToArray();

            if (unavailableIds.Length > 0)
            {
                await ShowUnavailableAssignedSessionAsync(
                    unavailableIds);
            }

            if (resumableAssignments.Length == 0)
            {
                await _sessionManager.InvalidateAssignedSessionAsync(
                    _sessionState);

                return;
            }

            _activeAssignments =
                resumableAssignments;

            if (savedIds.Count == 0 ||
                unavailableIds.Length > 0)
            {
                await _sessionManager.UpdateAssignmentContextAsync(
                    _sessionState,
                    resumableAssignments.Select(
                        assignment =>
                            assignment.Id));
            }
        }

        if (!_sessionManager.ShouldAskToContinue(
                _sessionState))
        {
            return;
        }

        var dialog =
            new ContinueSessionWindow(
                GetSafeValue(
                    _sessionState.ProjectKey,
                    DemoCatalog.PrimaryProjectName),
                GetSafeValue(
                    _sessionState.ApplicationVersion,
                    "Nie podano"),
                GetSafeValue(
                    _sessionState.TesterName,
                    "Nie podano"),
                GetSafeValue(
                    _sessionState.SessionMode,
                    "Assigned"),
                GetSafeValue(
                    _sessionState.LastOpenedTestName,
                    GetTestTypeDisplayName(
                        _sessionState.LastOpenedTestType)),
                _sessionState.LastSaveTime);

        var result =
            await dialog.ShowDialog<ContinueSessionResult>(
                this);

        if (result ==
            ContinueSessionResult.Continue)
        {
            await ContinueSavedSessionAsync();
            return;
        }
    }

    private void ThemeToggleButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var current =
            ApplicationAppearanceService.Current;

        ApplicationAppearanceService.SaveAndApply(
            new ApplicationAppearanceSettings
            {
                Theme =
                    string.Equals(
                        current.Theme,
                        "Dark",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Light"
                        : "Dark",
                FontFamily = current.FontFamily,
                TextSize = current.TextSize,
                UseSemiBoldText = current.UseSemiBoldText
            });

        _isDarkMode =
            Application.Current?.RequestedThemeVariant ==
            ThemeVariant.Dark;

        UpdateThemeButton();
        UpdateLanguagePicker();

        RefreshRoleBadges();

        Dispatcher.UIThread.Post(
            () => StartTestButton.Focus());
    }

    private async void LogoutButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await ConfirmLogoutAsync();
    }

    private async void MainWindow_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;

            if (Content is ExplorerView explorer)
            {
                await explorer.RefreshAsync();
            }
            else
            {
                await RefreshCurrentViewAsync();
            }

            return;
        }

        if (_logoutConfirmationOpen)
        {
            return;
        }

        if (Content is ExplorerView explorerView)
        {
            if (e.Key == Key.Enter &&
                explorerView.TryHandleEnter(
                    e.Source))
            {
                e.Handled =
                    true;

                return;
            }

            if (e.Key == Key.Escape)
            {
                e.Handled =
                    true;

                await HandleEscapeShortcutAsync();
            }

            return;
        }

        if (e.Key == Key.Enter &&
            !ProjectComboBox.IsDropDownOpen)
        {
            e.Handled =
                true;

            await StartTestAsync();

            return;
        }

        if (e.Key == Key.Escape)
        {
            e.Handled =
                true;

            await HandleEscapeShortcutAsync();
        }
    }

    private async Task HandleEscapeShortcutAsync()
    {
        await ConfirmLogoutAsync();
    }

    private async Task ConfirmLogoutAsync()
    {
        if (_logoutConfirmationOpen)
        {
            return;
        }

        _logoutConfirmationOpen =
            true;

        var dialog =
            new ConfirmLogoutWindow(
                _loggedInLogin);

        try
        {
            var confirmed =
                await dialog.ShowDialog<bool>(
                    this);

            if (!confirmed)
            {
                return;
            }

            RequestLogout();
        }
        finally
        {
            _logoutConfirmationOpen =
                false;
        }
    }

    private void RequestLogout()
    {
        LogoutRequested?.Invoke();
    }

    private async void StartTestButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await StartTestAsync();
    }

    private async void RefreshButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (Content is ExplorerView explorer)
        {
            await explorer.RefreshAsync();
            return;
        }

        await RefreshCurrentViewAsync();
    }

    private async Task RefreshCurrentViewAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        RefreshIndicatorBorder.IsVisible = true;
        _refreshIndicatorTimer.Start();

        try
        {
            await RefreshAssignmentAndNotificationStateAsync();
        }
        finally
        {
            _refreshIndicatorTimer.Stop();
            _refreshIndicatorRotateTransform.Angle = 0;
            RefreshIndicatorBorder.IsVisible = false;
            _isRefreshing = false;
        }
    }

    private bool CanManageRoles =>
        string.Equals(
            _highestSystemRole,
            SystemRoleService.AdministratorRole,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            _highestSystemRole,
            SystemRoleService.LeaderRole,
            StringComparison.OrdinalIgnoreCase);

    private async Task ConfigureAvailableProjectsAsync()
    {
        ProjectComboBox.Items.Clear();

        var profileService = new UserProfileService();
        var definitions = await profileService.GetRoleAndProjectDefinitionsAsync();
        _projectRoleColors.Clear();
        foreach (var role in definitions.Roles)
        {
            _projectRoleColors[role.Name] = role.BorderColor;
        }
        RefreshRoleBadges();
        var isAdministrator = _systemRoles.Contains(
            SystemRoleService.AdministratorRole,
            StringComparer.OrdinalIgnoreCase);

        var accessibleProjectKeys = isAdministrator
            ? definitions.Projects.Select(project => project.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : definitions.Roles
                .Where(role => _projectRoles.Contains(
                    role.Name,
                    StringComparer.OrdinalIgnoreCase))
                .SelectMany(role => role.ProjectKeys)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var project in definitions.Projects.Where(project =>
                     accessibleProjectKeys.Contains(project.Key)))
        {
            ProjectComboBox.Items.Add(new ComboBoxItem
            {
                Content = project.Name,
                Tag = project.Key
            });
        }

        var hasProjects = ProjectComboBox.ItemCount > 0;
        ProjectComboBox.SelectedIndex = hasProjects ? 0 : -1;
        ProjectComboBox.IsEnabled = hasProjects;
        StartTestButton.IsEnabled = hasProjects;
        NoProjectMessageTextBlock.IsVisible = !hasProjects;
    }
    private async void ManageRolesButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!CanManageRoles)
        {
            return;
        }

        var dialog =
            new RoleManagementWindow(
                _loggedInLogin);

        await dialog.ShowDialog(
            this);
    }

    private async void NetworkSyncButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog = new NetworkSyncWindow(
            CanManageRoles);
        await dialog.ShowDialog(this);
    }

    private async Task StartTestAsync()
    {
        if (ProjectComboBox.SelectedItem is null)
        {
            return;
        }

        var projectName =
            GetSelectedProjectName();

        var applicationVersion =
            string.Empty;

        var testerName =
            _loggedInLogin;

        await _sessionManager.MarkSessionStartedAsync(
            _sessionState,
            projectName,
            applicationVersion,
            testerName,
            "AdHoc");

        Content =
            new ExplorerView(
                projectName,
                applicationVersion,
                testerName,
                _sessionManager,
                _sessionState,
                _loggedInLogin,
                RequestLogout,
                _highestSystemRole,
                _systemRoles,
                _projectRoles);
    }

    private async Task ContinueSavedSessionAsync()
    {
        var savedIds =
            _sessionState.AssignmentIds.ToHashSet();

        await RefreshAssignmentAndNotificationStateAsync();

        var resumableAssignments =
            _activeAssignments
                .Where(
                    assignment =>
                        savedIds.Count == 0 ||
                        savedIds.Contains(
                            assignment.Id))
                .ToArray();

        if (resumableAssignments.Length == 0)
        {
            await ShowUnavailableAssignedSessionAsync(
                savedIds);

            await _sessionManager.InvalidateAssignedSessionAsync(
                _sessionState);

            return;
        }

        _activeAssignments =
            resumableAssignments;

        await _sessionManager.UpdateAssignmentContextAsync(
            _sessionState,
            resumableAssignments.Select(
                assignment =>
                    assignment.Id));

        var projectName =
            GetSafeValue(
                _sessionState.ProjectKey,
                DemoCatalog.PrimaryProjectName);

        var applicationVersion =
            _sessionState.ApplicationVersion
                ?.Trim()
            ?? string.Empty;

        var testerName =
            _loggedInLogin;

        var explorer =
            new ExplorerView(
                projectName,
                applicationVersion,
                testerName,
                _sessionManager,
            _sessionState,
            _loggedInLogin,
            RequestLogout,
            _highestSystemRole,
                _systemRoles,
                _projectRoles);

        Content =
            explorer;

        if (string.Equals(
                _sessionState.SessionMode,
                "Assigned",
                StringComparison.OrdinalIgnoreCase))
        {
            await explorer.ExecuteLatestAssignmentAsync();
        }
    }

    private async Task ShowUnavailableAssignedSessionAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.Distinct().ToArray();

        var assignments =
            await _assignmentService.GetAssignmentsByIdsAsync(
                ids);

        var managers =
            assignments
                .Select(
                    assignment =>
                        string.IsNullOrWhiteSpace(
                            assignment.WithdrawnByLogin)
                            ? assignment.AssignedByLogin
                            : assignment.WithdrawnByLogin)
                .Where(
                    login =>
                        !string.IsNullOrWhiteSpace(login))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var contactText =
            managers.Length == 0
                ? "Skontaktuj się z osobą zarządzającą sesją."
                : $"Skontaktuj się z osobą zarządzającą sesją: {string.Join(", ", managers)}.";

        await new OperationResultWindow(
                false,
                "Sesja została wstrzymana",
                $"Te testy zostały wstrzymane lub usunięte i nie są już dostępne. {contactText}")
            .ShowDialog(this);
    }

    private string GetSelectedProjectName()
    {
        if (ProjectComboBox.SelectedItem
                is ComboBoxItem selectedProject &&
            selectedProject.Content
                is string selectedProjectName)
        {
            return selectedProjectName;
        }

        return DemoCatalog.PrimaryProjectName;
    }

    private static string GetSafeValue(
        string? value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? fallback
            : value.Trim();
    }

    private static string GetTestTypeDisplayName(
        string? testTypeKey)
    {
        return testTypeKey?.ToUpperInvariant() switch
        {
            "REGRESSION" =>
                LocalizationService.T("Explorer.Regression"),

            "FUNCTIONAL" =>
                LocalizationService.T("Explorer.FunctionalTests"),

            null or "" =>
                LocalizationService.T("Common.NotSpecified"),

            _ =>
                testTypeKey
        };
    }

    private void BuildRoleBadges()
    {
        RefreshRoleBadges();

        RoleBadgesScrollViewer.PointerWheelChanged +=
            (_, eventArgs) =>
            {
                var current =
                    RoleBadgesScrollViewer.Offset;

                RoleBadgesScrollViewer.Offset =
                    new Vector(
                        Math.Max(
                            0,
                            current.X -
                            eventArgs.Delta.Y * 34),
                        current.Y);

                eventArgs.Handled =
                    true;
            };

        EnableRoleBadgeDragScrolling(
            RoleBadgesScrollViewer);
    }

    private void RefreshRoleBadges()
    {
        RoleBadgesPanel.Children.Clear();

        foreach (var role in
                 SystemRoleService.GetOrderedDisplayRoles(
                     _systemRoles,
                     _projectRoles))
        {
            RoleBadgesPanel.Children.Add(
                CreateRoleBadge(
                    role));
        }

    }

    private static void EnableRoleBadgeDragScrolling(
        ScrollViewer scrollViewer)
    {
        Point? dragStart =
            null;

        Vector startOffset =
            default;

        scrollViewer.PointerPressed +=
            (_, eventArgs) =>
            {
                var point =
                    eventArgs.GetCurrentPoint(
                        scrollViewer);

                if (!point.Properties.IsLeftButtonPressed)
                {
                    return;
                }

                dragStart =
                    point.Position;

                startOffset =
                    scrollViewer.Offset;

                eventArgs.Pointer.Capture(
                    scrollViewer);
            };

        scrollViewer.PointerMoved +=
            (_, eventArgs) =>
            {
                if (dragStart is null)
                {
                    return;
                }

                var current =
                    eventArgs.GetPosition(
                        scrollViewer);

                scrollViewer.Offset =
                    new Vector(
                        Math.Max(
                            0,
                            startOffset.X +
                            dragStart.Value.X -
                            current.X),
                        startOffset.Y);
            };

        scrollViewer.PointerReleased +=
            (_, eventArgs) =>
            {
                dragStart =
                    null;

                eventArgs.Pointer.Capture(
                    null);
            };
    }

    private async Task RefreshAssignmentAndNotificationStateAsync()
    {
        _activeAssignments =
            await _assignmentService.GetActiveAssignmentsForUserAsync(
                _loggedInLogin);

        ExecuteAssignedTestsButton.IsVisible =
            false;

        if (ExecuteAssignedTestsButton.IsVisible)
        {
            _assignmentGlowTimer.Start();
        }
        else
        {
            _assignmentGlowTimer.Stop();
            ExecuteAssignedTestsButton.Opacity = 1;
            ExecuteAssignedTestsButton.BorderBrush =
                new SolidColorBrush(
                    Color.Parse(
                        "#70A8DD"));
        }

        var unreadCount =
            await _assignmentService.GetUnreadCountAsync(
                _loggedInLogin);

        NotificationBadgeBorder.IsVisible =
            unreadCount > 0;

        NotificationBadgeTextBlock.Text =
            unreadCount.ToString();
    }

    private async void NotificationCenterButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog =
            new NotificationCenterWindow(
                _loggedInLogin);

        await dialog.ShowDialog(
            this);

        await RefreshAssignmentAndNotificationStateAsync();
    }

    private async void ProgressDashboardButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog =
            new ProgressDashboardWindow(
                _loggedInLogin,
                _systemRoles);

        await dialog.ShowDialog(
            this);

        await RefreshAssignmentAndNotificationStateAsync();
    }

    private async void ExecuteAssignedTestsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_activeAssignments.Length == 0)
        {
            await RefreshAssignmentAndNotificationStateAsync();
            return;
        }

        var projectName =
            _activeAssignments[0].ProjectName;

        var explorer =
            new ExplorerView(
                projectName,
                _activeAssignments[0].ApplicationVersion,
                _loggedInLogin,
                _sessionManager,
                _sessionState,
                _loggedInLogin,
                RequestLogout,
                _highestSystemRole,
                _systemRoles,
                _projectRoles);

        Content =
            explorer;

        await explorer.ExecuteLatestAssignmentAsync();
    }

    private Border CreateRoleBadge(
        string role)
    {
        var isDarkMode =
            Application.Current?.RequestedThemeVariant ==
            ThemeVariant.Dark;

        var (
            background,
            border,
            foreground) =
            (isDarkMode, role) switch
            {
                (true, "Admin") =>
                    ("#3A2427", "#75434A", "#F2AAB0"),

                (true, "Lider") =>
                    ("#3A3220", "#746638", "#E8CD79"),

                (true, _) =>
                    ("#223448", "#416887", "#A8CEF1"),

                (false, "Admin") =>
                    ("#FDEBEC", "#E9A8AC", "#B3262D"),

                (false, "Lider") =>
                    ("#FFF3D6", "#E4C36A", "#8A6200"),

                _ =>
                    ("#E8F2FF", "#A8CCF1", "#1F6FBF")
            };

        if (_projectRoleColors.TryGetValue(role, out var customBorderColor))
        {
            border = customBorderColor;
        }

        return new Border
        {
            Height =
                36,

            Padding =
                new Thickness(
                    13,
                    0),

            Background =
                new SolidColorBrush(
                    Color.Parse(
                        background)),

            BorderBrush =
                new SolidColorBrush(
                    Color.Parse(
                        border)),

            BorderThickness =
                new Thickness(1),

            CornerRadius =
                new CornerRadius(10),

            Child =
                new TextBlock
                {
                    Text =
                        role,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    FontSize =
                        13,

                    FontWeight =
                        FontWeight.SemiBold,

                    Foreground =
                        new SolidColorBrush(
                            Color.Parse(
                                foreground))
                }
        };
    }

    private void UpdateThemeButton()
    {
        if (_isDarkMode)
        {
            ThemeSunIcon.IsVisible = true;
            ThemeMoonIcon.IsVisible = false;

            ToolTip.SetTip(
                ThemeToggleButton,
                LocalizationService.T("Theme.SwitchToLight"));
        }
        else
        {
            ThemeSunIcon.IsVisible = false;
            ThemeMoonIcon.IsVisible = true;

            ToolTip.SetTip(
                ThemeToggleButton,
                LocalizationService.T("Theme.SwitchToDark"));
        }
    }

    private void PolishLanguageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LocalizationService.SaveAndApply(LocalizationService.Polish);
        LanguagePickerButton.Flyout?.Hide();
    }

    private void EnglishLanguageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LocalizationService.SaveAndApply(LocalizationService.English);
        LanguagePickerButton.Flyout?.Hide();
    }

    private void UpdateLanguagePicker()
    {
        var isEnglish = !LocalizationService.IsPolish;
        CurrentPolishFlag.IsVisible = !isEnglish;
        CurrentEnglishFlag.IsVisible = isEnglish;
    }

    private void LocalizationService_OnLanguageChanged(
        object? sender,
        EventArgs e)
    {
        LoggedInUserTextBlock.Text =
            string.Format(
                LocalizationService.T("Common.LoggedIn"),
                _loggedInLogin);
        UpdateLanguagePicker();
        UpdateThemeButton();
    }
}
