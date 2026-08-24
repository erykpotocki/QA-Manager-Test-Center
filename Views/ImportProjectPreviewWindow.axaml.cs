using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QARegressionManager.Models;

namespace QARegressionManager.Views;

public partial class ImportProjectPreviewWindow : Window
{
    private RadioButton? _overwriteStatusesRadioButton;

    public ImportProjectPreviewWindow()
    {
        AvaloniaXamlLoader.Load(
            this);

        _overwriteStatusesRadioButton =
            this.FindControl<RadioButton>(
                "OverwriteStatusesRadioButton");
    }

    public ImportProjectPreviewWindow(
        ProjectPackageMetadata metadata)
        : this()
    {
        var projectNameTextBlock =
            this.FindControl<TextBlock>(
                "ProjectNameTextBlock");

        var applicationVersionTextBlock =
            this.FindControl<TextBlock>(
                "ApplicationVersionTextBlock");

        var testerNameTextBlock =
            this.FindControl<TextBlock>(
                "TesterNameTextBlock");

        var exportedAtTextBlock =
            this.FindControl<TextBlock>(
                "ExportedAtTextBlock");

        if (projectNameTextBlock is not null)
        {
            projectNameTextBlock.Text =
                metadata.ProjectName;
        }

        if (applicationVersionTextBlock is not null)
        {
            applicationVersionTextBlock.Text =
                metadata.ApplicationVersion;
        }

        if (testerNameTextBlock is not null)
        {
            testerNameTextBlock.Text =
                metadata.TesterName;
        }

        if (exportedAtTextBlock is not null)
        {
            exportedAtTextBlock.Text =
                metadata.ExportedAt
                    .ToLocalTime()
                    .ToString(
                        "dd.MM.yyyy HH:mm");
        }
    }

    public bool OverwriteStatuses =>
        _overwriteStatusesRadioButton?
            .IsChecked ==
        true;

    private void ImportButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
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
            Close(
                true);

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
