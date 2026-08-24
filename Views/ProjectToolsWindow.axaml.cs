using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace QARegressionManager.Views;

public partial class ProjectToolsWindow : Window
{
    public ProjectToolsWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(ProjectToolsAction.Import);
    }

    private void ExportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(ProjectToolsAction.Export);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(ProjectToolsAction.None);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key != Key.Escape)
        {
            return;
        }

        Close(
            ProjectToolsAction.None);

        e.Handled =
            true;
    }
}

public enum ProjectToolsAction
{
    None,
    Import,
    Export
}
