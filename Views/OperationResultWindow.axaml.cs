using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class OperationResultWindow : Window
{
    public OperationResultWindow()
        : this(
            true,
            LocalizationService.T("Common.OperationCompleted"),
            string.Empty)
    {
    }

    public OperationResultWindow(
        bool success,
        string title,
        string message)
    {
        AvaloniaXamlLoader.Load(
            this);

        var iconBorder =
            this.FindControl<Border>(
                "ResultIconBorder");

        var iconTextBlock =
            this.FindControl<TextBlock>(
                "ResultIconTextBlock");

        var titleTextBlock =
            this.FindControl<TextBlock>(
                "TitleTextBlock");

        var messageTextBlock =
            this.FindControl<TextBlock>(
                "MessageTextBlock");

        if (iconBorder is not null)
        {
            iconBorder.Background =
                new SolidColorBrush(
                    Color.Parse(
                        success
                            ? "#2428C76F"
                            : "#24DC4C56"));
        }

        if (iconTextBlock is not null)
        {
            iconTextBlock.Text =
                success
                    ? "✓"
                    : "!";

            iconTextBlock.Foreground =
                new SolidColorBrush(
                    Color.Parse(
                        success
                            ? "#19944D"
                            : "#DC4C56"));
        }

        if (titleTextBlock is not null)
        {
            titleTextBlock.Text =
                title;
        }

        if (messageTextBlock is not null)
        {
            messageTextBlock.Text =
                message;
        }
    }

    private void OkButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter ||
            e.Key == Key.Escape)
        {
            Close();

            e.Handled =
                true;
        }
    }
}
