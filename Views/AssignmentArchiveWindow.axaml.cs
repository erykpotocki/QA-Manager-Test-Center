using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class AssignmentArchiveWindow : Window
{
    private readonly AssignmentService _assignmentService = new();
    private readonly Dictionary<Guid, CheckBox> _selection = new();

    public AssignmentArchiveWindow()
    {
        InitializeComponent();
        Opened += async (_, _) => await LoadArchiveAsync();
        KeyDown += OnWindowKeyDown;
    }

    private async Task LoadArchiveAsync()
    {
        var assignments = await _assignmentService.GetArchivedAssignmentsAsync();
        ArchivedSessionsPanel.Children.Clear();
        _selection.Clear();

        foreach (var assignment in assignments
                     .OrderByDescending(item => item.ArchivedAt ?? item.UpdatedAt))
        {
            var checkBox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            _selection[assignment.Id] = checkBox;

            var total = assignment.TestCaseIds.Distinct().Count();
            var completed = assignment.CaseProgress.Count(progress =>
                IsFinalStatus(progress.Status));

            var title = new TextBlock
            {
                Text = $"{assignment.RecipientLogin} • {assignment.ProjectName} • v{assignment.ApplicationVersion}",
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };
            var details = new TextBlock
            {
                Text = $"Wykonano {completed} z {total} • zarchiwizowano {(assignment.ArchivedAt ?? assignment.UpdatedAt).ToLocalTime():dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            };
            var text = new StackPanel { Spacing = 3 };
            text.Children.Add(title);
            text.Children.Add(details);

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,12,*"),
                Margin = new Avalonia.Thickness(4, 2)
            };
            Grid.SetColumn(checkBox, 0);
            Grid.SetColumn(text, 2);
            row.Children.Add(checkBox);
            row.Children.Add(text);

            var border = new Border
            {
                Padding = new Avalonia.Thickness(12),
                CornerRadius = new Avalonia.CornerRadius(11),
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#66808080")),
                Background = Brushes.Transparent,
                Child = row
            };
            ArchivedSessionsPanel.Children.Add(border);
        }

        ArchiveResultTextBlock.Text = assignments.Length == 0
            ? "Archiwum jest puste."
            : $"Sesje w archiwum: {assignments.Length.ToString(CultureInfo.InvariantCulture)}";
        DeleteSelectedButton.IsEnabled = assignments.Length > 0;
        DeleteAllButton.IsEnabled = assignments.Length > 0;
        SelectAllCheckBox.IsChecked = false;
    }

    private static bool IsFinalStatus(string? status) =>
        status is "Success" or "Failed" or "Blocked" or "NotApplicable" or "NA" or "N/A";

    private void SelectAllCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = SelectAllCheckBox.IsChecked == true;
        foreach (var checkBox in _selection.Values)
        {
            checkBox.IsChecked = selected;
        }
    }

    private async void DeleteSelectedButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedIds = _selection
            .Where(pair => pair.Value.IsChecked == true)
            .Select(pair => pair.Key)
            .ToArray();
        if (selectedIds.Length == 0)
        {
            ArchiveResultTextBlock.Text = "Najpierw zaznacz sesje do trwałego usunięcia.";
            return;
        }

        var confirmation = new ConfirmDeleteWindow(
            "Usunąć zaznaczone sesje z archiwum?",
            $"Ta operacja trwale usunie {selectedIds.Length} sesji oraz powiązane powiadomienia.",
            "USUŃ TRWALE");
        var accepted = await confirmation.ShowDialog<bool>(this);
        if (!accepted)
        {
            return;
        }

        await _assignmentService.DeleteArchivedAssignmentsAsync(selectedIds);
        await LoadArchiveAsync();
    }

    private async void DeleteAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var confirmation = new ConfirmDeleteWindow(
            "Usunąć całe archiwum?",
            "Ta operacja trwale usunie wszystkie zarchiwizowane sesje oraz powiązane z nimi powiadomienia.",
            "USUŃ WSZYSTKO");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        var removed = await _assignmentService.DeleteAllArchivedAssignmentsAsync();
        await LoadArchiveAsync();
        ArchiveResultTextBlock.Text = removed == 0
            ? "Archiwum było już puste."
            : $"Trwale usunięto {removed} sesji z archiwum.";
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }
}
