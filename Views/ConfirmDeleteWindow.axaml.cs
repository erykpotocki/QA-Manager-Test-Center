using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ConfirmDeleteWindow : Window
{
    private TextBlock? _titleTextBlock;
    private TextBlock? _messageTextBlock;
    private Button? _confirmButton;

    public ConfirmDeleteWindow()
    {
        InitializeComponent();
        FindControls();
    }

    public ConfirmDeleteWindow(
        string title,
        string message,
        string? confirmButtonText = null)
        : this()
    {
        if (_titleTextBlock is not null)
        {
            _titleTextBlock.Text =
                title;
        }

        if (_messageTextBlock is not null)
        {
            _messageTextBlock.Text =
                message;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Content =
                confirmButtonText ??
                LocalizationService.T("Common.Delete");
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(
            this);
    }

    private void FindControls()
    {
        _titleTextBlock =
            this.FindControl<TextBlock>(
                "TitleTextBlock");

        _messageTextBlock =
            this.FindControl<TextBlock>(
                "MessageTextBlock");

        _confirmButton =
            this.FindControl<Button>(
                "ConfirmButton");
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            false);
    }

    private void DeleteButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            true);
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
