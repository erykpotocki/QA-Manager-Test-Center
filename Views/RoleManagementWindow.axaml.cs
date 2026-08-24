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
    private readonly UserProfileService _profileService = new();
    private readonly AssignmentService _assignmentService = new();
    private readonly string _changedByLogin;
    private UserProfileModel[] _profiles = Array.Empty<UserProfileModel>();
    private ProjectDefinitionModel[] _projects = Array.Empty<ProjectDefinitionModel>();
    private List<ProjectRoleDefinitionModel> _roles = new();
    private Guid? _editedRoleId;

    public RoleManagementWindow() : this("Administrator")
    {
    }

    public RoleManagementWindow(string changedByLogin)
    {
        _changedByLogin = string.IsNullOrWhiteSpace(changedByLogin)
            ? "Administrator"
            : changedByLogin.Trim();

        InitializeComponent();
        Opened += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync(Guid? selectedProfileId = null, Guid? selectedRoleId = null)
    {
        _profiles = (await _profileService.GetProfilesAsync()).ToArray();
        var definitions = await _profileService.GetRoleAndProjectDefinitionsAsync();
        _projects = definitions.Projects;
        _roles = definitions.Roles.ToList();

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
        foreach (var role in _roles)
        {
            RoleDefinitionComboBox.Items.Add(new ComboBoxItem { Content = role.Name, Tag = role });
        }

        var roleIndex = FindIndex<ProjectRoleDefinitionModel>(
            RoleDefinitionComboBox,
            role => selectedRoleId.HasValue && role.Id == selectedRoleId.Value);
        RoleDefinitionComboBox.SelectedIndex = roleIndex;

        if (RoleDefinitionComboBox.ItemCount == 0 || roleIndex < 0)
        {
            BeginNewRole();
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

        UserProjectRolesPanel.Children.Clear();
        foreach (var role in _roles)
        {
            UserProjectRolesPanel.Children.Add(new CheckBox
            {
                Content = role.Name,
                Tag = role.Name,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = profile.ProjectRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)
            });
        }
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

        var projectRoles = UserProjectRolesPanel.Children
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Tag?.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        try
        {
            await _profileService.UpdateRolesAsync(profile.Id, systemRoles, projectRoles);
            await _assignmentService.SendUserNotificationAsync(
                profile.Login,
                "Zmieniono Twoje role",
                $"{_changedByLogin} zmienił Twoje role i dostęp do projektów.");
            ShowResult($"Role użytkownika {profile.Login} zostały zapisane.", true);
            await LoadAllAsync(profile.Id, _editedRoleId);
        }
        catch (Exception exception)
        {
            ShowResult(exception.Message, false);
        }
    }

    private void RoleDefinitionComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RoleDefinitionComboBox.SelectedItem is ComboBoxItem { Tag: ProjectRoleDefinitionModel role })
        {
            EditRole(role);
        }
    }

    private void NewRoleButton_OnClick(object? sender, RoutedEventArgs e) => BeginNewRole();

    private void BeginNewRole()
    {
        _editedRoleId = null;
        RoleDefinitionComboBox.SelectedIndex = -1;
        RoleNameTextBox.Text = string.Empty;
        RoleColorTextBox.Text = "#7154B8";
        BuildRoleAccessEditors(null);
        RoleNameTextBox.Focus();
    }

    private void EditRole(ProjectRoleDefinitionModel role)
    {
        _editedRoleId = role.Id;
        RoleNameTextBox.Text = role.Name;
        RoleColorTextBox.Text = role.BorderColor;
        BuildRoleAccessEditors(role);
    }

    private void BuildRoleAccessEditors(ProjectRoleDefinitionModel? role)
    {
        RoleProjectsPanel.Children.Clear();
        foreach (var project in _projects)
        {
            RoleProjectsPanel.Children.Add(new CheckBox
            {
                Content = project.Name,
                Tag = project.Key,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = role?.ProjectKeys.Contains(project.Key, StringComparer.OrdinalIgnoreCase) == true
            });
        }

        RoleMembersPanel.Children.Clear();
        foreach (var profile in _profiles)
        {
            RoleMembersPanel.Children.Add(new CheckBox
            {
                Content = profile.Login,
                Tag = profile.Login,
                Margin = new Avalonia.Thickness(0, 0, 18, 8),
                IsChecked = role is not null &&
                            profile.ProjectRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)
            });
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

        var color = RoleColorTextBox.Text?.Trim() ?? string.Empty;
        try
        {
            _ = Color.Parse(color);
        }
        catch
        {
            ShowResult("Podaj kolor w formacie HEX, np. #7154B8.", false);
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
            await LoadAllAsync(null, role.Id);
        }
        catch (Exception exception)
        {
            ShowResult(exception.Message, false);
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
        await LoadAllAsync();
    }

    private async void CreateProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var name = NewProjectNameTextBox.Text?.Trim() ?? string.Empty;
        if (name.Length < 2)
        {
            ShowResult("Podaj nazwę nowego projektu.", false);
            return;
        }

        if (_projects.Any(project => string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ShowResult("Projekt o tej nazwie już istnieje.", false);
            return;
        }

        var keyBase = new string(name.ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray()).Trim('-');
        var key = keyBase;
        var suffix = 2;
        while (_projects.Any(project => string.Equals(project.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            key = $"{keyBase}-{suffix++}";
        }

        var projects = _projects.ToList();
        projects.Add(new ProjectDefinitionModel { Id = Guid.NewGuid(), Key = key, Name = name });
        await _profileService.SaveRoleAndProjectDefinitionsAsync(projects, _roles);
        NewProjectNameTextBox.Text = string.Empty;
        ShowResult($"Projekt {name} został utworzony. Przypisz go teraz do wybranej roli.", true);
        await LoadAllAsync(null, _editedRoleId);
    }

    private void ShowResult(string message, bool success)
    {
        ResultTextBlock.Text = message;
        ResultTextBlock.Foreground = success ? Brushes.SeaGreen : Brushes.IndianRed;
        ResultTextBlock.IsVisible = true;
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
