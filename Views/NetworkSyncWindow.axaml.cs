using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class NetworkSyncWindow : Window
{
    private readonly bool _canConfigureHost;

    public NetworkSyncWindow()
        : this(false)
    {
    }

    public NetworkSyncWindow(bool canConfigureHost)
    {
        _canConfigureHost = canConfigureHost;
        InitializeComponent();
        HostConfigurationBorder.IsVisible = _canConfigureHost;
        LocalModeButton.IsVisible = _canConfigureHost;
        Opened += async (_, _) => await RefreshStatusAsync();
    }

    private async void ConfigureHostButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_canConfigureHost)
        {
            return;
        }

        await RunConfigurationAsync(async () =>
        {
            var pairingPath = await NetworkSyncConfiguration.ConfigureHostAsync();
            return
                string.Format(LocalizationService.T("Network.HostReady"), pairingPath);
        }, restartAfterSuccess: true);
    }

    private async void ConfigureClientButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = LocalizationService.T("Network.FilePickerTitle"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(LocalizationService.T("Network.FileType"))
                    {
                        Patterns = new[] { "*.json" }
                    }
                }
            });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await RunConfigurationAsync(async () =>
        {
            await NetworkSyncConfiguration.ConfigureClientAsync(path);
            return LocalizationService.T("Network.ClientSaved");
        }, restartAfterSuccess: true);
    }

    private async void LocalModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_canConfigureHost)
        {
            return;
        }

        await RunConfigurationAsync(async () =>
        {
            await NetworkSyncConfiguration.ConfigureLocalAsync();
            return LocalizationService.T("Network.LocalSaved");
        }, restartAfterSuccess: true);
    }

    private async System.Threading.Tasks.Task RunConfigurationAsync(
        Func<System.Threading.Tasks.Task<string>> operation,
        bool restartAfterSuccess = false)
    {
        ConfigureHostButton.IsEnabled = false;
        ConfigureClientButton.IsEnabled = false;
        try
        {
            ResultTextBlock.Foreground = Avalonia.Media.Brushes.SeaGreen;
            ResultTextBlock.Text = await operation();
            SharedDocumentStore.ResetConfigurationCache();
            await RefreshStatusAsync();

            if (restartAfterSuccess)
            {
                await System.Threading.Tasks.Task.Delay(700);
                ApplicationRestartService.Restart();
            }
        }
        catch (Exception exception)
        {
            ResultTextBlock.Foreground = Avalonia.Media.Brushes.IndianRed;
            ResultTextBlock.Text = string.Format(
                LocalizationService.T("Network.SaveFailed"),
                exception.Message);
        }
        finally
        {
            ConfigureHostButton.IsEnabled = true;
            ConfigureClientButton.IsEnabled = true;
        }
    }

    private async System.Threading.Tasks.Task RefreshStatusAsync()
    {
        var options = await NetworkSyncConfiguration.LoadAsync();
        switch (options.Mode)
        {
            case NetworkSyncModes.Host:
                CurrentModeTextBlock.Text = LocalizationService.T("Network.ModeHost");
                CurrentAddressTextBlock.Text = options.HostUrl;
                break;
            case NetworkSyncModes.Client:
                CurrentModeTextBlock.Text = LocalizationService.T("Network.ModeClient");
                CurrentAddressTextBlock.Text = options.HostUrl;
                break;
            default:
                CurrentModeTextBlock.Text = LocalizationService.T("Network.ModeLocal");
                CurrentAddressTextBlock.Text =
                    LocalizationService.T("Network.LocalDescription");
                break;
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
