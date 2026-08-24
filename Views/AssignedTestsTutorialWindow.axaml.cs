using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace QARegressionManager.Views;

public partial class AssignedTestsTutorialWindow : Window
{
    private readonly IReadOnlyList<(string Title, string Message)> _pages =
        new[]
        {
            ("Szybkie oznaczanie wyniku",
             "Kliknij przypadek dwukrotnie, aby oznaczyć go jako Sukces. Wynik możesz również wybrać z listy statusów."),
            ("Płynne przechodzenie przez przypadki",
             "Inteligentne przewijanie pozostawia ostatnio wykonany przypadek w zasięgu wzroku i pokazuje kolejne pozycje."),
            ("Komentarz dla zablokowanego testu",
             "Status Zablokowany wymaga krótkiego komentarza. Po wpisaniu przyczyna zostanie zapisana razem z wynikiem."),
            ("Zakończenie przypisanych testów",
             "Po uzupełnieniu wszystkich przypadków zakończ testy. Możesz wysłać wyniki bez raportu albo dodatkowo wygenerować raport.")
        };

    private readonly Border[] _dots;
    private readonly TextBlock _title;
    private readonly TextBlock _message;
    private readonly Button _next;
    private readonly CheckBox _dontShowAgain;
    private int _pageIndex;

    public bool DontShowAgain => _dontShowAgain.IsChecked == true;

    public AssignedTestsTutorialWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _title = this.FindControl<TextBlock>("TutorialTitleTextBlock")!;
        _message = this.FindControl<TextBlock>("TutorialMessageTextBlock")!;
        _next = this.FindControl<Button>("NextButton")!;
        _dontShowAgain = this.FindControl<CheckBox>("DontShowAgainCheckBox")!;
        _dots = new[]
        {
            this.FindControl<Border>("TutorialDot0")!,
            this.FindControl<Border>("TutorialDot1")!,
            this.FindControl<Border>("TutorialDot2")!,
            this.FindControl<Border>("TutorialDot3")!
        };
        ShowPage();
    }

    private void ShowPage()
    {
        var page = _pages[_pageIndex];
        _title.Text = page.Title;
        _message.Text = page.Message;
        _next.Content = _pageIndex == _pages.Count - 1 ? "ROZUMIEM" : "DALEJ";

        for (var index = 0; index < _dots.Length; index++)
        {
            var active = index == _pageIndex;
            _dots[index].Width = active ? 18 : 8;
            _dots[index].Background = new SolidColorBrush(
                Color.Parse(active ? "#2E86D1" : "#AAB4BE"));
        }
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e) => MoveNext();

    private void MoveNext()
    {
        if (_pageIndex >= _pages.Count - 1)
        {
            Close();
            return;
        }

        _pageIndex++;
        ShowPage();
    }

    private void SkipButton_OnClick(object? sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter)
        {
            MoveNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
