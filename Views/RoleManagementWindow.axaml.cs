using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class RoleManagementWindow : Window
{
    private sealed record RolePaletteOption(
        string Name,
        string Border,
        string Background,
        string Text);

    private static readonly RolePaletteOption[] RolePalettes =
    {
        new("Szmaragdowy", "#164A34", "#2F7657", "#E0F7EC"),
        new("Zielony", "#31501F", "#587A39", "#F0F7DF"),
        new("Oliwkowy", "#4E4B18", "#777331", "#F7F3D4"),
        new("Limonkowy", "#42631C", "#6D963A", "#F1F9DC"),
        new("Miętowy", "#1D574A", "#3A8975", "#DDF8F0"),
        new("Turkusowy", "#164E52", "#2C7B80", "#DDF7F7"),
        new("Morski", "#174458", "#2E6B82", "#DCEFF7"),
        new("Cyjan", "#1B5064", "#397E99", "#E0F4FA"),
        new("Błękitny", "#294F70", "#4A7EA5", "#E4F2FC"),
        new("Chabrowy", "#283F78", "#4864A5", "#E6ECFF"),
        new("Granatowy", "#222F63", "#3D4D88", "#E6E9FA"),
        new("Indygo", "#332867", "#55458F", "#ECE7FB"),
        new("Fioletowy", "#452969", "#704897", "#F0E6FA"),
        new("Śliwkowy", "#572B59", "#854D87", "#F6E5F5"),
        new("Jagodowy", "#522B4D", "#7D4B75", "#F5E5F1"),
        new("Różowy", "#6E3454", "#A4577E", "#FBE7F1"),
        new("Koralowy", "#743D36", "#AD655B", "#FBE9E6"),
        new("Pomarańczowy", "#70441F", "#A86B35", "#FAEBDD"),
        new("Bursztynowy", "#664E1D", "#947532", "#F9F1D7"),
        new("Brązowy", "#49372B", "#705747", "#F1E8E1"),
        new("Piaskowy", "#5B503B", "#82745B", "#F5F0E5"),
        new("Stalowy", "#354755", "#566C7B", "#E7EEF2"),
        new("Grafitowy", "#2E363C", "#4B555D", "#E8EDF0"),
        new("Srebrny", "#4A5055", "#717980", "#F0F3F5")
    };

    private readonly UserProfileService _profileService = new();
    private readonly AssignmentService _assignmentService = new();
    private readonly string _changedByLogin;
    private UserProfileModel[] _profiles = Array.Empty<UserProfileModel>();
    private ProjectDefinitionModel[] _projects = Array.Empty<ProjectDefinitionModel>();
    private List<ProjectRoleDefinitionModel> _roles = new();
    private Guid? _editedRoleId;
    private bool _updatingMemberSelection;
    private bool _updatingPalette;
    private string? _selectedRoleAssignmentProjectKey;
    private readonly Guid? _initialRoleId;
    private readonly bool _beginWithNewRole;

    public RoleManagementWindow() : this("Administrator")
    {
    }

    public RoleManagementWindow(string changedByLogin)
        : this(changedByLogin, null)
    {
    }

    public RoleManagementWindow(
        string changedByLogin,
        string? selectedProjectKey)
        : this(changedByLogin, selectedProjectKey, null, false)
    {
    }

    public RoleManagementWindow(
        string changedByLogin,
        string? selectedProjectKey,
        Guid? initialRoleId,
        bool beginWithNewRole)
    {
        _changedByLogin = string.IsNullOrWhiteSpace(changedByLogin)
            ? "Administrator"
            : changedByLogin.Trim();

        _selectedRoleAssignmentProjectKey =
            string.IsNullOrWhiteSpace(selectedProjectKey)
                ? null
                : selectedProjectKey.Trim();

        _initialRoleId = initialRoleId;
        _beginWithNewRole = beginWithNewRole;

        InitializeComponent();
        PopulateRolePalettePicker();
        SelectAllRoleMembersCheckBox.IsCheckedChanged +=
            SelectAllRoleMembersCheckBox_OnChanged;
        UpdateRolePreview();
        Opened += async (_, _) =>
            await LoadAllAsync(null, _initialRoleId);
    }

    private async Task LoadAllAsync(Guid? selectedProfileId = null, Guid? selectedRoleId = null)
    {
        _profiles = (await _profileService.GetProfilesAsync()).ToArray();
        var definitions = await _profileService.GetRoleAndProjectDefinitionsAsync();
        _projects = definitions.Projects;
        _roles = definitions.Roles.ToList();

        UserRoleProjectComboBox.Items.Clear();
        UserRoleProjectComboBox.Items.Add(new ComboBoxItem
        {
            Content = "Brak — nie wybrano projektu"
        });
        foreach (var project in _projects)
        {
            UserRoleProjectComboBox.Items.Add(new ComboBoxItem
            {
                Content = project.Name,
                Tag = project
            });
        }

        var selectedProjectIndex = FindIndex<ProjectDefinitionModel>(
            UserRoleProjectComboBox,
            project => string.Equals(
                project.Key,
                _selectedRoleAssignmentProjectKey,
                StringComparison.OrdinalIgnoreCase));
        UserRoleProjectComboBox.SelectedIndex =
            string.IsNullOrWhiteSpace(_selectedRoleAssignmentProjectKey)
                ? 0
                : selectedProjectIndex;

        ProfileComboBox.Items.Clear();
        foreach (var profile in _profiles)
        {
            ProfileComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"{profile.DisplayName} ({profile.Login})",
                Tag = profile
            });
        }

        ProfileComboBox.SelectedIndex = FindIndex<UserProfileModel>(
            ProfileComboBox,
            profile => selectedProfileId.HasValue && profile.Id == selectedProfileId.Value);

        RoleDefinitionComboBox.Items.Clear();
        RoleDefinitionComboBox.Items.Add(new ComboBoxItem
        {
            Content = "Brak — nie wybrano roli"
        });
        foreach (var role in _roles)
        {
            RoleDefinitionComboBox.Items.Add(new ComboBoxItem { Content = role.Name, Tag = role });
        }

        var roleIndex = selectedRoleId.HasValue
            ? FindIndex<ProjectRoleDefinitionModel>(
                RoleDefinitionComboBox,
                role => role.Id == selectedRoleId.Value)
            : -1;
        RoleDefinitionComboBox.SelectedIndex = roleIndex;

        if (_beginWithNewRole)
        {
            BeginNewRole();
        }
        else if (_roles.Count == 0)
        {
            BeginNewRole();
        }
        else if (roleIndex < 0)
        {
            RoleDefinitionComboBox.SelectedIndex = 0;
            SetRoleEditorVisible(false);
        }
    }

    private static int FindIndex<T>(ComboBox comboBox, Func<T, bool> predicate)
    {
        var matches = comboBox.Items
            .OfType<ComboBoxItem>()
            .Select((item, index) => (item, index))
            .Where(pair => pair.item.Tag is T value && predicate(value))
            .Select(pair => pair.index)
            .ToArray();

        return matches.Length > 0 ? matches[0] : comboBox.ItemCount > 0 ? 0 : -1;
    }

    private void ProfileComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not ComboBoxItem { Tag: UserProfileModel profile })
        {
            return;
        }

        AdministratorRoleCheckBox.IsChecked = profile.SystemRoles.Contains(
            SystemRoleService.AdministratorRole,
            StringComparer.OrdinalIgnoreCase);
        var dedicatedAdmin = string.Equals(profile.Login, "admin", StringComparison.OrdinalIgnoreCase);
        AdministratorRoleCheckBox.IsEnabled = !dedicatedAdmin;
        if (dedicatedAdmin)
        {
            AdministratorRoleCheckBox.IsChecked = true;
        }

        LeaderRoleCheckBox.IsChecked = profile.SystemRoles.Contains(
            SystemRoleService.LeaderRole,
            StringComparer.OrdinalIgnoreCase);
        TesterRoleCheckBox.IsChecked = profile.SystemRoles.Contains(
            SystemRoleService.TesterRole,
            StringComparer.OrdinalIgnoreCase);

        PopulateUserProjectRoles();
    }

    private void UserRoleProjectComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        _selectedRoleAssignmentProjectKey =
            UserRoleProjectComboBox.SelectedItem is ComboBoxItem
                { Tag: ProjectDefinitionModel project }
                ? project.Key
                : null;

        PopulateUserProjectRoles();
    }

    private void PopulateUserProjectRoles()
    {
        UserProjectRolesPanel.Children.Clear();

        if (ProfileComboBox.SelectedItem is not ComboBoxItem
                { Tag: UserProfileModel profile } ||
            string.IsNullOrWhiteSpace(_selectedRoleAssignmentProjectKey))
        {
            UserProjectRoleSelectionPanel.IsVisible = false;
            return;
        }

        var rolesForProject = _roles
            .Where(role => role.ProjectKeys.Contains(
                _selectedRoleAssignmentProjectKey,
                StringComparer.OrdinalIgnoreCase))
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var role in rolesForProject)
        {
            UserProjectRolesPanel.Children.Add(new CheckBox
            {
                Content = role.Name,
                Tag = role.Name,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = profile.ProjectRoles.Contains(
                    role.Name,
                    StringComparer.OrdinalIgnoreCase)
            });
        }

        if (rolesForProject.Length == 0)
        {
            UserProjectRolesPanel.Children.Add(new TextBlock
            {
                Text = "Brak ról przypisanych do tego projektu.",
                Foreground = Brushes.Gray
            });
        }

        UserProjectRoleSelectionPanel.IsVisible = true;
    }

    private async void SaveUserRolesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not ComboBoxItem { Tag: UserProfileModel profile })
        {
            return;
        }

        var systemRoles = new[]
        {
            (AdministratorRoleCheckBox.IsChecked == true, SystemRoleService.AdministratorRole),
            (LeaderRoleCheckBox.IsChecked == true, SystemRoleService.LeaderRole),
            (TesterRoleCheckBox.IsChecked == true, SystemRoleService.TesterRole)
        }.Where(value => value.Item1).Select(value => value.Item2).ToArray();

        var selectedProjectRoleNames = UserProjectRolesPanel.Children
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Tag?.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        var rolesManagedForSelectedProject = string.IsNullOrWhiteSpace(
                _selectedRoleAssignmentProjectKey)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : _roles
                .Where(role => role.ProjectKeys.Contains(
                    _selectedRoleAssignmentProjectKey,
                    StringComparer.OrdinalIgnoreCase))
                .Select(role => role.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var projectRoles = profile.ProjectRoles
            .Where(role => !rolesManagedForSelectedProject.Contains(role))
            .Concat(selectedProjectRoleNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            await _profileService.UpdateRolesAsync(profile.Id, systemRoles, projectRoles);
            await _assignmentService.SendUserNotificationAsync(
                profile.Login,
                "Zmieniono Twoje role",
                $"{_changedByLogin} zmienił Twoje role i dostęp do projektów.");
            ShowResult($"Role użytkownika {profile.Login} zostały zapisane.", true);
            ShowInlineSaveResult(UserRolesSaveResultTextBlock, "✓ Zapisano", true);
            await LoadAllAsync(profile.Id, _editedRoleId);
        }
        catch (Exception exception)
        {
            ShowResult(exception.Message, false);
            ShowInlineSaveResult(UserRolesSaveResultTextBlock, "Nie udało się zapisać", false);
        }
    }

    private void RoleDefinitionComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RoleDefinitionComboBox.SelectedItem is ComboBoxItem { Tag: ProjectRoleDefinitionModel role })
        {
            EditRole(role);
            return;
        }

        SetRoleEditorVisible(false);
        _editedRoleId = null;
    }

    private void NewRoleButton_OnClick(object? sender, RoutedEventArgs e) => BeginNewRole();

    private void BeginNewRole()
    {
        SetRoleEditorVisible(true);
        _editedRoleId = null;
        RoleDefinitionComboBox.SelectedIndex = -1;
        RoleNameTextBox.Text = string.Empty;
        RolePalettePicker.SelectedIndex = 1;
        BuildRoleAccessEditors(null);
        UpdateRolePreview();
        RoleNameTextBox.Focus();
    }

    private void EditRole(ProjectRoleDefinitionModel role)
    {
        SetRoleEditorVisible(true);
        _editedRoleId = role.Id;
        RoleNameTextBox.Text = role.Name;
        RoleColorTextBox.Text = role.BorderColor;
        RoleBackgroundColorTextBox.Text = string.IsNullOrWhiteSpace(role.BackgroundColor)
            ? "#332A47"
            : role.BackgroundColor;
        RoleTextColorTextBox.Text = string.IsNullOrWhiteSpace(role.TextColor)
            ? "#E9DDFF"
            : role.TextColor;
        SelectMatchingRolePalette(role);
        BuildRoleAccessEditors(role);
        UpdateRolePreview();
    }

    private void SetRoleEditorVisible(bool isVisible)
    {
        RoleEditorPanel.IsVisible = isVisible;

        // Podczas tworzenia lub edycji konkretnej roli pokazujemy tylko
        // jej formularz. Ogólny edytor uprawnień konta nie dotyczy wtedy
        // wybranej roli i nie powinien zajmować miejsca nad formularzem.
        UserRoleManagementPanel.IsVisible = !isVisible;
    }

    private void BuildRoleAccessEditors(ProjectRoleDefinitionModel? role)
    {
        RoleProjectsPanel.Children.Clear();

        var roleProjectKey =
            _selectedRoleAssignmentProjectKey ??
            role?.ProjectKeys.FirstOrDefault();

        var roleProject =
            _projects.FirstOrDefault(project =>
                string.Equals(
                    project.Key,
                    roleProjectKey,
                    StringComparison.OrdinalIgnoreCase));

        if (roleProject is not null)
        {
            RoleProjectsPanel.Children.Add(new CheckBox
            {
                Content = roleProject.Name,
                Tag = roleProject.Key,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = true,
                IsEnabled = false
            });
        }

        RoleMembersPanel.Children.Clear();
        foreach (var profile in _profiles)
        {
            var memberCheckBox = new CheckBox
            {
                Content = profile.Login,
                Tag = profile.Login,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = role is not null &&
                            profile.ProjectRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)
            };
            memberCheckBox.IsCheckedChanged += (_, _) => UpdateSelectAllMembersState();
            RoleMembersPanel.Children.Add(memberCheckBox);
        }

        UpdateSelectAllMembersState();
    }

    private void PopulateRolePalettePicker()
    {
        for (var index = 0; index < RolePalettes.Length; index++)
        {
            var palette = RolePalettes[index];
            RolePalettePicker.Items.Add(new ComboBoxItem
            {
                Tag = palette,
                Content = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new Border
                        {
                            Width = 18,
                            Height = 18,
                            CornerRadius = new Avalonia.CornerRadius(5),
                            BorderBrush = Brushes.Gray,
                            BorderThickness = new Avalonia.Thickness(1),
                            Background = new SolidColorBrush(Color.Parse(palette.Background))
                        },
                        new TextBlock
                        {
                            Text = palette.Name,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        }
                    }
                }
            });

            var paletteButton = new Button
            {
                Width = 32,
                Height = 32,
                Margin = new Avalonia.Thickness(0, 0, 7, 7),
                Padding = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(16),
                Background = new SolidColorBrush(Color.Parse(palette.Background)),
                BorderBrush = new SolidColorBrush(Color.Parse(palette.Border)),
                BorderThickness = new Avalonia.Thickness(2),
                Tag = index
            };

            ToolTip.SetTip(paletteButton, palette.Name);
            paletteButton.Click += RolePaletteCircle_OnClick;
            RolePalettePanel.Children.Add(paletteButton);
        }
    }

    private void RolePaletteCircle_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index } &&
            index >= 0 &&
            index < RolePalettePicker.Items.Count)
        {
            RolePalettePicker.SelectedIndex = index;
            UpdatePaletteCircleSelection(index);
        }
    }

    private void UpdatePaletteCircleSelection(int selectedIndex)
    {
        for (var index = 0; index < RolePalettePanel.Children.Count; index++)
        {
            if (RolePalettePanel.Children[index] is Button button)
            {
                button.BorderThickness = new Avalonia.Thickness(
                    index == selectedIndex ? 4 : 2);
            }
        }
    }

    private void SelectMatchingRolePalette(ProjectRoleDefinitionModel role)
    {
        _updatingPalette = true;
        try
        {
            RolePalettePicker.SelectedIndex = Array.FindIndex(
                RolePalettes,
                palette =>
                    string.Equals(palette.Border, role.BorderColor, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(palette.Background, role.BackgroundColor, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(palette.Text, role.TextColor, StringComparison.OrdinalIgnoreCase));
            UpdatePaletteCircleSelection(RolePalettePicker.SelectedIndex);
        }
        finally
        {
            _updatingPalette = false;
        }
    }

    private void RolePalettePicker_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingPalette ||
            RolePalettePicker.SelectedItem is not ComboBoxItem { Tag: RolePaletteOption palette })
        {
            return;
        }

        RoleColorTextBox.Text = palette.Border;
        RoleBackgroundColorTextBox.Text = palette.Background;
        RoleTextColorTextBox.Text = palette.Text;
        UpdatePaletteCircleSelection(RolePalettePicker.SelectedIndex);
        UpdateRolePreview();
    }

    private void RolePreviewValue_OnChanged(object? sender, TextChangedEventArgs e) =>
        UpdateRolePreview();

    private void UpdateRolePreview()
    {
        if (RolePreviewBorder is null || RolePreviewTextBlock is null)
        {
            return;
        }

        RolePreviewTextBlock.Text = string.IsNullOrWhiteSpace(RoleNameTextBox?.Text)
            ? "Podgląd roli"
            : RoleNameTextBox.Text.Trim();

        if (TryParseColor(RoleColorTextBox?.Text, out var border))
        {
            RolePreviewBorder.BorderBrush = new SolidColorBrush(border);
        }

        if (TryParseColor(RoleBackgroundColorTextBox?.Text, out var background))
        {
            RolePreviewBorder.Background = new SolidColorBrush(background);
        }

        if (TryParseColor(RoleTextColorTextBox?.Text, out var foreground))
        {
            RolePreviewTextBlock.Foreground = new SolidColorBrush(foreground);
        }
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        try
        {
            color = Color.Parse(value?.Trim() ?? string.Empty);
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }

    private bool TryReadColor(TextBox textBox, string label, out string color)
    {
        color = textBox.Text?.Trim() ?? string.Empty;
        if (TryParseColor(color, out _))
        {
            return true;
        }

        ShowResult($"Wybierz poprawny kolor {label} lub wpisz HEX, np. #7154B8.", false);
        textBox.Focus();
        return false;
    }

    private void SelectAllRoleMembersCheckBox_OnChanged(object? sender, RoutedEventArgs e)
    {
        if (_updatingMemberSelection)
        {
            return;
        }

        _updatingMemberSelection = true;
        try
        {
            var isChecked = SelectAllRoleMembersCheckBox.IsChecked == true;
            foreach (var checkBox in RoleMembersPanel.Children.OfType<CheckBox>())
            {
                checkBox.IsChecked = isChecked;
            }
        }
        finally
        {
            _updatingMemberSelection = false;
        }
    }

    private void UpdateSelectAllMembersState()
    {
        if (_updatingMemberSelection)
        {
            return;
        }

        var memberCheckBoxes = RoleMembersPanel.Children.OfType<CheckBox>().ToArray();
        _updatingMemberSelection = true;
        try
        {
            SelectAllRoleMembersCheckBox.IsChecked =
                memberCheckBoxes.Length > 0 && memberCheckBoxes.All(checkBox => checkBox.IsChecked == true);
        }
        finally
        {
            _updatingMemberSelection = false;
        }
    }

    private async void SaveRoleDefinitionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var name = RoleNameTextBox.Text?.Trim() ?? string.Empty;
        if (name.Length < 2)
        {
            ShowResult("Nazwa roli musi mieć co najmniej 2 znaki.", false);
            return;
        }

        if (_roles.Any(role => role.Id != _editedRoleId &&
                               string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ShowResult("Rola o tej nazwie już istnieje.", false);
            return;
        }

        if (!TryReadColor(RoleColorTextBox, "obramowania", out var color) ||
            !TryReadColor(RoleBackgroundColorTextBox, "tła", out var backgroundColor) ||
            !TryReadColor(RoleTextColorTextBox, "czcionki", out var textColor))
        {
            return;
        }

        var projectKeys = RoleProjectsPanel.Children.OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Tag?.ToString())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Cast<string>()
            .ToList();

        var members = RoleMembersPanel.Children.OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Tag?.ToString())
            .Where(login => !string.IsNullOrWhiteSpace(login))
            .Cast<string>()
            .ToArray();

        var role = _editedRoleId.HasValue
            ? _roles.First(item => item.Id == _editedRoleId.Value)
            : new ProjectRoleDefinitionModel { Id = Guid.NewGuid() };

        var oldName = role.Name;
        role.Name = name;
        role.BorderColor = color;
        role.BackgroundColor = backgroundColor;
        role.TextColor = textColor;
        role.IsProfessionalRole = true;
        role.ProjectKeys = projectKeys;
        if (!_editedRoleId.HasValue)
        {
            _roles.Add(role);
        }

        try
        {
            await _profileService.SaveRoleAndProjectDefinitionsAsync(_projects, _roles);
            if (!string.IsNullOrWhiteSpace(oldName) &&
                !string.Equals(oldName, name, StringComparison.OrdinalIgnoreCase))
            {
                await _profileService.SetProjectRoleMembersAsync(oldName, Array.Empty<string>());
            }

            await _profileService.SetProjectRoleMembersAsync(name, members);
            _editedRoleId = role.Id;
            ShowResult($"Rola {name} została zapisana.", true);
            ShowInlineSaveResult(RoleDefinitionSaveResultTextBlock, "✓ Zapisano", true);
            Close();
        }
        catch (Exception exception)
        {
            ShowResult(exception.Message, false);
            ShowInlineSaveResult(RoleDefinitionSaveResultTextBlock, "Nie udało się zapisać", false);
        }
    }

    private async void DeleteRoleDefinitionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_editedRoleId.HasValue)
        {
            ShowResult("Najpierw wybierz rolę do usunięcia.", false);
            return;
        }

        var role = _roles.First(item => item.Id == _editedRoleId.Value);
        var confirmation = new ConfirmDeleteWindow(
            "Usunąć rolę?",
            $"Rola „{role.Name}” zostanie usunięta wszystkim użytkownikom.",
            "USUŃ ROLĘ");
        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        _roles.Remove(role);
        await _profileService.SaveRoleAndProjectDefinitionsAsync(_projects, _roles);
        ShowResult($"Rola {role.Name} została usunięta.", true);
        Close();
    }

    private void ShowResult(string message, bool success)
    {
        ResultTextBlock.Text = message;
        ResultTextBlock.Foreground = success ? Brushes.SeaGreen : Brushes.IndianRed;
        ResultTextBlock.IsVisible = true;
    }

    private static void ShowInlineSaveResult(
        TextBlock textBlock,
        string message,
        bool success)
    {
        textBlock.Text = message;
        textBlock.Foreground = success ? Brushes.SeaGreen : Brushes.IndianRed;
        textBlock.IsVisible = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
