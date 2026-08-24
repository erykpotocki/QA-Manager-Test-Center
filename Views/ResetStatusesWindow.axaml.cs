using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ResetStatusesWindow : Window
{
    private StackPanel? _scopeOptionsStackPanel;
    private RadioButton? _allProjectRadioButton;
    private RadioButton? _inProgressRadioButton;
    private RadioButton? _pendingAndNaRadioButton;

    private readonly List<(RadioButton RadioButton, ResetScopeOption Option)>
        _scopeButtons = new();

    public ResetStatusesWindow()
        : this(new[]
        {
            new ResetScopeOption("regression-root", LocalizationService.T("Explorer.RegressionTests")),
            new ResetScopeOption("functional-root", LocalizationService.T("Explorer.FunctionalTests"))
        })
    {
    }

    public ResetStatusesWindow(
        IEnumerable<ResetScopeOption> scopeOptions)
    {
        AvaloniaXamlLoader.Load(this);

        _scopeOptionsStackPanel =
            this.FindControl<StackPanel>(
                "ScopeOptionsStackPanel");

        _allProjectRadioButton =
            this.FindControl<RadioButton>(
                "AllProjectRadioButton");

        _inProgressRadioButton =
            this.FindControl<RadioButton>(
                "InProgressRadioButton");

        _pendingAndNaRadioButton =
            this.FindControl<RadioButton>(
                "PendingAndNaRadioButton");

        BuildScopeOptions(
            scopeOptions);
    }

    public ResetStatusesRequest Request { get; private set; } =
        new();

    private void BuildScopeOptions(
        IEnumerable<ResetScopeOption> scopeOptions)
    {
        if (_scopeOptionsStackPanel is null)
        {
            return;
        }

        _scopeOptionsStackPanel.Children.Clear();
        _scopeButtons.Clear();

        var options =
            scopeOptions
                .Where(
                    option =>
                        !string.IsNullOrWhiteSpace(
                            option.FolderKey))
                .ToList();

        foreach (var option in options)
        {
            var radioButton =
                new RadioButton
                {
                    Content =
                        option.DisplayName,

                    GroupName =
                        "Scope"
                };

            _scopeOptionsStackPanel.Children.Add(
                radioButton);

            _scopeButtons.Add(
                (
                    radioButton,
                    option));
        }

        if (_allProjectRadioButton is not null)
        {
            _allProjectRadioButton.IsChecked =
                true;
        }
    }

    private void ApplyButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        ApplyAndClose();
    }

    private void ApplyAndClose()
    {
        var selectedScope =
            _scopeButtons
                .FirstOrDefault(
                    item =>
                        item.RadioButton.IsChecked ==
                        true);

        Request =
            new ResetStatusesRequest
            {
                ScopeFolderKey =
                    _allProjectRadioButton?.IsChecked ==
                    true
                        ? null
                        : selectedScope.Option?.FolderKey,

                NewStatus =
                    _inProgressRadioButton?.IsChecked ==
                    true
                        ? "InProgress"
                        : "None",

                OnlyPendingAndNa =
                    _pendingAndNaRadioButton?.IsChecked ==
                    true
            };

        Close(
            true);
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            false);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            ApplyAndClose();

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

public sealed class ResetScopeOption
{
    public ResetScopeOption(
        string folderKey,
        string displayName)
    {
        FolderKey =
            folderKey;

        DisplayName =
            displayName;
    }

    public string FolderKey { get; }

    public string DisplayName { get; }
}

public sealed class ResetStatusesRequest
{
    public string? ScopeFolderKey { get; init; }

    public string NewStatus { get; init; } =
        "None";

    public bool OnlyPendingAndNa { get; init; }
}
