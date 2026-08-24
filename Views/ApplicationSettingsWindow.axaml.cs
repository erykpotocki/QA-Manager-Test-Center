using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ApplicationSettingsWindow : Window
{
    private ComboBox? _themeComboBox;
    private ComboBox? _textSizeComboBox;
    private ComboBox? _fontFamilyComboBox;
    private ComboBox? _fontWeightComboBox;
    private ComboBox? _accentColorComboBox;
    private ComboBox? _languageComboBox;
    private Border? _previewBorder;
    private TextBlock? _previewTitleTextBlock;
    private TextBlock? _previewBodyTextBlock;
    private bool _isInitializing = true;
    private bool _settingsSaved;
    private readonly string _savedLanguage = LocalizationService.CurrentLanguage;

    public ApplicationSettingsWindow()
    {
        AvaloniaXamlLoader.Load(
            this);

        _themeComboBox =
            this.FindControl<ComboBox>("ThemeComboBox");
        _textSizeComboBox =
            this.FindControl<ComboBox>("TextSizeComboBox");
        _fontFamilyComboBox =
            this.FindControl<ComboBox>("FontFamilyComboBox");
        _fontWeightComboBox =
            this.FindControl<ComboBox>("FontWeightComboBox");
        _accentColorComboBox =
            this.FindControl<ComboBox>("AccentColorComboBox");
        _languageComboBox =
            this.FindControl<ComboBox>("LanguageComboBox");
        _previewBorder =
            this.FindControl<Border>("PreviewBorder");
        _previewTitleTextBlock =
            this.FindControl<TextBlock>("PreviewTitleTextBlock");
        _previewBodyTextBlock =
            this.FindControl<TextBlock>("PreviewBodyTextBlock");

        SelectByTag(
            _themeComboBox,
            ApplicationAppearanceService.Current.Theme);
        SelectByTag(
            _textSizeComboBox,
            ApplicationAppearanceService.Current.TextSize);
        SelectByTag(
            _fontFamilyComboBox,
            ApplicationAppearanceService.Current.FontFamily);
        SelectByTag(
            _fontWeightComboBox,
            ApplicationAppearanceService.Current.UseSemiBoldText
                ? "SemiBold"
                : "Normal");
        SelectByTag(
            _accentColorComboBox,
            ApplicationAppearanceService.Current.AccentColor);
        SelectByTag(
            _languageComboBox,
            LocalizationService.CurrentLanguage);
        SubscribeToPreviewChanges();
        _isInitializing = false;
        RefreshComboBoxTextColors(
            GetSelectedSettings());
        UpdatePreview(
            GetSelectedSettings());
    }

    private void SaveButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        ApplicationAppearanceService.SaveAndApply(
            GetSelectedSettings());
        LocalizationService.SaveAndApply(
            GetSelectedTag(_languageComboBox, LocalizationService.English));

        _settingsSaved = true;

        Close(
            true);
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        RestoreSavedAppearance();

        Close(
            false);
    }

    protected override void OnClosed(
        EventArgs e)
    {
        if (!_settingsSaved)
        {
            RestoreSavedAppearance();
        }

        base.OnClosed(
            e);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Escape)
        {
            CancelButton_OnClick(
                this,
                new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            SaveButton_OnClick(
                this,
                new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SubscribeToPreviewChanges()
    {
        foreach (var comboBox in new[]
                 {
                     _themeComboBox,
                     _textSizeComboBox,
                     _fontFamilyComboBox,
                     _fontWeightComboBox,
                     _accentColorComboBox
                 })
        {
            if (comboBox is not null)
            {
                comboBox.SelectionChanged +=
                    AppearanceComboBox_OnSelectionChanged;
            }
        }
    }

    private void AppearanceComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        var settings =
            GetSelectedSettings();

        ApplicationAppearanceService.ApplyPreview(
            settings);

        RecreateComboBoxSelectionPresenters();

        RefreshComboBoxTextColors(
            settings);

        // SelectedItem bywa renderowany ponownie chwilę po zmianie wariantu
        // motywu. Drugi przebieg po renderze zapobiega zachowaniu koloru
        // tekstu z poprzedniego motywu w obu kierunkach Light/Dark.
        Dispatcher.UIThread.Post(
            () =>
            {
                RecreateComboBoxSelectionPresenters();
                RefreshComboBoxTextColors(settings);
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        RecreateComboBoxSelectionPresenters();
                        RefreshComboBoxTextColors(settings);
                    },
                    DispatcherPriority.Background);
            },
            DispatcherPriority.Render);

        UpdatePreview(
            settings);
    }

    private ApplicationAppearanceSettings GetSelectedSettings() =>
        new()
        {
            Theme = GetSelectedTag(_themeComboBox, "Light"),
            AccentColor = GetSelectedTag(_accentColorComboBox, "Blue"),
            TextSize = GetSelectedTag(_textSizeComboBox, "Standard"),
            FontFamily = GetSelectedTag(_fontFamilyComboBox, "Inter"),
            UseSemiBoldText = string.Equals(
                GetSelectedTag(_fontWeightComboBox, "Normal"),
                "SemiBold",
                StringComparison.OrdinalIgnoreCase)
        };

    private void UpdatePreview(
        ApplicationAppearanceSettings settings)
    {
        var isDark =
            string.Equals(
                settings.Theme,
                "Dark",
                StringComparison.OrdinalIgnoreCase);

        var accent =
            settings.AccentColor switch
            {
                "Green" => "#527C61",
                "Yellow" => "#A97810",
                "Purple" => "#7154B8",
                "Pink" => "#B24F7A",
                _ => "#347FC4"
            };

        var background =
            (settings.AccentColor, isDark) switch
            {
                ("Green", true) => "#263D2E",
                ("Green", false) => "#E4F2E8",
                ("Yellow", true) => "#41371E",
                ("Yellow", false) => "#FFF3C9",
                ("Purple", true) => "#332A47",
                ("Purple", false) => "#EFE8FB",
                ("Pink", true) => "#452B37",
                ("Pink", false) => "#F9E5EE",
                ("Blue", true) => "#22364B",
                _ => "#E3EFFA"
            };

        var titleSize =
            settings.TextSize switch
            {
                "Compact" => 18,
                "Large" => 23,
                _ => 20
            };

        var bodySize =
            settings.TextSize switch
            {
                "Compact" => 12,
                "Large" => 16,
                _ => 14
            };

        var fontFamily =
            new FontFamily(
                settings.FontFamily);
        var primaryText =
            new SolidColorBrush(
                Color.Parse(
                    isDark ? "#F4F7FA" : "#17212B"));
        var secondaryText =
            new SolidColorBrush(
                Color.Parse(
                    isDark ? "#CFD8E0" : "#50606E"));

        if (_previewBorder is not null)
        {
            _previewBorder.Background =
                new SolidColorBrush(
                    Color.Parse(background));
            _previewBorder.BorderBrush =
                new SolidColorBrush(
                    Color.Parse(accent));
        }

        if (_previewTitleTextBlock is not null)
        {
            _previewTitleTextBlock.FontFamily = fontFamily;
            _previewTitleTextBlock.FontSize = titleSize;
            _previewTitleTextBlock.FontWeight =
                settings.UseSemiBoldText
                    ? FontWeight.Bold
                    : FontWeight.SemiBold;
            _previewTitleTextBlock.Foreground = primaryText;
        }

        if (_previewBodyTextBlock is not null)
        {
            _previewBodyTextBlock.FontFamily = fontFamily;
            _previewBodyTextBlock.FontSize = bodySize;
            _previewBodyTextBlock.FontWeight =
                settings.UseSemiBoldText
                    ? FontWeight.SemiBold
                    : FontWeight.Normal;
            _previewBodyTextBlock.Foreground = secondaryText;
        }
    }

    private void RefreshComboBoxTextColors(
        ApplicationAppearanceSettings settings)
    {
        var foreground = new SolidColorBrush(
            Color.Parse(
                string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase)
                    ? "#F2F7F3"
                    : "#17221B"));

        foreach (var comboBox in new[]
                 {
                     _themeComboBox,
                     _textSizeComboBox,
                     _fontFamilyComboBox,
                     _fontWeightComboBox,
                     _accentColorComboBox,
                     _languageComboBox
                 })
        {
            if (comboBox is null)
            {
                continue;
            }

            comboBox.Foreground = foreground;

            // Zaznaczona pozycja jest przez Avalonię prezentowana jako
            // osobne drzewo wizualne, niezależne od Content ComboBoxItem.
            // Aktualizujemy więc również faktycznie renderowane etykiety.
            foreach (var textBlock in comboBox
                         .GetVisualDescendants()
                         .OfType<TextBlock>())
            {
                textBlock.Foreground = foreground;
            }

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                item.Foreground = foreground;

                if (item.Content is Control content)
                {
                    SetTextForeground(content, foreground);
                }
            }
        }
    }

    private void RecreateComboBoxSelectionPresenters()
    {
        var wasInitializing =
            _isInitializing;

        _isInitializing =
            true;

        try
        {
            foreach (var comboBox in new[]
                     {
                         _languageComboBox,
                         _themeComboBox,
                         _textSizeComboBox,
                         _fontFamilyComboBox,
                         _fontWeightComboBox,
                         _accentColorComboBox
                     })
            {
                if (comboBox is null ||
                    comboBox.SelectedIndex < 0)
                {
                    continue;
                }

                var selectedIndex =
                    comboBox.SelectedIndex;

                comboBox.SelectedIndex =
                    -1;
                comboBox.SelectedIndex =
                    selectedIndex;
            }
        }
        finally
        {
            _isInitializing =
                wasInitializing;
        }
    }

    private static void SetTextForeground(
        Control control,
        IBrush foreground)
    {
        if (control is TextBlock textBlock)
        {
            textBlock.Foreground = foreground;
        }

        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                SetTextForeground(child, foreground);
            }
        }
        else if (control is Decorator { Child: Control child })
        {
            SetTextForeground(child, foreground);
        }
    }

    private void RestoreSavedAppearance()
    {
        ApplicationAppearanceService.ApplyPreview(
            ApplicationAppearanceService.Current);
        LocalizationService.Apply(
            _savedLanguage);
    }

    private void LanguageComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        LocalizationService.Apply(
            GetSelectedTag(_languageComboBox, LocalizationService.English));
    }

    private static string GetSelectedTag(
        ComboBox? comboBox,
        string fallback) =>
        (comboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString()
        ?? fallback;

    private static void SelectByTag(
        ComboBox? comboBox,
        string tag)
    {
        if (comboBox is null)
        {
            return;
        }

        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(
                    comboBoxItem.Tag?.ToString(),
                    tag,
                    StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem =
                    comboBoxItem;
                return;
            }
        }
    }
}
