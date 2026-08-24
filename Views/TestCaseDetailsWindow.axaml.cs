using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using QARegressionManager.Models;

namespace QARegressionManager.Views;

public partial class TestCaseDetailsWindow : Window
{
    private readonly StackPanel _stepsPanel;

    public string CaseName => NameTextBox.Text?.Trim() ?? string.Empty;
    public string Summary => SummaryTextBox.Text?.Trim() ?? string.Empty;
    public string Preconditions => PreconditionsTextBox.Text?.Trim() ?? string.Empty;
    public List<string> Platforms => (PlatformsTextBox.Text ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public List<TestStepModel> Steps => _stepsPanel.Children
        .OfType<StepEditor>()
        .Select((editor, index) => editor.ToModel(index + 1))
        .ToList();

    public TestCaseDetailsWindow()
        : this(string.Empty, string.Empty, string.Empty, Array.Empty<string>(), Array.Empty<TestStepModel>())
    {
    }

    public TestCaseDetailsWindow(
        string name,
        string summary,
        string preconditions,
        IReadOnlyCollection<string> platforms,
        IReadOnlyCollection<TestStepModel> steps)
    {
        AvaloniaXamlLoader.Load(this);
        _stepsPanel = this.FindControl<StackPanel>("StepsPanel")!;
        NameTextBox.Text = name;
        SummaryTextBox.Text = summary;
        PreconditionsTextBox.Text = preconditions;
        PlatformsTextBox.Text = string.Join(", ", platforms);

        foreach (var step in steps.OrderBy(item => item.Number))
        {
            AddStep(step);
        }

        Opened += (_, _) => NameTextBox.Focus();
    }

    private void AddStep(TestStepModel? step = null)
    {
        var editor = new StepEditor(step);
        editor.RemoveRequested += (_, _) => _stepsPanel.Children.Remove(editor);
        _stepsPanel.Children.Add(editor);
    }

    private void AddStepButton_OnClick(object? sender, RoutedEventArgs e) => AddStep();

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(false);

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CaseName))
        {
            NameTextBox.Focus();
            return;
        }

        Close(true);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SaveButton_OnClick(null, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private sealed class StepEditor : Border
    {
        private readonly TextBox _actions;
        private readonly TextBox _expected;
        public event EventHandler? RemoveRequested;

        public StepEditor(TestStepModel? step)
        {
            Padding = new Thickness(12);
            CornerRadius = new CornerRadius(10);
            BorderThickness = new Thickness(1);
            BorderBrush = Application.Current?.FindResource("InputBorderBrush") as Avalonia.Media.IBrush;

            _actions = new TextBox { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 70, Text = step?.Actions ?? string.Empty };
            _expected = new TextBox { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 70, Text = step?.ExpectedResults ?? string.Empty };
            var remove = new Button { Content = "USUŃ KROK", HorizontalAlignment = HorizontalAlignment.Right };
            remove.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);

            Child = new StackPanel
            {
                Spacing = 7,
                Children =
                {
                    new TextBlock { Text = "CZYNNOŚCI", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                    _actions,
                    new TextBlock { Text = "OCZEKIWANY REZULTAT", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                    _expected,
                    remove
                }
            };
        }

        public TestStepModel ToModel(int number) => new()
        {
            Number = number,
            Actions = _actions.Text?.Trim() ?? string.Empty,
            ExpectedResults = _expected.Text?.Trim() ?? string.Empty
        };
    }
}
