using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ProjectManagementWindow : Window
{
    private readonly UserProfileService _profileService = new();
    private ProjectDefinitionModel[] _projects = Array.Empty<ProjectDefinitionModel>();
    private ProjectRoleDefinitionModel[] _roles = Array.Empty<ProjectRoleDefinitionModel>();
    private bool _updatingRoleSelection;

    public ProjectManagementWindow()
    {
        InitializeComponent();
        SelectAllRolesCheckBox.IsCheckedChanged += (_, _) => SelectAllRolesCheckBox_OnChanged();
        Opened += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var definitions = await _profileService.GetRoleAndProjectDefinitionsAsync();
        _projects = definitions.Projects;
        _roles = definitions.Roles;
        ProjectComboBox.Items.Clear();
        ProjectComboBox.Items.Add(new ComboBoxItem { Content = "Brak — nie wybrano projektu" });
        foreach (var project in _projects
                     .OrderBy(project => DemoCatalog.IsTestProject(project.Name) ? 1 : 0)
                     .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase))
            ProjectComboBox.Items.Add(new ComboBoxItem { Content = project.Name, Tag = project });
        ProjectComboBox.SelectedIndex = 0;
        SelectedProjectPanel.IsVisible = false;
    }

    private void ProjectComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem is ComboBoxItem { Tag: ProjectDefinitionModel project })
        {
            SelectedProjectNameTextBlock.Text = project.Name;
            PopulateProjectRoles(project);
            SelectedProjectPanel.IsVisible = true;
            return;
        }
        SelectedProjectPanel.IsVisible = false;
    }

    private void PopulateProjectRoles(ProjectDefinitionModel project)
    {
        _updatingRoleSelection = true;
        ProjectRolesPanel.Children.Clear();

        foreach (var role in _roles.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            ProjectRolesPanel.Children.Add(new CheckBox
            {
                Content = role.Name,
                Tag = role,
                IsChecked = role.ProjectKeys.Contains(project.Key, StringComparer.OrdinalIgnoreCase)
            });
        }

        SelectAllRolesCheckBox.IsChecked =
            _roles.Length > 0 &&
            ProjectRolesPanel.Children.OfType<CheckBox>().All(item => item.IsChecked == true);
        _updatingRoleSelection = false;
    }

    private void SelectAllRolesCheckBox_OnChanged()
    {
        if (_updatingRoleSelection)
        {
            return;
        }

        var isChecked = SelectAllRolesCheckBox.IsChecked == true;
        foreach (var checkBox in ProjectRolesPanel.Children.OfType<CheckBox>())
        {
            checkBox.IsChecked = isChecked;
        }
    }

    private async void SaveProjectRolesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem is not ComboBoxItem { Tag: ProjectDefinitionModel project })
        {
            ShowResult("Wybierz projekt.", false);
            return;
        }

        var selectedRoleIds = ProjectRolesPanel.Children
            .OfType<CheckBox>()
            .Where(item => item.IsChecked == true)
            .Select(item => (item.Tag as ProjectRoleDefinitionModel)?.Id)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToHashSet();

        foreach (var role in _roles)
        {
            role.ProjectKeys.RemoveAll(key =>
                string.Equals(key, project.Key, StringComparison.OrdinalIgnoreCase));

            if (selectedRoleIds.Contains(role.Id))
            {
                role.ProjectKeys.Add(project.Key);
            }
        }

        await _profileService.SaveRoleAndProjectDefinitionsAsync(_projects, _roles);
        ShowResult("✓ Zapisano dostęp ról do projektu.", true);
        PopulateProjectRoles(project);
    }

    private void ShowCreationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowCreationButton.IsVisible = false;
        CreationPanel.IsVisible = true;
        NewProjectNameTextBox.Focus();
    }

    private void CancelCreationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NewProjectNameTextBox.Text = string.Empty;
        CreationPanel.IsVisible = false;
        ShowCreationButton.IsVisible = true;
    }

    private async void CreateProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var name = NewProjectNameTextBox.Text?.Trim() ?? string.Empty;
        if (name.Length < 2) { ShowResult("Podaj nazwę projektu.", false); return; }
        if (_projects.Any(project => string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase)))
        { ShowResult("Projekt o tej nazwie już istnieje.", false); return; }

        var keyBase = new string(name.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray()).Trim('-');
        var key = keyBase;
        var suffix = 2;
        while (_projects.Any(project => string.Equals(project.Key, key, StringComparison.OrdinalIgnoreCase))) key = $"{keyBase}-{suffix++}";

        var projects = _projects.Append(new ProjectDefinitionModel { Id = Guid.NewGuid(), Key = key, Name = name }).ToArray();
        await _profileService.SaveRoleAndProjectDefinitionsAsync(projects, _roles);
        NewProjectNameTextBox.Text = string.Empty;
        CreationPanel.IsVisible = false;
        ShowCreationButton.IsVisible = true;
        ShowResult("✓ Zapisano", true);
        await LoadAsync();
    }

    private async void DeleteProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem is not ComboBoxItem { Tag: ProjectDefinitionModel project }) return;
        var confirmation = new ConfirmDeleteWindow("Usunąć projekt?",
            $"Projekt „{project.Name}” zostanie usunięty z listy i odłączony od wszystkich ról.", "USUŃ PROJEKT");
        if (!await confirmation.ShowDialog<bool>(this)) return;
        await _profileService.SaveRoleAndProjectDefinitionsAsync(_projects.Where(item => item.Id != project.Id), _roles);
        ShowResult($"Projekt „{project.Name}” został usunięty.", true);
        await LoadAsync();
    }

    private void ShowResult(string message, bool success)
    {
        ResultTextBlock.Text = message;
        ResultTextBlock.Foreground = success ? Brushes.SeaGreen : Brushes.IndianRed;
        ResultTextBlock.IsVisible = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
