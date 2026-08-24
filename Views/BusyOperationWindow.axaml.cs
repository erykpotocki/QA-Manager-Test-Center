using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class BusyOperationWindow : Window
{
    private readonly Func<Task> _operation;

    public BusyOperationWindow()
        : this(
            LocalizationService.T("Common.PleaseWait"),
            LocalizationService.T("Common.PerformingOperation"),
            () => Task.CompletedTask)
    {
    }

    public BusyOperationWindow(
        string title,
        string message,
        Func<Task> operation)
    {
        _operation =
            operation;

        AvaloniaXamlLoader.Load(
            this);

        var titleTextBlock =
            this.FindControl<TextBlock>(
                "TitleTextBlock");

        var messageTextBlock =
            this.FindControl<TextBlock>(
                "MessageTextBlock");

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

        Opened +=
            OnOpened;
    }

    public Exception? OperationException { get; private set; }

    private async void OnOpened(
        object? sender,
        EventArgs e)
    {
        try
        {
            // Oddanie sterowania pozwala Avalonia narysować okno przed
            // rozpoczęciem dłuższej serii zapisów.
            await Task.Yield();
            await _operation();
        }
        catch (Exception exception)
        {
            OperationException =
                exception;
        }
        finally
        {
            Close();
        }
    }
}
