using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace QARegressionManager.Views;

public partial class CollectionDescriptionWindow : Window
{
    public const int MaximumDescriptionLength =
        180;

    private TextBox? _descriptionTextBox;
    private TextBlock? _characterCountTextBlock;

    public string Description { get; private set; } =
        string.Empty;

    public CollectionDescriptionWindow()
        : this(
            string.Empty)
    {
    }

    public CollectionDescriptionWindow(
        string? currentDescription)
    {
        AvaloniaXamlLoader.Load(
            this);

        _descriptionTextBox =
            this.FindControl<TextBox>(
                "DescriptionTextBox");

        _characterCountTextBlock =
            this.FindControl<TextBlock>(
                "CharacterCountTextBlock");

        if (_descriptionTextBox is not null)
        {
            _descriptionTextBox.Text =
                (currentDescription ?? string.Empty)
                    [..Math.Min(
                        currentDescription?.Length ?? 0,
                        MaximumDescriptionLength)];

            _descriptionTextBox.AddHandler(
                InputElement.KeyDownEvent,
                DescriptionTextBox_OnPreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        UpdateCharacterCount();

        Opened +=
            (
                _,
                _) =>
            {
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (_descriptionTextBox is null)
                        {
                            return;
                        }

                        _descriptionTextBox.Focus();

                        _descriptionTextBox.CaretIndex =
                            _descriptionTextBox.Text?.Length
                            ?? 0;
                    },
                    DispatcherPriority.Loaded);
            };
    }

    private void DescriptionTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        UpdateCharacterCount();
    }

    private void DescriptionTextBox_OnPreviewKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            !e.KeyModifiers.HasFlag(
                KeyModifiers.Control))
        {
            return;
        }

        SaveAndClose();

        e.Handled =
            true;
    }

    private void UpdateCharacterCount()
    {
        if (_characterCountTextBlock is null)
        {
            return;
        }

        var characterCount =
            _descriptionTextBox?.Text?.Length
            ?? 0;

        _characterCountTextBlock.Text =
            $"{characterCount}/{MaximumDescriptionLength}";
    }

    private void SaveButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        SaveAndClose();
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            false);
    }

    private void SaveAndClose()
    {
        Description =
            _descriptionTextBox?.Text
                ?.Trim()
            ?? string.Empty;

        Close(
            true);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Escape)
        {
            Close(
                false);

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Enter &&
            e.KeyModifiers.HasFlag(
                KeyModifiers.Control))
        {
            SaveAndClose();

            e.Handled =
                true;
        }
    }
}
