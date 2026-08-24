using Avalonia;
using System;

using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Services;

namespace QARegressionManager;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        args = ApplicationRestartService.WaitForPreviousInstance(args);

        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: @"Local\QAManager.SingleInstance",
            createdNew: out var isFirstInstance);

        if (!isFirstInstance)
        {
            return;
        }

        SharedStorageHost.StartIfConfiguredAsync()
            .GetAwaiter()
            .GetResult();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            try
            {
                SharedStorageHost.StopAsync()
                    .WaitAsync(TimeSpan.FromSeconds(5))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception) when (
                exception is TimeoutException
                or OperationCanceledException
                or ObjectDisposedException)
            {
                // Host synchronizacji nie może blokować zamknięcia aplikacji.
            }
        }

        // Kończy także ewentualne wątki serwera pozostawione przez bibliotekę hostującą.
        Environment.Exit(0);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
