using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class SharedDocumentStore
{
    public const string TestCasesDocument = "test-cases";
    public const string AssignmentsDocument = "assignments";
    public const string ProfilesDocument = "profiles";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 64
    };

    private static readonly AsyncLocal<Dictionary<string, string>?> Versions = new();
    private static readonly SemaphoreSlim ClientInitializationLock = new(1, 1);
    private static HttpClient? _client;
    private static NetworkSyncOptions? _options;

    public static async Task<T> LoadAsync<T>(string documentName, string localPath)
        where T : new()
    {
        var options = await GetOptionsAsync();
        if (!IsClient(options))
        {
            return await LoadLocalAsync<T>(localPath);
        }

        var client = await GetClientAsync(options);
        using var response = await client.GetAsync(
            $"api/documents/{Uri.EscapeDataString(documentName)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            SetVersion(documentName, "missing");
            return new T();
        }

        response.EnsureSuccessStatusCode();
        SetVersion(documentName, response.Headers.ETag?.Tag?.Trim('"') ?? "missing");
        return JsonSerializer.Deserialize<T>(
                   await response.Content.ReadAsStringAsync(),
                   JsonOptions)
               ?? new T();
    }

    public static async Task SaveAsync<T>(string documentName, string localPath, T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var options = await GetOptionsAsync();
        if (!IsClient(options))
        {
            await SaveLocalAsync(localPath, data);
            return;
        }

        var client = await GetClientAsync(options);
        var version = GetVersion(documentName) ?? "missing";
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/documents/{Uri.EscapeDataString(documentName)}");
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        request.Content = new StringContent(
            JsonSerializer.Serialize(data, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "Dane zostały równocześnie zmienione na innym komputerze. Odśwież widok i ponów operację.");
        }

        response.EnsureSuccessStatusCode();
        SetVersion(documentName, response.Headers.ETag?.Tag?.Trim('"') ?? version);
    }

    public static async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var options = await GetOptionsAsync();
            if (IsLocal(options))
            {
                return false;
            }

            using var response = await (await GetClientAsync(options)).GetAsync("api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> UsesNetworkAsync() =>
        IsClient(await GetOptionsAsync());

    public static void ResetConfigurationCache()
    {
        _client?.Dispose();
        _client = null;
        _options = null;
        Versions.Value = null;
    }

    private static bool IsLocal(NetworkSyncOptions options) =>
        string.Equals(options.Mode, NetworkSyncModes.Local, StringComparison.OrdinalIgnoreCase);

    private static bool IsClient(NetworkSyncOptions options) =>
        string.Equals(options.Mode, NetworkSyncModes.Client, StringComparison.OrdinalIgnoreCase);

    private static string? GetVersion(string documentName) =>
        Versions.Value is not null &&
        Versions.Value.TryGetValue(documentName, out var version)
            ? version
            : null;

    private static void SetVersion(string documentName, string version)
    {
        Versions.Value ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Versions.Value[documentName] = version;
    }

    private static async Task<NetworkSyncOptions> GetOptionsAsync()
    {
        if (_options is not null)
        {
            return _options;
        }

        await ClientInitializationLock.WaitAsync();
        try
        {
            return _options ??= await NetworkSyncConfiguration.LoadAsync();
        }
        finally
        {
            ClientInitializationLock.Release();
        }
    }

    private static async Task<HttpClient> GetClientAsync(NetworkSyncOptions options)
    {
        if (_client is not null)
        {
            return _client;
        }

        await ClientInitializationLock.WaitAsync();
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var expectedThumbprint =
                NetworkSyncConfiguration.NormalizeThumbprint(options.CertificateThumbprint);
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    certificate is not null &&
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.ASCII.GetBytes(
                            NetworkSyncConfiguration.NormalizeThumbprint(
                                certificate.GetCertHashString())),
                        Encoding.ASCII.GetBytes(expectedThumbprint))
            };

            var baseUrl =
                string.Equals(options.Mode, NetworkSyncModes.Host, StringComparison.OrdinalIgnoreCase)
                    ? $"https://127.0.0.1:{options.Port}/"
                    : options.HostUrl.TrimEnd('/') + "/";

            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(8)
            };
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiToken);
            return _client;
        }
        finally
        {
            ClientInitializationLock.Release();
        }
    }

    private static async Task<T> LoadLocalAsync<T>(string path)
        where T : new()
    {
        if (!File.Exists(path))
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(
                       await File.ReadAllTextAsync(path),
                       JsonOptions)
                   ?? new T();
        }
        catch (JsonException)
        {
            return new T();
        }
    }

    private static async Task SaveLocalAsync<T>(string path, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temporaryPath, path, true);
    }
}
