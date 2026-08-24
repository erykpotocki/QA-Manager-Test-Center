using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class SharedStorageHost
{
    private const long MaximumRequestSize = 4 * 1024 * 1024;
    private static WebApplication? _application;

    public static async Task StartIfConfiguredAsync()
    {
        var options = await NetworkSyncConfiguration.LoadAsync();
        if (!string.Equals(options.Mode, NetworkSyncModes.Host, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ValidateHostOptions(options);
        var certificate = X509CertificateLoader.LoadPkcs12(
            await File.ReadAllBytesAsync(options.CertificatePath),
            options.CertificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(server =>
        {
            server.Limits.MaxRequestBodySize = MaximumRequestSize;
            server.AddServerHeader = false;
            server.Listen(
                IPAddress.Parse(options.ListenAddress),
                options.Port,
                listen => listen.UseHttps(certificate));
            server.Listen(
                IPAddress.Loopback,
                options.Port,
                listen => listen.UseHttps(certificate));
        });

        builder.Services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 180,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 8,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });

        var repository = new HostDocumentRepository();
        var app = builder.Build();
        app.UseRateLimiter();
        app.Use(async (context, next) =>
        {
            if (!IsAuthorized(context, options.ApiToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            await next();
        });

        app.MapGet("/api/health", () => Results.Ok(new { status = "online" }));

        app.MapGet("/api/documents/{documentName}", async (
            string documentName,
            HttpContext context) =>
        {
            if (!repository.IsAllowed(documentName))
            {
                return Results.NotFound();
            }

            var document = await repository.ReadAsync(documentName);
            if (document is null)
            {
                return Results.NotFound();
            }

            context.Response.Headers.ETag = $"\"{document.Version}\"";
            return Results.Text(document.Json, "application/json", Encoding.UTF8);
        });

        app.MapPut("/api/documents/{documentName}", async (
            string documentName,
            HttpContext context) =>
        {
            if (!repository.IsAllowed(documentName))
            {
                return Results.NotFound();
            }

            var expectedVersion = context.Request.Headers.IfMatch
                .ToString()
                .Trim()
                .Trim('"');
            if (string.IsNullOrWhiteSpace(expectedVersion))
            {
                return Results.StatusCode(StatusCodes.Status428PreconditionRequired);
            }

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var json = await reader.ReadToEndAsync();
            if (Encoding.UTF8.GetByteCount(json) > MaximumRequestSize || !IsValidJson(json))
            {
                return Results.BadRequest();
            }

            var newVersion = await repository.TryWriteAsync(
                documentName,
                expectedVersion,
                json);
            if (newVersion is null)
            {
                return Results.Conflict();
            }

            context.Response.Headers.ETag = $"\"{newVersion}\"";
            return Results.NoContent();
        });

        _application = app;
        await app.StartAsync();
    }

    public static async Task StopAsync()
    {
        var application =
            _application;

        _application = null;

        if (application is null)
        {
            return;
        }

        using var cancellation =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(4));

        try
        {
            await application.StopAsync(
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Zamknięcie aplikacji nie może pozostać zablokowane
            // przez aktywne połączenie klienta synchronizacji.
        }
        finally
        {
            try
            {
                await application.DisposeAsync()
                    .AsTask()
                    .WaitAsync(
                        TimeSpan.FromSeconds(4));
            }
            catch (TimeoutException)
            {
                // Proces kończy pracę, więc nie czekamy bez końca
                // na zwolnienie zasobów serwera.
            }
        }
    }

    private static bool IsAuthorized(HttpContext context, string expectedToken)
    {
        var header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(header[prefix.Length..]),
            Encoding.UTF8.GetBytes(expectedToken));
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    MaxDepth = 64,
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow
                });
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidateHostOptions(NetworkSyncOptions options)
    {
        if (!IPAddress.TryParse(options.ListenAddress, out _) ||
            options.Port is < 1024 or > 65535 ||
            string.IsNullOrWhiteSpace(options.ApiToken) ||
            !File.Exists(options.CertificatePath))
        {
            throw new InvalidOperationException(
                "Konfiguracja hosta synchronizacji jest niepełna lub nieprawidłowa.");
        }
    }

    private sealed class HostDocumentRepository
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _paths =
            new(StringComparer.OrdinalIgnoreCase);

        public HostDocumentRepository()
        {
            var localAppData =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _paths[SharedDocumentStore.TestCasesDocument] =
                Path.Combine(localAppData, "QATestCenter", "TestCases.json");
            _paths[SharedDocumentStore.AssignmentsDocument] =
                Path.Combine(localAppData, "QARegressionManager", "assignments.json");
            _paths[SharedDocumentStore.ProfilesDocument] =
                Path.Combine(localAppData, "QARegressionManager", "profiles.json");
        }

        public bool IsAllowed(string documentName) => _paths.ContainsKey(documentName);

        public async Task<StoredDocument?> ReadAsync(string documentName)
        {
            var path = _paths[documentName];
            var gate = _locks.GetOrAdd(documentName, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(path);
                return new StoredDocument(json, ComputeVersion(json));
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<string?> TryWriteAsync(
            string documentName,
            string expectedVersion,
            string json)
        {
            var path = _paths[documentName];
            var gate = _locks.GetOrAdd(documentName, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                var currentVersion = File.Exists(path)
                    ? ComputeVersion(await File.ReadAllTextAsync(path))
                    : "missing";
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.ASCII.GetBytes(currentVersion),
                        Encoding.ASCII.GetBytes(expectedVersion)))
                {
                    return null;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var temporaryPath = path + ".network.tmp";
                await File.WriteAllTextAsync(temporaryPath, json);
                File.Move(temporaryPath, path, true);
                return ComputeVersion(json);
            }
            finally
            {
                gate.Release();
            }
        }

        private static string ComputeVersion(string json) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

        public sealed record StoredDocument(string Json, string Version);
    }
}
