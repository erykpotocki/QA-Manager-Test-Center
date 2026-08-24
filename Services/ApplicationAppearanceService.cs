using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;

namespace QARegressionManager.Services;

public sealed class ApplicationAppearanceSettings
{
    public string Theme { get; set; } = "Light";
    public string AccentColor { get; set; } = "Blue";
    public string FontFamily { get; set; } = "Arial";
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
            FontFamily = "Arial",
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

        SetBrush(application, "AccentPrimaryBrush", palette.Primary);
        SetBrush(application, "AccentPrimaryHoverBrush", palette.Hover);
        SetBrush(application, "AccentSoftBrush", palette.Soft);
        SetBrush(application, "AccentSelectionBrush", palette.Selection);
        SetBrush(application, "AccentMutedBrush", palette.Muted);
        SetBrush(application, "InputHoverBorderBrush", palette.Muted);
        SetBrush(application, "InputFocusBorderBrush", palette.Primary);
        SetBrush(application, "PanelSplitterBrush", palette.Splitter);
        SetBrush(application, "PanelToggleAccentBrush", palette.Muted);
        SetBrush(application, "PanelToggleBrush", isDark ? "#E3EBF2" : palette.Hover);
        SetBrush(application, "SelectionBrush", palette.Selection);
        SetBrush(application, "SelectedItemBrush", palette.Selection);
        SetBrush(application, "HoverItemBrush", palette.Soft);
        SetBrush(application, "IconBackgroundBrush", palette.Soft);
        SetBrush(application, "DecorationOneBrush", palette.Soft);
        SetBrush(application, "DecorationTwoBrush", palette.Selection);
        SetBrush(application, "ThemeButtonHoverBrush", palette.Soft);
        SetBrush(application, "ThemeButtonPressedBrush", palette.Selection);
        SetBrush(application, "AppBackgroundBrush", palette.AppBackground);
    }

    private static void SetBrush(
        Application application,
        string resourceKey,
        string color) =>
        application.Resources[resourceKey] =
            new SolidColorBrush(
                Color.Parse(color));

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
            settings.FontFamily = "Inter";
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
