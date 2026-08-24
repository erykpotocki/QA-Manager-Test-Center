using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class JsonStorageService
{
    private const string AppFolderName = "QATestCenter";
    private const string DataFileName = "TestCases.json";
    private const string BackupFileName = "TestCases.backup.json";

    private static readonly SemaphoreSlim StorageLock =
        new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string AppDataDirectory { get; }

    public string DataFilePath { get; }

    public string BackupFilePath { get; }

    public JsonStorageService()
    {
        var localAppDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        AppDataDirectory =
            Path.Combine(
                localAppDataPath,
                AppFolderName);

        DataFilePath =
            Path.Combine(
                AppDataDirectory,
                DataFileName);

        BackupFilePath =
            Path.Combine(
                AppDataDirectory,
                BackupFileName);
    }

    public async Task<UserTestDataModel> LoadAsync()
    {
        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            return await SharedDocumentStore.LoadAsync<UserTestDataModel>(
                SharedDocumentStore.TestCasesDocument,
                DataFilePath);
        }

        EnsureDirectoryExists();

        await StorageLock.WaitAsync();

        try
        {
            if (!File.Exists(DataFilePath))
            {
                return new UserTestDataModel();
            }

            try
            {
                var json =
                    await File.ReadAllTextAsync(
                        DataFilePath);

                var data =
                    JsonSerializer.Deserialize<UserTestDataModel>(
                        json,
                        _jsonOptions);

                return data ?? new UserTestDataModel();
            }
            catch
            {
                return await LoadBackupAsync();
            }
        }
        finally
        {
            StorageLock.Release();
        }
    }

    public async Task SaveAsync(
        UserTestDataModel data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            await SharedDocumentStore.SaveAsync(
                SharedDocumentStore.TestCasesDocument,
                DataFilePath,
                data);
            return;
        }

        EnsureDirectoryExists();

        await StorageLock.WaitAsync();

        try
        {
            if (File.Exists(DataFilePath))
            {
                File.Copy(
                    DataFilePath,
                    BackupFilePath,
                    overwrite: true);
            }

            var json =
                JsonSerializer.Serialize(
                    data,
                    _jsonOptions);

            var temporaryFilePath =
                DataFilePath + ".tmp";

            await File.WriteAllTextAsync(
                temporaryFilePath,
                json);

            File.Move(
                temporaryFilePath,
                DataFilePath,
                true);
        }
        finally
        {
            StorageLock.Release();
        }
    }

    private async Task<UserTestDataModel> LoadBackupAsync()
    {
        if (!File.Exists(BackupFilePath))
        {
            return new UserTestDataModel();
        }

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    BackupFilePath);

            var data =
                JsonSerializer.Deserialize<UserTestDataModel>(
                    json,
                    _jsonOptions);

            return data ?? new UserTestDataModel();
        }
        catch
        {
            return new UserTestDataModel();
        }
    }

    private void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(
            AppDataDirectory);
    }
}
