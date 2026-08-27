using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace QARegressionManager.Services;

public sealed class ApplicationAppearanceSettings
{
    public string Theme { get; set; } = "Light";
    public string AccentColor { get; set; } = "Blue";
    public string FontFamily { get; set; } = "Calibri";
    public string TextSize { get; set; } = "Standard";
    public bool UseSemiBoldText { get; set; }
}

public static class ApplicationAppearanceService
{
    private static readonly double[] SupportedFontSizes =
    [
        9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 24, 25, 26, 27, 28, 30, 31, 32, 34,
        36, 38, 40, 42, 44, 48
    ];

    private static readonly string SettingsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "QARegressionManager");

    private static readonly string SettingsPath =
        Path.Combine(
            SettingsDirectory,
            "application-settings.json");

    private static string? _activeProfileLogin;

    public static ApplicationAppearanceSettings Current { get; private set; } =
        new();

    public static IReadOnlyList<string> AvailableFonts { get; } =
        [
            "Inter",
            "Segoe UI",
            "Arial",
            "Calibri",
            "Cambria",
            "Georgia",
            "Comic Sans MS",
            "Tahoma",
            "Trebuchet MS",
            "Verdana"
        ];

    public static IReadOnlyList<string> AvailableAccentColors { get; } =
        ["Blue", "Green", "Yellow", "Purple", "Pink"];

    public static void LoadAndApply()
    {
        _activeProfileLogin = null;
        LoadAndApplyFromPath(
            SettingsPath);
    }

    public static void LoadAndApplyForProfile(
        string login)
    {
        _activeProfileLogin =
            NormalizeLogin(login);

        var profilePath =
            GetProfileSettingsPath(
                _activeProfileLogin);

        if (File.Exists(profilePath))
        {
            LoadAndApplyFromPath(
                profilePath);
            return;
        }

        // Przy pierwszym użyciu konto dziedziczy aktualny wygląd lokalny.
        Normalize(
            Current);
        Apply(
            Current);
    }

    public static void LoadAndApplyLocal()
    {
        _activeProfileLogin = null;
        LoadAndApplyFromPath(
            SettingsPath);
    }

    private static void LoadAndApplyFromPath(
        string settingsPath)
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                Current =
                    JsonSerializer.Deserialize<ApplicationAppearanceSettings>(
                        File.ReadAllText(settingsPath))
                    ?? new ApplicationAppearanceSettings();
            }
            else
            {
                Current =
                    new ApplicationAppearanceSettings();
            }
        }
        catch
        {
            Current =
                new ApplicationAppearanceSettings();
        }

        Normalize(
            Current);

        Apply(
            Current);
    }

    public static void SaveAndApply(
        ApplicationAppearanceSettings settings)
    {
        Normalize(
            settings);

        Directory.CreateDirectory(
            SettingsDirectory);

        var temporaryPath =
            GetCurrentSettingsPath() + ".tmp";

        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        File.Move(
            temporaryPath,
            GetCurrentSettingsPath(),
            true);

        Current =
            settings;

        Apply(
            settings);
    }

    public static void ResetAllProfilesToTestDefaults()
    {
        var defaults = new ApplicationAppearanceSettings
        {
            Theme = "Light",
            AccentColor = "Blue",
            FontFamily = "Calibri",
            TextSize = "Standard",
            UseSemiBoldText = false
        };

        Normalize(defaults);
        Directory.CreateDirectory(SettingsDirectory);

        var json = JsonSerializer.Serialize(
            defaults,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(SettingsPath, json);
        foreach (var profilePath in Directory.EnumerateFiles(
                     SettingsDirectory,
                     "application-settings-*.json"))
        {
            File.WriteAllText(profilePath, json);
        }

        Current = defaults;
        Apply(defaults);
    }

    public static void ApplyPreview(
        ApplicationAppearanceSettings settings)
    {
        Normalize(
            settings);

        Apply(
            settings);
    }

    public static double ScaleFontSize(
        double baseSize) =>
        baseSize *
        GetTextScale(
            Current.TextSize);

    private static string GetCurrentSettingsPath() =>
        _activeProfileLogin is null
            ? SettingsPath
            : GetProfileSettingsPath(
                _activeProfileLogin);

    private static string GetProfileSettingsPath(
        string login)
    {
        var hash =
            Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(login)))
                .ToLowerInvariant();

        return Path.Combine(
            SettingsDirectory,
            $"application-settings-{hash}.json");
    }

    private static string NormalizeLogin(
        string login) =>
        login.Trim().ToLowerInvariant();

    private static void Apply(
        ApplicationAppearanceSettings settings)
    {
        var application =
            Application.Current;

        if (application is null)
        {
            return;
        }

        var themeVariant =
            string.Equals(
                settings.Theme,
                "Dark",
                StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

        application.RequestedThemeVariant =
            themeVariant;

        // Avalonia nie zawsze propaguje zmianę wariantu do już otwartych
        // okien. Ustawienie wariantu bezpośrednio usuwa konieczność
        // zapisywania ustawień i ponownego otwierania okna.
        if (application.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.RequestedThemeVariant =
                    themeVariant;

                // Te właściwości są dziedziczone przez zawartość okna. Ich
                // bezpośrednia aktualizacja gwarantuje natychmiastowy podgląd
                // także w kontrolkach, których presenter został już utworzony.
                window.FontFamily =
                    new FontFamily(
                        settings.FontFamily);
                window.FontSize =
                    14 *
                    GetTextScale(
                        settings.TextSize);
                window.FontWeight =
                    settings.UseSemiBoldText
                        ? FontWeight.SemiBold
                        : FontWeight.Normal;
            }
        }

        ApplyAccentPalette(
            application,
            settings.AccentColor,
            string.Equals(
                settings.Theme,
                "Dark",
                StringComparison.OrdinalIgnoreCase));

        application.Resources["AppFontFamily"] =
            new FontFamily(
                settings.FontFamily);

        application.Resources["AppBaseFontSize"] =
            14 *
            GetTextScale(
                settings.TextSize);

        application.Resources["AppFontWeight"] =
            settings.UseSemiBoldText
                ? FontWeight.SemiBold
                : FontWeight.Normal;

        var scale =
            GetTextScale(
                settings.TextSize);

        foreach (var fontSize in SupportedFontSizes)
        {
            application.Resources[$"AppFontSize{fontSize:0}"] =
                fontSize * scale;
        }
    }

    private static double GetTextScale(
        string textSize) =>
        textSize switch
        {
            "Compact" => 0.94,
            "Large" => 1.08,
            _ => 1.0
        };

    private static void ApplyAccentPalette(
        Application application,
        string accentColor,
        bool isDark)
    {
        var palette =
            GetAccentPalette(
                accentColor,
                isDark);

        var primary = Color.Parse(palette.Primary);
        var appBackground = Color.Parse(palette.AppBackground);
        var cardBackground = isDark
            ? Blend(appBackground, primary, 0.07)
            : Color.Parse("#FFFFFF");
        var inputBackground = isDark
            ? Blend(appBackground, primary, 0.11)
            : Color.Parse("#FFFFFF");
        var inputBorder = Blend(appBackground, primary, isDark ? 0.30 : 0.18);
        var cardBorder = Blend(appBackground, primary, isDark ? 0.21 : 0.12);
        var themeButtonBackground = isDark
            ? Blend(appBackground, primary, 0.12)
            : Color.Parse("#FFFFFF");

        // ComboBox przechowuje presenter zaznaczonej pozycji również po
        // zmianie wariantu motywu. Taki presenter może nadal wskazywać pędzel
        // z poprzedniego słownika (Light albo Dark). Aktualizacja obu
        // słowników sprawia, że istniejące i nowe presentery zawsze dostają
        // właściwy kontrast, także po wielu zmianach Light <-> Dark.
        SetBrushAcrossThemes(
            application,
            "PrimaryTextBrush",
            Color.Parse(isDark ? "#F2F7F3" : "#17221B"));
        SetBrushAcrossThemes(
            application,
            "SecondaryTextBrush",
            Color.Parse(isDark ? "#B4C0B8" : "#68726B"));
        SetBrushAcrossThemes(
            application,
            "FooterTextBrush",
            Color.Parse(isDark ? "#95A29A" : "#89928C"));
        SetBrushAcrossThemes(
            application,
            "FooterSignatureBrush",
            Color.Parse(isDark ? "#76827B" : "#A0A8A3"));

        SetBrush(application, isDark, "AccentPrimaryBrush", palette.Primary);
        SetBrush(application, isDark, "AccentPrimaryHoverBrush", palette.Hover);
        SetBrush(application, isDark, "AccentSoftBrush", palette.Soft);
        SetBrush(application, isDark, "AccentSelectionBrush", palette.Selection);
        SetBrush(application, isDark, "AccentMutedBrush", palette.Muted);
        SetBrush(application, isDark, "InputHoverBorderBrush", palette.Muted);
        SetBrush(application, isDark, "InputFocusBorderBrush", palette.Primary);
        SetBrush(application, isDark, "PanelSplitterBrush", palette.Splitter);
        SetBrush(application, isDark, "PanelToggleAccentBrush", palette.Muted);
        SetBrush(application, isDark, "PanelToggleBrush", palette.Hover);
        SetBrush(application, isDark, "SelectionBrush", palette.Selection);
        SetBrush(application, isDark, "SelectedItemBrush", palette.Selection);
        SetBrush(application, isDark, "HoverItemBrush", palette.Soft);
        SetBrush(application, isDark, "IconBackgroundBrush", palette.Soft);
        SetBrush(application, isDark, "DecorationOneBrush", palette.Soft);
        SetBrush(application, isDark, "DecorationTwoBrush", palette.Selection);
        SetBrush(application, isDark, "ThemeButtonBackgroundBrush", themeButtonBackground);
        SetBrush(application, isDark, "ThemeButtonHoverBrush", palette.Soft);
        SetBrush(application, isDark, "ThemeButtonPressedBrush", palette.Selection);
        SetBrush(application, isDark, "AppBackgroundBrush", appBackground);
        SetBrush(application, isDark, "CardBackgroundBrush", cardBackground);
        SetBrush(application, isDark, "InputBackgroundBrush", inputBackground);
        SetBrush(application, isDark, "InputBorderBrush", inputBorder);
        SetBrush(application, isDark, "CardBorderBrush", cardBorder);

        ApplyNativeWindowAccent(
            application,
            primary);

        // DWM potrafi ponownie nadać kolor systemowy podczas przebudowy lub
        // maksymalizowania okna. Utrwalamy akcent po zakończeniu bieżącego
        // przebiegu renderowania.
        Dispatcher.UIThread.Post(
            () =>
                ApplyNativeWindowAccent(
                    application,
                    primary),
            DispatcherPriority.Loaded);
    }

    public static void RefreshNativeWindowAccent(
        Window window)
    {
        var palette =
            GetAccentPalette(
                Current.AccentColor,
                string.Equals(
                    Current.Theme,
                    "Dark",
                    StringComparison.OrdinalIgnoreCase));

        ApplyNativeWindowAccent(
            window,
            Color.Parse(palette.Primary));
    }

    private static void ApplyNativeWindowAccent(
        Application application,
        Color accent)
    {
        if (!OperatingSystem.IsWindows() ||
            application.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // Natywny pasek tytułu Windows nie korzysta z zasobów Avalonia.
        // Bez osobnej aktualizacji zachowuje systemowy (na tym komputerze
        // zielony) kolor nawet po zmianie akcentu całej aplikacji.
        foreach (var window in desktop.Windows)
        {
            ApplyNativeWindowAccent(
                window,
                accent);
        }
    }

    private static void ApplyNativeWindowAccent(
        Window window,
        Color accent)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle =
            window.TryGetPlatformHandle()?.Handle ??
            IntPtr.Zero;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        var captionColor =
            ToColorRef(accent);
        var textColor =
            ToColorRef(
                IsLight(accent)
                    ? Color.Parse("#17221B")
                    : Color.Parse("#FFFFFF"));

        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeBorderColor,
            ref captionColor,
            sizeof(uint));
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeCaptionColor,
            ref captionColor,
            sizeof(uint));
        _ = DwmSetWindowAttribute(
            handle,
            DwmWindowAttributeTextColor,
            ref textColor,
            sizeof(uint));
    }

    private static AccentPalette GetAccentPalette(
        string accentColor,
        bool isDark) =>
        (accentColor, isDark) switch
        {
            ("Green", false) => new AccentPalette("#527C61", "#3E654C", "#E4F2E8", "#CDE6D4", "#73A281", "#F3F8F4", "#D9E8DE"),
            ("Green", true) => new AccentPalette("#6AA77D", "#82BE92", "#263D2E", "#31533C", "#78B189", "#151A17", "#2C4434"),
            ("Yellow", false) => new AccentPalette("#A97810", "#895F09", "#FFF3C9", "#F4DEA0", "#C39735", "#FAF8F1", "#EBDFC0"),
            ("Yellow", true) => new AccentPalette("#D2A83F", "#E2BD5B", "#41371E", "#554723", "#DDB653", "#1B1913", "#4B4024"),
            ("Purple", false) => new AccentPalette("#7154B8", "#5B409E", "#EFE8FB", "#DDD0F2", "#8D73C8", "#F7F5FA", "#DED8E8"),
            ("Purple", true) => new AccentPalette("#9275D3", "#A68BE0", "#332A47", "#44365F", "#A087D6", "#18161D", "#403651"),
            ("Pink", false) => new AccentPalette("#B24F7A", "#953B64", "#F9E5EE", "#EDC9DA", "#C16E91", "#FAF5F7", "#E8D7DF"),
            ("Pink", true) => new AccentPalette("#D16F9A", "#DF88AC", "#452B37", "#5A3546", "#D581A5", "#1C1619", "#4E3340"),
            ("Blue", true) => new AccentPalette("#4D91D8", "#68A5E3", "#22364B", "#294A68", "#72A9DE", "#14191E", "#283E52"),
            _ => new AccentPalette("#347FC4", "#2869A5", "#E3EFFA", "#C9DFF2", "#659CD0", "#F3F6F9", "#D6E2EC")
        };

    private static uint ToColorRef(
        Color color) =>
        (uint)(color.R |
               color.G << 8 |
               color.B << 16);

    private static bool IsLight(
        Color color) =>
        (0.2126 * color.R +
         0.7152 * color.G +
         0.0722 * color.B) >= 150;

    private const int DwmWindowAttributeBorderColor = 34;
    private const int DwmWindowAttributeCaptionColor = 35;
    private const int DwmWindowAttributeTextColor = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref uint attributeValue,
        int attributeSize);

    private static void SetBrush(
        Application application,
        bool isDark,
        string resourceKey,
        string color) =>
        SetBrush(
            application,
            isDark,
            resourceKey,
            Color.Parse(color));

    private static void SetBrush(
        Application application,
        bool isDark,
        string resourceKey,
        Color color)
    {
        UpdateBrush(
            application.Resources,
            resourceKey,
            color);

        var themeVariant =
            isDark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

        if (application.Resources.ThemeDictionaries.TryGetValue(
                themeVariant,
                out var themeResources) &&
            themeResources is IResourceDictionary themeDictionary)
        {
            UpdateBrush(
                themeDictionary,
                resourceKey,
                color);
        }
    }

    private static void SetBrushAcrossThemes(
        Application application,
        string resourceKey,
        Color color)
    {
        UpdateBrush(
            application.Resources,
            resourceKey,
            color);

        foreach (var themeVariant in new[]
                 {
                     ThemeVariant.Light,
                     ThemeVariant.Dark
                 })
        {
            if (application.Resources.ThemeDictionaries.TryGetValue(
                    themeVariant,
                    out var themeResources) &&
                themeResources is IResourceDictionary themeDictionary)
            {
                UpdateBrush(
                    themeDictionary,
                    resourceKey,
                    color);
            }
        }
    }

    private static void UpdateBrush(
        IResourceDictionary resources,
        string resourceKey,
        Color color)
    {
        if (resources.TryGetResource(
                resourceKey,
                null,
                out var resource) &&
            resource is SolidColorBrush brush)
        {
            // Mutacja istniejącego pędzla natychmiast odświeża kontrolki,
            // które już pobrały DynamicResource z aktywnego motywu.
            brush.Color = color;
            return;
        }

        resources[resourceKey] =
            new SolidColorBrush(color);
    }

    private static Color Blend(
        Color background,
        Color accent,
        double accentShare)
    {
        static byte Mix(
            byte backgroundChannel,
            byte accentChannel,
            double share) =>
            (byte)Math.Round(
                backgroundChannel * (1 - share) +
                accentChannel * share);

        return Color.FromRgb(
            Mix(background.R, accent.R, accentShare),
            Mix(background.G, accent.G, accentShare),
            Mix(background.B, accent.B, accentShare));
    }

    private static void Normalize(
        ApplicationAppearanceSettings settings)
    {
        if (!string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            settings.Theme = "Light";
        }

        if (!AvailableFonts.Contains(
                settings.FontFamily,
                StringComparer.OrdinalIgnoreCase))
        {
            settings.FontFamily = "Calibri";
        }

        if (!AvailableAccentColors.Contains(
                settings.AccentColor,
                StringComparer.OrdinalIgnoreCase))
        {
            settings.AccentColor = "Blue";
        }

        settings.TextSize =
            settings.TextSize switch
            {
                "Compact" => "Compact",
                "Large" => "Large",
                _ => "Standard"
            };
    }

    private sealed record AccentPalette(
        string Primary,
        string Hover,
        string Soft,
        string Selection,
        string Muted,
        string AppBackground,
        string Splitter);
}
