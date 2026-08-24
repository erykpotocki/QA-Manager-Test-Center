using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace QARegressionManager.Views;

public partial class AssignmentCompletionConfirmationWindow : Window
{
    public AssignmentCompletionConfirmationWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;
    }

    public bool DontShowAgain =>
        DontShowAgainCheckBox.IsChecked == true;

    private void FinishButton_OnClick(object? sender, RoutedEventArgs e) =>
        Close(AssignmentCompletionChoice.FinishWithoutReport);

    private void FinishWithReportButton_OnClick(object? sender, RoutedEventArgs e) =>
        Close(AssignmentCompletionChoice.FinishWithReport);

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) =>
        Close(AssignmentCompletionChoice.Cancel);

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Close(AssignmentCompletionChoice.FinishWithoutReport);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(AssignmentCompletionChoice.Cancel);
        }
    }
}

public enum AssignmentCompletionChoice
{
    Cancel,
    FinishWithoutReport,
    FinishWithReport
}
