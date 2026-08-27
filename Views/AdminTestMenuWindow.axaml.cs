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
    public bool GlobalResetCompleted { get; private set; }
    private readonly UserProfileService _profileService =
        new();
    private readonly AssignmentService _assignmentService =
        new();
    private readonly JsonStorageService _storageService =
        new();

    private ComboBox? _profileComboBox;
    private StackPanel? _selectedProfileActionsPanel;
    private Button? _deleteSelectedUserButton;
    private Button? _roleAndProjectEditorButton;
    private Button? _projectManagementButton;
    private TextBlock? _resultTextBlock;
    private Border? _resetAllProfilesPanel;
    private Border? _roleManagementPanel;
    private Control? _accountManagementPanel;
    private Border? _projectManagementDetailsPanel;
    private Border? _accountManagementDetailsPanel;
    private Border? _newAccountActionPanel;
    private TextBlock? _projectManagementChevronTextBlock;
    private TextBlock? _accountManagementChevronTextBlock;
    private StackPanel? _resetAssignmentsPanel;
    private CheckBox? _administratorRoleCheckBox;
    private CheckBox? _leaderRoleCheckBox;
    private CheckBox? _testerRoleCheckBox;
    private WrapPanel? _ownedRolesPanel;
    private Border? _roleEditorPanel;
    private WrapPanel? _projectRoleCheckBoxesPanel;
    private ProjectRoleDefinitionModel[] _availableProjectRoles =
        Array.Empty<ProjectRoleDefinitionModel>();
    private TextBlock? _roleSaveResultTextBlock;
    private TextBox? _newUserLoginTextBox;
    private Grid? _newUserFormPanel;
    private Button? _showNewUserFormButton;
    private TextBox? _globalResetPinTextBox;
    private ComboBox? _quickProjectComboBox;
    private Button? _showQuickProjectCreationButton;
    private Grid? _quickProjectCreationPanel;
    private TextBox? _quickProjectNameTextBox;
    private StackPanel? _quickProjectActionsPanel;
    private StackPanel? _quickProjectRolesPanel;
    private StackPanel? _quickProjectSetupPromptPanel;
    private TextBlock? _quickProjectResultTextBlock;
    private ProjectDefinitionModel[] _quickProjects =
        Array.Empty<ProjectDefinitionModel>();
    private ProjectRoleDefinitionModel[] _quickRoles =
        Array.Empty<ProjectRoleDefinitionModel>();
    private UserProfileModel[] _profiles =
        Array.Empty<UserProfileModel>();
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

        _roleAndProjectEditorButton =
            this.FindControl<Button>(
                "RoleAndProjectEditorButton");

        _projectManagementButton =
            this.FindControl<Button>(
                "ProjectManagementButton");

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
            this.FindControl<Control>(
                "AccountManagementPanel");

        _projectManagementDetailsPanel =
            this.FindControl<Border>(
                "ProjectManagementDetailsPanel");

        _accountManagementDetailsPanel =
            this.FindControl<Border>(
                "AccountManagementDetailsPanel");

        _newAccountActionPanel =
            this.FindControl<Border>(
                "NewAccountActionPanel");

        _projectManagementChevronTextBlock =
            this.FindControl<TextBlock>(
                "ProjectManagementChevronTextBlock");

        _accountManagementChevronTextBlock =
            this.FindControl<TextBlock>(
                "AccountManagementChevronTextBlock");

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

        _ownedRolesPanel =
            this.FindControl<WrapPanel>(
                "OwnedRolesPanel");

        _roleEditorPanel =
            this.FindControl<Border>(
                "RoleEditorPanel");

        _projectRoleCheckBoxesPanel =
            this.FindControl<WrapPanel>(
                "ProjectRoleCheckBoxesPanel");

        _roleSaveResultTextBlock =
            this.FindControl<TextBlock>(
                "RoleSaveResultTextBlock");

        _newUserLoginTextBox =
            this.FindControl<TextBox>(
                "NewUserLoginTextBox");

        _newUserFormPanel =
            this.FindControl<Grid>(
                "NewUserFormPanel");

        _showNewUserFormButton =
            this.FindControl<Button>(
                "ShowNewUserFormButton");

        _globalResetPinTextBox =
            this.FindControl<TextBox>(
                "GlobalResetPinTextBox");

        _quickProjectComboBox =
            this.FindControl<ComboBox>(
                "QuickProjectComboBox");

        _showQuickProjectCreationButton =
            this.FindControl<Button>(
                "ShowQuickProjectCreationButton");

        _quickProjectCreationPanel =
            this.FindControl<Grid>(
                "QuickProjectCreationPanel");

        _quickProjectNameTextBox =
            this.FindControl<TextBox>(
                "QuickProjectNameTextBox");

        _quickProjectActionsPanel =
            this.FindControl<StackPanel>(
                "QuickProjectActionsPanel");

        _quickProjectRolesPanel =
            this.FindControl<StackPanel>(
                "QuickProjectRolesPanel");

        _quickProjectSetupPromptPanel =
            this.FindControl<StackPanel>(
                "QuickProjectSetupPromptPanel");

        _quickProjectResultTextBlock =
            this.FindControl<TextBlock>(
                "QuickProjectResultTextBlock");

        if (_resetAllProfilesPanel is not null)
        {
            _resetAllProfilesPanel.IsVisible =
                false;
        }

        if (_roleManagementPanel is not null)
        {
            _roleManagementPanel.IsVisible =
                _canManageRoles;
        }

        if (_roleAndProjectEditorButton is not null)
        {
            _roleAndProjectEditorButton.IsVisible =
                _canManageRoles;
        }

        if (_projectManagementButton is not null)
        {
            _projectManagementButton.IsVisible =
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
                false;
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
                await LoadQuickProjectsAsync();
            };
    }

    private async Task LoadQuickProjectsAsync(
        string? selectedProjectKey = null)
    {
        if (_quickProjectComboBox is null)
        {
            return;
        }

        var definitions =
            await _profileService.GetRoleAndProjectDefinitionsAsync();

        _quickProjects =
            definitions.Projects;

        _quickRoles =
            definitions.Roles;

        _quickProjectComboBox.Items.Clear();
        _quickProjectComboBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Wybierz projekt"
            });

        var selectedIndex = 0;
        var index = 1;

        foreach (var project in _quickProjects
                     .OrderBy(
                         item =>
                             DemoCatalog.IsTestProject(item.Name)
                                 ? 1
                                 : 0)
                     .ThenBy(
                         item => item.Name,
                         StringComparer.OrdinalIgnoreCase))
        {
            _quickProjectComboBox.Items.Add(
                new ComboBoxItem
                {
                    Content = project.Name,
                    Tag = project
                });

            if (string.Equals(
                    project.Key,
                    selectedProjectKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = index;
            }

            index++;
        }

        _quickProjectComboBox.SelectedIndex =
            selectedIndex;

        UpdateQuickProjectActions();
    }

    private void QuickProjectComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateQuickProjectActions();
        HideQuickProjectSetupPrompt();
    }

    private void UpdateQuickProjectActions()
    {
        var project =
            GetSelectedQuickProject();

        if (_quickProjectActionsPanel is not null)
        {
            _quickProjectActionsPanel.IsVisible =
                project is not null;
        }

        PopulateQuickProjectRoles(
            project);
    }

    private void PopulateQuickProjectRoles(
        ProjectDefinitionModel? project)
    {
        if (_quickProjectRolesPanel is null)
        {
            return;
        }

        _quickProjectRolesPanel.Children.Clear();

        if (project is null)
        {
            return;
        }

        var roles =
            _quickRoles
                .Where(role => role.ProjectKeys.Contains(
                    project.Key,
                    StringComparer.OrdinalIgnoreCase))
                .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (roles.Length == 0)
        {
            _quickProjectRolesPanel.Children.Add(
                new TextBlock
                {
                    Text = "Tu na razie nie ma żadnych ról. Naciśnij „Dodaj rolę”, aby utworzyć pierwszą rolę dla projektu.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Avalonia.Thickness(4)
                });

            return;
        }

        foreach (var role in roles)
        {
            var editButton =
                new Button
                {
                    Content = "✎  EDYTUJ",
                    Height = 32,
                    Padding = new Avalonia.Thickness(11, 0),
                    Tag = role
                };

            editButton.Click +=
                async (_, _) =>
                    await OpenProjectRoleEditorAsync(
                        project,
                        role.Id,
                        false);

            var deleteButton =
                new Button
                {
                    Content = "×",
                    Width = 32,
                    Height = 32,
                    Padding = new Avalonia.Thickness(0),
                    Background = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#DC4C56")),
                    Foreground = Avalonia.Media.Brushes.White,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Tag = role
                };

            var actionPanel =
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 5,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

            void ShowDefaultActions()
            {
                actionPanel.Children.Clear();
                actionPanel.Children.Add(editButton);
                actionPanel.Children.Add(deleteButton);
            }

            deleteButton.Click +=
                (_, _) =>
                {
                    actionPanel.Children.Clear();
                    actionPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "Usunąć?",
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            FontSize = 12
                        });

                    var confirmButton =
                        new Button
                        {
                            Content = "TAK",
                            Height = 30,
                            Padding = new Avalonia.Thickness(9, 0),
                            Background = new Avalonia.Media.SolidColorBrush(
                                Avalonia.Media.Color.Parse("#DC4C56")),
                            Foreground = Avalonia.Media.Brushes.White
                        };
                    confirmButton.Click +=
                        async (_, _) =>
                            await DeleteQuickProjectRoleAsync(
                                project,
                                role);

                    var cancelButton =
                        new Button
                        {
                            Content = "NIE",
                            Height = 30,
                            Padding = new Avalonia.Thickness(9, 0)
                        };
                    cancelButton.Click += (_, _) => ShowDefaultActions();

                    actionPanel.Children.Add(confirmButton);
                    actionPanel.Children.Add(cancelButton);
                };

            ShowDefaultActions();

            var row =
                new Grid
                {
                    ColumnDefinitions =
                        new ColumnDefinitions("Auto,10,*,10,Auto")
                };

            var rolePreview =
                new Border
                {
                    MinWidth = 118,
                    Height = 30,
                    Padding = new Avalonia.Thickness(11, 0),
                    CornerRadius = new Avalonia.CornerRadius(9),
                    BorderThickness = new Avalonia.Thickness(1),
                    BorderBrush = CreateRoleBrush(
                        role.BorderColor,
                        "#46545E"),
                    Background = CreateRoleBrush(
                        role.BackgroundColor,
                        "#64727C"),
                    Child = new TextBlock
                    {
                        Text = role.Name,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Foreground = CreateRoleBrush(
                            role.TextColor,
                            "#F0F4F6")
                    }
                };

            row.Children.Add(rolePreview);

            var membersPanel =
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 6,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

            var members = _profiles
                .Where(profile => profile.ProjectRoles.Contains(
                    role.Name,
                    StringComparer.OrdinalIgnoreCase))
                .OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(profile => profile.Login, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (members.Length == 0)
            {
                membersPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "Brak przypisanych osób",
                        Foreground = Avalonia.Media.Brushes.Gray,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    });
            }
            else
            {
                foreach (var member in members)
                {
                    var displayName = string.IsNullOrWhiteSpace(member.DisplayName)
                        ? member.Login
                        : member.DisplayName.Trim();

                    membersPanel.Children.Add(
                        new Border
                        {
                            Padding = new Avalonia.Thickness(9, 4),
                            CornerRadius = new Avalonia.CornerRadius(8),
                            Background = this.FindResource("InputBackgroundBrush") as Avalonia.Media.IBrush,
                            BorderBrush = this.FindResource("InputBorderBrush") as Avalonia.Media.IBrush,
                            BorderThickness = new Avalonia.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = $"{displayName} ({member.Login})",
                                FontSize = 12,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                            }
                        });
                }
            }

            var membersScrollViewer =
                new ScrollViewer
                {
                    Content = membersPanel,
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

            Grid.SetColumn(membersScrollViewer, 2);
            row.Children.Add(membersScrollViewer);

            Grid.SetColumn(actionPanel, 4);
            row.Children.Add(actionPanel);

            _quickProjectRolesPanel.Children.Add(
                row);
        }
    }

    private async Task DeleteQuickProjectRoleAsync(
        ProjectDefinitionModel project,
        ProjectRoleDefinitionModel role)
    {
        _quickRoles = _quickRoles
            .Where(item => item.Id != role.Id)
            .ToArray();

        await _profileService.SaveRoleAndProjectDefinitionsAsync(
            _quickProjects,
            _quickRoles);
        await LoadProfilesAsync();
        await LoadQuickProjectsAsync(project.Key);

        ShowQuickProjectResult(
            $"Rola „{role.Name}” została usunięta.",
            true);
    }

    private static Avalonia.Media.IBrush CreateRoleBrush(
        string? colorValue,
        string fallback)
    {
        var value = string.IsNullOrWhiteSpace(colorValue)
            ? fallback
            : colorValue;

        return Avalonia.Media.Color.TryParse(value, out var color)
            ? new Avalonia.Media.SolidColorBrush(color)
            : new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse(fallback));
    }

    private async void AddRoleToQuickProjectButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var project =
            GetSelectedQuickProject();

        if (project is not null)
        {
            await OpenProjectRoleEditorAsync(
                project,
                null,
                true);
        }
    }

    private async Task OpenProjectRoleEditorAsync(
        ProjectDefinitionModel project,
        Guid? roleId,
        bool beginWithNewRole)
    {
        var dialog =
            new RoleManagementWindow(
                _changedByLogin,
                project.Key,
                roleId,
                beginWithNewRole);

        await dialog.ShowDialog(this);
        await LoadProfilesAsync();
        await LoadQuickProjectsAsync(project.Key);
    }

    private void ToggleProjectManagementButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var show =
            _projectManagementDetailsPanel?.IsVisible != true;

        if (_projectManagementDetailsPanel is not null)
        {
            _projectManagementDetailsPanel.IsVisible =
                show;
        }

        if (_accountManagementDetailsPanel is not null)
        {
            _accountManagementDetailsPanel.IsVisible =
                false;
        }

        if (_newAccountActionPanel is not null)
        {
            _newAccountActionPanel.IsVisible =
                false;
        }

        if (_projectManagementChevronTextBlock is not null)
        {
            _projectManagementChevronTextBlock.Text =
                show ? "⌃" : "⌄";
        }

        if (_accountManagementChevronTextBlock is not null)
        {
            _accountManagementChevronTextBlock.Text =
                "⌄";
        }
    }

    private void ToggleAccountManagementButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var show =
            _accountManagementDetailsPanel?.IsVisible != true;

        if (_accountManagementDetailsPanel is not null)
        {
            _accountManagementDetailsPanel.IsVisible =
                show;
        }

        if (_newAccountActionPanel is not null)
        {
            _newAccountActionPanel.IsVisible =
                show;
        }

        if (_projectManagementDetailsPanel is not null)
        {
            _projectManagementDetailsPanel.IsVisible =
                false;
        }

        if (_accountManagementChevronTextBlock is not null)
        {
            _accountManagementChevronTextBlock.Text =
                show ? "⌃" : "⌄";
        }

        if (_projectManagementChevronTextBlock is not null)
        {
            _projectManagementChevronTextBlock.Text =
                "⌄";
        }
    }

    private ProjectDefinitionModel? GetSelectedQuickProject()
    {
        return (_quickProjectComboBox?.SelectedItem as ComboBoxItem)?.Tag
            as ProjectDefinitionModel;
    }

    private void ShowQuickProjectCreationButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_showQuickProjectCreationButton is not null)
        {
            _showQuickProjectCreationButton.IsVisible =
                false;
        }

        if (_quickProjectCreationPanel is not null)
        {
            _quickProjectCreationPanel.IsVisible =
                true;
        }

        HideQuickProjectSetupPrompt();
        _quickProjectNameTextBox?.Focus();
    }

    private void CancelQuickProjectCreationButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_quickProjectNameTextBox is not null)
        {
            _quickProjectNameTextBox.Text =
                string.Empty;
        }

        if (_quickProjectCreationPanel is not null)
        {
            _quickProjectCreationPanel.IsVisible =
                false;
        }

        if (_showQuickProjectCreationButton is not null)
        {
            _showQuickProjectCreationButton.IsVisible =
                true;
        }
    }

    private async void CreateQuickProjectButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var name =
            _quickProjectNameTextBox?.Text?.Trim() ??
            string.Empty;

        if (name.Length < 2)
        {
            ShowQuickProjectResult(
                "Podaj nazwę projektu.",
                false);

            return;
        }

        if (_quickProjects.Any(
                project => string.Equals(
                    project.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase)))
        {
            ShowQuickProjectResult(
                "Projekt o tej nazwie już istnieje.",
                false);

            return;
        }

        var keyBase =
            new string(
                    name.ToLowerInvariant()
                        .Select(
                            character =>
                                char.IsLetterOrDigit(character)
                                    ? character
                                    : '-')
                        .ToArray())
                .Trim('-');

        if (string.IsNullOrWhiteSpace(keyBase))
        {
            keyBase =
                "projekt";
        }

        var key =
            keyBase;

        var suffix =
            2;

        while (_quickProjects.Any(
                   project => string.Equals(
                       project.Key,
                       key,
                       StringComparison.OrdinalIgnoreCase)))
        {
            key =
                $"{keyBase}-{suffix++}";
        }

        var project =
            new ProjectDefinitionModel
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name
            };

        await _profileService.SaveRoleAndProjectDefinitionsAsync(
            _quickProjects.Append(project),
            _quickRoles);

        CancelQuickProjectCreationButton_OnClick(
            null,
            new RoutedEventArgs());

        await LoadQuickProjectsAsync(
            project.Key);

        ShowQuickProjectResult(
            $"✓ Utworzono projekt „{project.Name}”.",
            true);

        if (_quickProjectSetupPromptPanel is not null)
        {
            _quickProjectSetupPromptPanel.IsVisible =
                true;
        }
    }

    private async void DeleteQuickProjectButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var project =
            GetSelectedQuickProject();

        if (project is null)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Usunąć projekt?",
                $"Projekt „{project.Name}” zostanie usunięty i odłączony od wszystkich ról. Potwierdź operację PIN-em konta admin.",
                "USUŃ PROJEKT");

        confirmation.RequirePin();

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        var authentication =
            await _profileService.AuthenticateAsync(
                "admin",
                confirmation.EnteredPin);

        if (authentication.Status != AuthenticationStatus.Success)
        {
            ShowQuickProjectResult(
                "Nieprawidłowy PIN administratora. Projekt nie został usunięty.",
                false);
            return;
        }

        await _profileService.SaveRoleAndProjectDefinitionsAsync(
            _quickProjects.Where(
                item => item.Id != project.Id),
            _quickRoles);

        await LoadQuickProjectsAsync();

        ShowQuickProjectResult(
            $"Projekt „{project.Name}” został usunięty.",
            true);
    }

    private async void ConfigureQuickProjectRolesButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var project =
            GetSelectedQuickProject();

        if (project is null)
        {
            return;
        }

        HideQuickProjectSetupPrompt();

        var dialog =
            new RoleManagementWindow(
                _changedByLogin,
                project.Key);

        await dialog.ShowDialog(this);
        await LoadQuickProjectsAsync(project.Key);
    }

    private void DismissQuickProjectSetupButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        HideQuickProjectSetupPrompt();
    }

    private void HideQuickProjectSetupPrompt()
    {
        if (_quickProjectSetupPromptPanel is not null)
        {
            _quickProjectSetupPromptPanel.IsVisible =
                false;
        }
    }

    private void ShowQuickProjectResult(
        string message,
        bool success)
    {
        if (_quickProjectResultTextBlock is null)
        {
            return;
        }

        _quickProjectResultTextBlock.Text =
            message;

        _quickProjectResultTextBlock.Foreground =
            success
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;

        _quickProjectResultTextBlock.IsVisible =
            true;
    }

    private async Task LoadProfilesAsync()
    {
        if (_profileComboBox is null)
        {
            return;
        }

        _profiles =
            (await _profileService.GetProfilesAsync()).ToArray();

        var roleAndProjectDefinitions =
            await _profileService.GetRoleAndProjectDefinitionsAsync();
        _availableProjectRoles =
            roleAndProjectDefinitions.Roles;

        _profileComboBox.Items.Clear();

        _profileComboBox.Items.Add(
            new ComboBoxItem
            {
                Content =
                    "Brak — nie wybrano profilu"
            });

        foreach (var profile in _profiles
                     .OrderBy(profile => GetSystemRoleOrder(profile.SystemRoles))
                     .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(profile => profile.Login, StringComparer.OrdinalIgnoreCase))
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

        return $"{profile.DisplayName} ({profile.Login}) — {role}";
    }

    private static int GetSystemRoleOrder(
        System.Collections.Generic.IEnumerable<string>? roles)
    {
        var highestRole = SystemRoleService.GetHighestRole(roles);

        if (string.Equals(
                highestRole,
                SystemRoleService.AdministratorRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(
                highestRole,
                SystemRoleService.LeaderRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
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

        if (_roleEditorPanel is not null)
        {
            _roleEditorPanel.IsVisible =
                false;
        }

        if (_deleteSelectedUserButton is not null)
        {
            var isCurrentProfile =
                string.Equals(
                    profile.Login,
                    _changedByLogin,
                    StringComparison.OrdinalIgnoreCase);

            var isProtectedProfile =
                IsProtectedProfile(
                    profile.Login);

            _deleteSelectedUserButton.IsEnabled =
                !isCurrentProfile &&
                !isProtectedProfile;

            ToolTip.SetTip(
                _deleteSelectedUserButton,
                isCurrentProfile
                    ? "Nie można usunąć aktualnie zalogowanego konta."
                    : isProtectedProfile
                        ? "Konto systemowe jest chronione i nie może zostać usunięte."
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

        if (_projectRoleCheckBoxesPanel is not null)
        {
            _projectRoleCheckBoxesPanel.Children.Clear();

            foreach (var projectRole in _availableProjectRoles)
            {
                _projectRoleCheckBoxesPanel.Children.Add(
                    new CheckBox
                    {
                        Content = projectRole.Name,
                        Tag = projectRole.Name,
                        Margin = new Avalonia.Thickness(0, 0, 18, 8),
                        IsChecked = profile.ProjectRoles.Contains(
                            projectRole.Name,
                            StringComparer.OrdinalIgnoreCase)
                    });
            }
        }

        PopulateOwnedRoles(profile);
    }

    private void PopulateOwnedRoles(
        UserProfileModel profile)
    {
        if (_ownedRolesPanel is null)
        {
            return;
        }

        _ownedRolesPanel.Children.Clear();

        var roles =
            SystemRoleService.GetOrderedDisplayRoles(
                    profile.SystemRoles,
                    profile.ProjectRoles)
                .ToArray();

        foreach (var role in roles)
        {
            _ownedRolesPanel.Children.Add(
                new Border
                {
                    Margin = new Avalonia.Thickness(0, 0, 7, 7),
                    Padding = new Avalonia.Thickness(10, 5),
                    CornerRadius = new Avalonia.CornerRadius(9),
                    Background = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#183885B8")),
                    BorderBrush = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#7154B8")),
                    BorderThickness = new Avalonia.Thickness(1),
                    Child = new TextBlock
                    {
                        Text = role,
                        FontSize = 12,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    }
                });
        }
    }

    private void ShowRoleEditorButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_roleEditorPanel is not null)
        {
            _roleEditorPanel.IsVisible =
                true;
        }
    }

    private void CancelRoleEditorButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateRoleSelection();
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
            _projectRoleCheckBoxesPanel?.Children
                .OfType<CheckBox>()
                .Where(checkBox => checkBox.IsChecked == true)
                .Select(checkBox => checkBox.Tag?.ToString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Cast<string>()
                .ToList()
            ?? new System.Collections.Generic.List<string>();

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

            profile.SystemRoles = systemRoles.ToList();
            profile.ProjectRoles = projectRoles;
            PopulateOwnedRoles(profile);

            if (_roleEditorPanel is not null)
            {
                _roleEditorPanel.IsVisible = false;
            }

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

            SetNewUserFormVisible(false);

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

    private void ShowNewUserFormButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        SetNewUserFormVisible(true);
        _newUserLoginTextBox?.Focus();
    }

    private void CancelNewUserButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_newUserLoginTextBox is not null)
        {
            _newUserLoginTextBox.Text = string.Empty;
        }

        SetNewUserFormVisible(false);
    }

    private void SetNewUserFormVisible(bool isVisible)
    {
        if (_newUserFormPanel is not null)
        {
            _newUserFormPanel.IsVisible = isVisible;
        }

        if (_showNewUserFormButton is not null)
        {
            _showNewUserFormButton.IsEnabled = !isVisible;
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

        if (IsProtectedProfile(profile.Login))
        {
            ShowResult(
                "Konta admin i epotocki są chronione i nie mogą zostać usunięte.",
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

    private static bool IsProtectedProfile(string? login)
    {
        return string.Equals(
                   login,
                   "admin",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   login,
                   "epotocki",
                   StringComparison.OrdinalIgnoreCase);
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
            "To operacja nieodwracalna. Wszystkie PIN-y zostaną ustawione na 000000, a przypisania, powiadomienia, sesje, statusy, komentarze i zapisane wartości formularzy zostaną wyzerowane. Język wróci do angielskiego, a wygląd do ustawień domyślnych. Konta, role i struktura przypadków pozostaną.",
            "RESETUJ ŚRODOWISKO");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        var busyWindow = new BusyOperationWindow(
            "Resetowanie danych testowych",
            "Przywracanie kont, wyglądu, przypisań, sesji i statusów. Po zakończeniu nastąpi wylogowanie.",
            async () =>
            {
                await _assignmentService.ResetAllAssignmentDataAsync();
                var testData = await _storageService.LoadAsync();
                foreach (var testCase in testData.TestCases)
                {
                    testCase.Status = "None";
                    testCase.Comment = string.Empty;
                }

                await _storageService.SaveAsync(testData);
                await _profileService.ResetAllProfilesForTestAsync();
                SessionManager.DeleteAllLocalSessions();
                AssignmentInputPresetService.Reset();
                LocalizationService.ResetToDefault();
                ApplicationAppearanceService.ResetAllProfilesToTestDefaults();
            });

        await busyWindow.ShowDialog(this);

        if (busyWindow.OperationException is Exception exception)
        {
            ShowResult($"Globalny reset nie powiódł się: {exception.Message}", false);
            await ShowOperationResultAsync(
                false,
                "Globalny reset nie powiódł się",
                "Nie udało się przywrócić środowiska testowego. Żadne dalsze operacje nie zostały wykonane.");
            return;
        }

        _globalResetPinTextBox.Text = string.Empty;
        GlobalResetCompleted = true;
        Close();
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

    private async void OpenResetSettingsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canResetAllProfiles)
        {
            return;
        }

        var dialog = new ResetSettingsWindow(_changedByLogin);
        await dialog.ShowDialog(this);
        if (dialog.GlobalResetCompleted)
        {
            GlobalResetCompleted = true;
            Close();
        }
    }

    private async void OpenProjectManagementButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_canManageRoles)
        {
            return;
        }

        var selectedProjectKey =
            GetSelectedQuickProject()?.Key;

        await new ProjectManagementWindow().ShowDialog(this);
        await LoadQuickProjectsAsync(selectedProjectKey);
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
        await LoadQuickProjectsAsync(
            GetSelectedQuickProject()?.Key);
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
