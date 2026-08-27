using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
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
    private TextBlock? _previewPolishCharactersTextBlock;
    private Button? _saveButton;
    private TextBlock? _saveButtonText;
    private StackPanel? _savingIndicatorPanel;
    private TextBlock? _savingSpinner;
    private StackPanel? _savedConfirmationPanel;
    private StackPanel? _languageChangeIndicatorPanel;
    private TextBlock? _languageChangeSpinner;
    private readonly DispatcherTimer _languageChangeSpinnerTimer =
        new()
        {
            Interval = TimeSpan.FromMilliseconds(28)
        };
    private double _languageChangeSpinnerAngle;
    private int _appearanceChangeVersion;
    private bool _isInitializing = true;
    private bool _settingsSaved;
    private string _savedLanguage = LocalizationService.CurrentLanguage;

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
        _previewPolishCharactersTextBlock =
            this.FindControl<TextBlock>("PreviewPolishCharactersTextBlock");
        _saveButton =
            this.FindControl<Button>("SaveButton");
        _saveButtonText =
            this.FindControl<TextBlock>("SaveButtonText");
        _savingIndicatorPanel =
            this.FindControl<StackPanel>("SavingIndicatorPanel");
        _savingSpinner =
            this.FindControl<TextBlock>("SavingSpinner");
        _savedConfirmationPanel =
            this.FindControl<StackPanel>("SavedConfirmationPanel");
        _languageChangeIndicatorPanel =
            this.FindControl<StackPanel>("LanguageChangeIndicatorPanel");
        _languageChangeSpinner =
            this.FindControl<TextBlock>("LanguageChangeSpinner");

        _languageChangeSpinnerTimer.Tick +=
            (_, _) =>
            {
                _languageChangeSpinnerAngle =
                    (_languageChangeSpinnerAngle + 18) % 360;

                if (_languageChangeSpinner?.RenderTransform is RotateTransform transform)
                {
                    transform.Angle =
                        _languageChangeSpinnerAngle;
                }
            };

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
        UpdatePreview(
            GetSelectedSettings());
    }

    private async void SaveButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        SetSavingState(
            true);

        var spinnerAngle = 0d;
        var spinnerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(28)
        };
        spinnerTimer.Tick += (_, _) =>
        {
            spinnerAngle =
                (spinnerAngle + 18) % 360;

            if (_savingSpinner?.RenderTransform is RotateTransform transform)
            {
                transform.Angle =
                    spinnerAngle;
            }
        };
        spinnerTimer.Start();

        // Pozwala wyrenderować stan zapisywania przed operacją plikową i
        // utrzymuje go na tyle długo, aby potwierdzenie było zauważalne.
        await Task.Delay(80);

        ApplicationAppearanceService.SaveAndApply(
            GetSelectedSettings());
        LocalizationService.SaveAndApply(
            GetSelectedTag(_languageComboBox, LocalizationService.English));

        _savedLanguage =
            LocalizationService.CurrentLanguage;

        _settingsSaved = true;

        await Task.Delay(420);

        spinnerTimer.Stop();
        SetSavingState(
            false);

        if (_savedConfirmationPanel is not null)
        {
            _savedConfirmationPanel.IsVisible =
                true;
        }
    }

    private void SetSavingState(
        bool isSaving)
    {
        if (_saveButton is not null)
        {
            _saveButton.IsEnabled =
                !isSaving;
        }

        if (_saveButtonText is not null)
        {
            _saveButtonText.IsVisible =
                !isSaving;
        }

        if (_savingIndicatorPanel is not null)
        {
            _savingIndicatorPanel.IsVisible =
                isSaving;
        }
    }

    private void MarkSettingsAsChanged()
    {
        _settingsSaved = false;

        if (_savedConfirmationPanel is not null)
        {
            _savedConfirmationPanel.IsVisible =
                false;
        }
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
        _languageChangeSpinnerTimer.Stop();

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

    private async void AppearanceComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        MarkSettingsAsChanged();

        var changeVersion =
            ++_appearanceChangeVersion;

        ShowAppearanceChangeIndicator();

        // Najpierw pozwalamy interfejsowi narysować wskaźnik pracy. Zmiana
        // wariantu motywu przebudowuje zasoby wszystkich otwartych okien.
        await Task.Delay(55);

        if (changeVersion != _appearanceChangeVersion)
        {
            return;
        }

        var settings =
            GetSelectedSettings();

        ApplicationAppearanceService.ApplyPreview(
            settings);

        UpdatePreview(
            settings);

        await Task.Delay(180);

        if (changeVersion == _appearanceChangeVersion)
        {
            HideAppearanceChangeIndicator();
        }
    }

    private ApplicationAppearanceSettings GetSelectedSettings() =>
        new()
        {
            Theme = GetSelectedTag(_themeComboBox, "Light"),
            AccentColor = GetSelectedTag(_accentColorComboBox, "Blue"),
            TextSize = GetSelectedTag(_textSizeComboBox, "Standard"),
            FontFamily = GetSelectedTag(_fontFamilyComboBox, "Calibri"),
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

        if (_previewPolishCharactersTextBlock is not null)
        {
            _previewPolishCharactersTextBlock.FontFamily = fontFamily;
            _previewPolishCharactersTextBlock.FontSize = bodySize;
            _previewPolishCharactersTextBlock.FontWeight =
                settings.UseSemiBoldText
                    ? FontWeight.SemiBold
                    : FontWeight.Normal;
            _previewPolishCharactersTextBlock.Foreground = secondaryText;
        }
    }

    private void RestoreSavedAppearance()
    {
        ApplicationAppearanceService.ApplyPreview(
            ApplicationAppearanceService.Current);
        LocalizationService.Apply(
            _savedLanguage);
    }

    private async void LanguageComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        MarkSettingsAsChanged();

        var changeVersion =
            ++_appearanceChangeVersion;

        ShowAppearanceChangeIndicator();

        // Najpierw renderujemy informację o pracy, dopiero potem przebudowujemy
        // zasoby językowe i prezentery zaznaczonych pozycji.
        await Task.Delay(70);

        if (changeVersion != _appearanceChangeVersion)
        {
            return;
        }

        LocalizationService.Apply(
            GetSelectedTag(_languageComboBox, LocalizationService.English));

        RefreshLocalizedComboBoxPresenters();

        await Task.Delay(260);

        if (changeVersion != _appearanceChangeVersion)
        {
            return;
        }

        HideAppearanceChangeIndicator();
    }

    private void ShowAppearanceChangeIndicator()
    {
        if (_languageChangeIndicatorPanel is not null)
        {
            _languageChangeIndicatorPanel.IsVisible =
                true;
        }

        _languageChangeSpinnerTimer.Start();
    }

    private void HideAppearanceChangeIndicator()
    {
        _languageChangeSpinnerTimer.Stop();

        if (_languageChangeIndicatorPanel is not null)
        {
            _languageChangeIndicatorPanel.IsVisible =
                false;
        }
    }

    private void RefreshLocalizedComboBoxPresenters()
    {
        var wasInitializing =
            _isInitializing;

        _isInitializing = true;

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

                // Avalonia kopiuje zawartość zaznaczonego ComboBoxItem do
                // osobnego presentera. Ponowny wybór tworzy go z aktualnych
                // zasobów językowych, nie zmieniając faktycznej wartości pola.
                comboBox.SelectedIndex = -1;
                comboBox.SelectedIndex = selectedIndex;
            }
        }
        finally
        {
            _isInitializing =
                wasInitializing;
        }
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
