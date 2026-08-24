using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ContinueSessionWindow : Window
{
    private TextBlock? _projectNameTextBlock;
    private TextBlock? _applicationVersionTextBlock;
    private TextBlock? _testerNameTextBlock;
    private TextBlock? _sessionModeTextBlock;
    private TextBlock? _lastTestTextBlock;
    private TextBlock? _lastSaveTextBlock;

    public ContinueSessionWindow()
    {
        InitializeComponent();
        FindControls();
    }

    public ContinueSessionWindow(
        string projectName,
        string applicationVersion,
        string testerName,
        string sessionMode,
        string lastTestName,
        DateTimeOffset lastSaveTime)
        : this()
    {
        SetText(
            _projectNameTextBlock,
            projectName);

        SetText(
            _applicationVersionTextBlock,
            applicationVersion);

        SetText(
            _testerNameTextBlock,
            testerName);

        SetText(
            _sessionModeTextBlock,
            string.Equals(
                sessionMode,
                "Assigned",
                StringComparison.OrdinalIgnoreCase)
                ? LocalizationService.T("ContinueSession.AssignedMode")
                : LocalizationService.T("ContinueSession.AdHocMode"));

        SetText(
            _lastTestTextBlock,
            string.IsNullOrWhiteSpace(
                lastTestName)
                ? LocalizationService.T("ContinueSession.NotSpecified")
                : lastTestName);

        SetText(
            _lastSaveTextBlock,
            lastSaveTime
                .ToLocalTime()
                .ToString(
                    "g",
                    CultureInfo.GetCultureInfo(
                        LocalizationService.IsPolish ? "pl-PL" : "en-US")));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(
            this);
    }

    private void FindControls()
    {
        _projectNameTextBlock =
            this.FindControl<TextBlock>(
                "ProjectNameTextBlock");

        _applicationVersionTextBlock =
            this.FindControl<TextBlock>(
                "ApplicationVersionTextBlock");

        _testerNameTextBlock =
            this.FindControl<TextBlock>(
                "TesterNameTextBlock");

        _sessionModeTextBlock =
            this.FindControl<TextBlock>(
                "SessionModeTextBlock");

        _lastTestTextBlock =
            this.FindControl<TextBlock>(
                "LastTestTextBlock");

        _lastSaveTextBlock =
            this.FindControl<TextBlock>(
                "LastSaveTextBlock");
    }

    private static void SetText(
        TextBlock? textBlock,
        string value)
    {
        if (textBlock is not null)
        {
            textBlock.Text =
                value;
        }
    }

    private void DismissButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            ContinueSessionResult.Dismiss);
    }

    private void ContinueButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            ContinueSessionResult.Continue);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            Close(
                ContinueSessionResult.Continue);

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Escape)
        {
            Close(
                ContinueSessionResult.Dismiss);

            e.Handled =
                true;
        }
    }
}

public enum ContinueSessionResult
{
    Continue,
    Dismiss
}
