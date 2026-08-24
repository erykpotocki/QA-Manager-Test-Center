using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace QARegressionManager.Views;

public partial class RenameItemWindow : Window
{
    private TextBlock? _titleTextBlock;

    private TextBox? _nameTextBox;

    public string NewName { get; private set; } =
        string.Empty;

    public RenameItemWindow()
    {
        InitializeComponent();

        FindControls();

        Opened +=
            RenameItemWindow_OnOpened;
    }

    public RenameItemWindow(
        string title,
        string currentName)
        : this()
    {
        if (_titleTextBlock is not null)
        {
            _titleTextBlock.Text =
                title;
        }

        if (_nameTextBox is not null)
        {
            _nameTextBox.Text =
                currentName;
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

        _nameTextBox =
            this.FindControl<TextBox>(
                "NameTextBox");
    }

    private void RenameItemWindow_OnOpened(
        object? sender,
        System.EventArgs e)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_nameTextBox is null)
                {
                    return;
                }

                _nameTextBox.Focus();

                _nameTextBox.SelectAll();
            },
            DispatcherPriority.Input);
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

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            SaveAndClose();

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

    private void SaveAndClose()
    {
        var name =
            _nameTextBox?
                .Text?
                .Trim();

        if (string.IsNullOrWhiteSpace(
                name))
        {
            return;
        }

        NewName =
            name;

        Close(
            true);
    }
}