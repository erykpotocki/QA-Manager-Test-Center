using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace QARegressionManager.Views;

public partial class HelpInfoWindow : Window
{
    public HelpInfoWindow()
    {
        AvaloniaXamlLoader.Load(
            this);
    }

    private void CloseButton_OnClick(
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
