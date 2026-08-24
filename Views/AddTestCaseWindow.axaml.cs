using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace QARegressionManager.Views;

public partial class AddTestCaseWindow : Window
{
    private TextBox? _testCaseNameTextBox;

    public string TestCaseName { get; private set; } = string.Empty;

    public AddTestCaseWindow()
    {
        InitializeComponent();

        _testCaseNameTextBox =
            this.FindControl<TextBox>("TestCaseNameTextBox");

        Opened += (_, _) =>
            Dispatcher.UIThread.Post(
                () =>
                {
                    _testCaseNameTextBox?.Focus();
                    _testCaseNameTextBox?.SelectAll();
                },
                DispatcherPriority.Input);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        AddAndClose();
    }

    private void AddAndClose()
    {
        var name = _testCaseNameTextBox?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        TestCaseName = name;

        Close(true);
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            AddAndClose();

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
