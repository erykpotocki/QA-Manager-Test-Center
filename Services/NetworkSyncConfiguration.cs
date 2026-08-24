using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class NetworkSyncConfiguration
{
    private const string SettingsFileName = "network-sync.json";
    private const string CertificateFileName = "network-sync-host.pfx";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QARegressionManager");

    public static string SettingsPath =>
        Path.Combine(SettingsDirectory, SettingsFileName);

    public static async Task<NetworkSyncOptions> LoadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            return new NetworkSyncOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<NetworkSyncOptions>(
                       await File.ReadAllTextAsync(SettingsPath),
                       JsonOptions)
                   ?? new NetworkSyncOptions();
        }
        catch
        {
            return new NetworkSyncOptions();
        }
    }

    public static async Task SaveAsync(NetworkSyncOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Directory.CreateDirectory(SettingsDirectory);

        var temporaryPath = SettingsPath + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(options, JsonOptions));
        File.Move(temporaryPath, SettingsPath, true);
    }

    public static async Task<string> ConfigureHostAsync()
    {
        var address = FindLanAddress()
                      ?? throw new InvalidOperationException(
                          "Nie znaleziono aktywnego adresu IPv4 sieci lokalnej.");

        Directory.CreateDirectory(SettingsDirectory);

        var certificatePassword = CreateSecret(32);
        var certificatePath = Path.Combine(SettingsDirectory, CertificateFileName);
        var certificate = CreateServerCertificate(address);
        await File.WriteAllBytesAsync(
            certificatePath,
            certificate.Export(X509ContentType.Pfx, certificatePassword));

        var options = new NetworkSyncOptions
        {
            Mode = NetworkSyncModes.Host,
            ListenAddress = address.ToString(),
            Port = 54443,
            HostUrl = $"https://{address}:54443",
            ApiToken = CreateSecret(48),
            CertificateThumbprint = NormalizeThumbprint(certificate.Thumbprint),
            CertificatePath = certificatePath,
            CertificatePassword = certificatePassword
        };

        await SaveAsync(options);

        var pairing = new NetworkPairingModel
        {
            HostUrl = options.HostUrl,
            ApiToken = options.ApiToken,
            CertificateThumbprint = options.CertificateThumbprint
        };

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var pairingPath = Path.Combine(desktop, "QA-Manager-polaczenie.json");
        await File.WriteAllTextAsync(
            pairingPath,
            JsonSerializer.Serialize(pairing, JsonOptions));

        return pairingPath;
    }

    public static async Task ConfigureClientAsync(string pairingPath)
    {
        if (!File.Exists(pairingPath))
        {
            throw new FileNotFoundException("Nie znaleziono pliku połączenia.", pairingPath);
        }

        var pairing = JsonSerializer.Deserialize<NetworkPairingModel>(
                          await File.ReadAllTextAsync(pairingPath),
                          JsonOptions)
                      ?? throw new InvalidOperationException("Plik połączenia jest nieprawidłowy.");

        if (!Uri.TryCreate(pairing.HostUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(pairing.ApiToken) ||
            string.IsNullOrWhiteSpace(pairing.CertificateThumbprint))
        {
            throw new InvalidOperationException("Plik połączenia nie zawiera poprawnej konfiguracji HTTPS.");
        }

        await SaveAsync(new NetworkSyncOptions
        {
            Mode = NetworkSyncModes.Client,
            HostUrl = pairing.HostUrl.TrimEnd('/'),
            ApiToken = pairing.ApiToken,
            CertificateThumbprint = NormalizeThumbprint(pairing.CertificateThumbprint)
        });
    }

    public static Task ConfigureLocalAsync() =>
        SaveAsync(new NetworkSyncOptions());

    public static string NormalizeThumbprint(string? thumbprint) =>
        new string((thumbprint ?? string.Empty)
            .Where(Uri.IsHexDigit)
            .ToArray())
            .ToUpperInvariant();

    private static IPAddress? FindLanAddress()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(network =>
                network.OperationalStatus == OperationalStatus.Up &&
                network.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(network => network.GetIPProperties().UnicastAddresses)
            .Where(address =>
                address.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(address.Address) &&
                !address.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
            .Select(address => address.Address)
            .FirstOrDefault();
    }

    private static X509Certificate2 CreateServerCertificate(IPAddress address)
    {
        using var key = RSA.Create(3072);
        var request = new CertificateRequest(
            "CN=QA Manager Local Sync",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new("1.3.6.1.5.5.7.3.1")
                },
                true));

        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(address);
        san.AddIpAddress(IPAddress.Loopback);
        san.AddDnsName("localhost");
        request.CertificateExtensions.Add(san.Build());

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(3));
    }

    private static string CreateSecret(int byteCount) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteCount))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
