using System.Linq;
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
    private Button? _alternateButton;
    private StackPanel? _pinPanel;
    private TextBox? _pinTextBox;
    private TextBlock? _pinLabelTextBlock;
    private TextBlock? _pinValidationTextBlock;
    private bool _useThreeChoices;

    public string EnteredPin =>
        _pinTextBox?.Text?.Trim() ?? string.Empty;

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

    public ConfirmDeleteWindow(
        string title,
        string message,
        string confirmButtonText,
        string alternateButtonText)
        : this(
            title,
            message,
            confirmButtonText)
    {
        _useThreeChoices = true;

        if (_alternateButton is not null)
        {
            _alternateButton.Content =
                alternateButtonText;
            _alternateButton.IsVisible =
                true;
        }

        Width = 760;
        MinWidth = 720;
        Height = 420;
        MinHeight = 390;
    }

    public void RequirePin(
        string label = "PIN ADMINISTRATORA")
    {
        if (_pinPanel is not null)
        {
            _pinPanel.IsVisible = true;
        }

        if (_pinLabelTextBlock is not null)
        {
            _pinLabelTextBlock.Text = label;
        }

        Height = 455;
        MinHeight = 455;
        _pinTextBox?.Focus();
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

        _alternateButton =
            this.FindControl<Button>(
                "AlternateButton");

        _pinPanel = this.FindControl<StackPanel>("PinPanel");
        _pinTextBox = this.FindControl<TextBox>("PinTextBox");
        _pinLabelTextBlock = this.FindControl<TextBlock>("PinLabelTextBlock");
        _pinValidationTextBlock = this.FindControl<TextBlock>("PinValidationTextBlock");
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_useThreeChoices)
        {
            Close(
                OperationConfirmationChoice.Cancel);
        }
        else
        {
            Close(
                false);
        }
    }

    private void DeleteButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!CanConfirm())
        {
            return;
        }

        if (_useThreeChoices)
        {
            Close(
                OperationConfirmationChoice.Confirm);
        }
        else
        {
            Close(
                true);
        }
    }

    private void AlternateButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            OperationConfirmationChoice.Alternate);
    }

    private bool CanConfirm()
    {
        if (_pinPanel?.IsVisible != true)
        {
            return true;
        }

        var valid =
            EnteredPin.Length == 6 &&
            EnteredPin.All(char.IsDigit);

        if (_pinValidationTextBlock is not null)
        {
            _pinValidationTextBlock.IsVisible = !valid;
        }

        return valid;
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            if (!CanConfirm())
            {
                e.Handled = true;
                return;
            }

            if (_useThreeChoices)
            {
                Close(
                    OperationConfirmationChoice.Confirm);
            }
            else
            {
                Close(
                    true);
            }

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_useThreeChoices)
            {
                Close(
                    OperationConfirmationChoice.Cancel);
            }
            else
            {
                Close(
                    false);
            }

            e.Handled =
                true;
        }
    }
}

public enum OperationConfirmationChoice
{
    Cancel,
    Confirm,
    Alternate
}
