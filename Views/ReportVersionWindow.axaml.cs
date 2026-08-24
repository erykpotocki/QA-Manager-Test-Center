using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace QARegressionManager.Views;

public partial class ReportVersionWindow : Window
{
    public ReportVersionWindow()
        : this(null, null)
    {
    }

    public ReportVersionWindow(
        string? assignedVersion,
        string? defaultFileName)
    {
        InitializeComponent();

        var defaultDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "QA Manager",
                "Raporty");

        var rememberedDirectory =
            LoadLastReportDirectory();

        DestinationPathTextBox.Text =
            !string.IsNullOrWhiteSpace(
                rememberedDirectory) &&
            Directory.Exists(
                rememberedDirectory)
                ? rememberedDirectory
                : defaultDirectory;

        FileNameTextBox.Text =
            defaultFileName
            ?? "RAPORT";

        if (!string.IsNullOrWhiteSpace(
                assignedVersion))
        {
            VersionTextBox.Text =
                assignedVersion.Trim();

            VersionTextBox.IsReadOnly =
                true;

            DescriptionTextBlock.Text =
                "Wersja pochodzi z przypisanej sesji. Sprawdź nazwę i miejsce zapisu raportu.";
        }
    }

    private async void BrowseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var folders =
            await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Wybierz katalog raportów",

                    AllowMultiple =
                        false
                });

        if (folders.Count == 0)
        {
            return;
        }

        DestinationPathTextBox.Text =
            folders[0].Path.LocalPath;

        SaveLastReportDirectory(
            folders[0].Path.LocalPath);

        ErrorTextBlock.IsVisible =
            false;
    }

    private void FileNameTextBox_OnGotFocus(
        object? sender,
        RoutedEventArgs e)
    {
        FileNameTextBox.SelectAll();
    }

    private void GenerateButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        GenerateAndClose();
    }

    private void GenerateAndClose()
    {
        var fileName =
            FileNameTextBox.Text
                ?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            ShowError(
                "Wpisz nazwę pliku.",
                FileNameTextBox);

            return;
        }

        if (fileName.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowError(
                "Nazwa pliku zawiera niedozwolone znaki.",
                FileNameTextBox,
                selectAll: true);

            return;
        }

        var enteredExtension =
            Path.GetExtension(
                fileName);

        if (new[]
            {
                ".pdf",
                ".xlsx",
                ".json"
            }.Contains(
                enteredExtension,
                StringComparer.OrdinalIgnoreCase))
        {
            fileName =
                Path.GetFileNameWithoutExtension(
                    fileName);
        }

        var version =
            VersionTextBox.Text
                ?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                version))
        {
            ShowError(
                "Wpisz numer wersji.",
                VersionTextBox);

            return;
        }

        var directoryPath =
            DestinationPathTextBox.Text
                ?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                directoryPath))
        {
            ShowError(
                "Wybierz miejsce zapisu raportu.");

            return;
        }

        try
        {
            Directory.CreateDirectory(
                directoryPath);
        }
        catch
        {
            ShowError(
                "Nie można użyć wybranego katalogu. Wybierz inne miejsce.");

            return;
        }

        SaveLastReportDirectory(
            directoryPath);

        Close(
            new ReportExportRequest(
                version,
                directoryPath,
                fileName,
                ReportFormatComboBox.SelectedIndex switch
                {
                    1 =>
                        TestReportFormat.Excel,

                    2 =>
                        TestReportFormat.Json,

                    _ =>
                        TestReportFormat.Pdf
                },
                ReportScopeComboBox.SelectedIndex != 1));
    }

    private void ShowError(
        string message,
        TextBox? target = null,
        bool selectAll = false)
    {
        ErrorTextBlock.Text =
            message;

        ErrorTextBlock.IsVisible =
            true;

        if (target is null)
        {
            return;
        }

        target.Focus();

        if (selectAll)
        {
            target.SelectAll();
        }
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            null);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(
            e);

        if (e.Key == Key.Enter)
        {
            GenerateAndClose();

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Escape)
        {
            Close(
                null);

            e.Handled =
                true;
        }
    }

    private static string? LoadLastReportDirectory()
    {
        try
        {
            var settingsPath =
                GetSettingsPath();

            if (!File.Exists(
                    settingsPath))
            {
                return null;
            }

            var settings =
                JsonSerializer.Deserialize<ReportWindowSettings>(
                    File.ReadAllText(
                        settingsPath));

            return settings?.LastReportDirectory;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLastReportDirectory(
        string directoryPath)
    {
        try
        {
            var settingsPath =
                GetSettingsPath();

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    settingsPath)!);

            File.WriteAllText(
                settingsPath,
                JsonSerializer.Serialize(
                    new ReportWindowSettings
                    {
                        LastReportDirectory =
                            directoryPath
                    },
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true
                    }));
        }
        catch
        {
            // Preferencja jest pomocnicza i nie może blokować raportu.
        }
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "QAManager",
            "ReportWindowSettings.json");
    }
}

public sealed record ReportExportRequest(
    string ApplicationVersion,
    string DirectoryPath,
    string FileNameBase,
    TestReportFormat Format,
    bool IncludeUnfinished);

public sealed class ReportWindowSettings
{
    public string LastReportDirectory { get; set; } =
        string.Empty;
}

public enum TestReportFormat
{
    Pdf,
    Excel,
    Json
}
