using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QARegressionManager.Services;

public sealed class AssignmentInputPresets
{
    public List<string> SessionNames { get; set; } = [];
    public List<string> Versions { get; set; } = [];
}

public static class AssignmentInputPresetService
{
    private const int MaximumValuesPerList = 30;

    private static readonly string SettingsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "QARegressionManager");

    private static readonly string SettingsPath =
        Path.Combine(
            SettingsDirectory,
            "assignment-input-presets.json");

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    public static AssignmentInputPresets Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AssignmentInputPresets();
            }

            var presets =
                JsonSerializer.Deserialize<AssignmentInputPresets>(
                    File.ReadAllText(SettingsPath),
                    JsonOptions)
                ?? new AssignmentInputPresets();

            presets.SessionNames =
                Normalize(presets.SessionNames);
            presets.Versions =
                Normalize(presets.Versions);

            return presets;
        }
        catch
        {
            return new AssignmentInputPresets();
        }
    }

    public static void Save(
        AssignmentInputPresets presets)
    {
        presets.SessionNames =
            Normalize(presets.SessionNames);
        presets.Versions =
            Normalize(presets.Versions);

        Directory.CreateDirectory(
            SettingsDirectory);

        var temporaryPath =
            SettingsPath + ".tmp";

        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(
                presets,
                JsonOptions));

        File.Move(
            temporaryPath,
            SettingsPath,
            true);
    }

    private static List<string> Normalize(
        IEnumerable<string>? values)
    {
        return (values ?? [])
            .Select(value => value?.Trim())
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumValuesPerList)
            .Cast<string>()
            .ToList();
    }
}
