namespace QARegressionManager.Models;

public sealed class NetworkSyncOptions
{
    public string Mode { get; set; } = NetworkSyncModes.Local;

    public string HostUrl { get; set; } = string.Empty;

    public string ListenAddress { get; set; } = string.Empty;

    public int Port { get; set; } = 54443;

    public string ApiToken { get; set; } = string.Empty;

    public string CertificateThumbprint { get; set; } = string.Empty;

    public string CertificatePath { get; set; } = string.Empty;

    public string CertificatePassword { get; set; } = string.Empty;
}

public static class NetworkSyncModes
{
    public const string Local = "Local";
    public const string Host = "Host";
    public const string Client = "Client";
}

public sealed class NetworkPairingModel
{
    public string HostUrl { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public string CertificateThumbprint { get; set; } = string.Empty;
}
