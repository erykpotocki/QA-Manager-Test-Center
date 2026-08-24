using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace QARegressionManager.Services;

public static class ApplicationRestartService
{
    private const string RestartArgument = "--restart-after";

    public static string[] WaitForPreviousInstance(string[] args)
    {
        if (args.Length < 2 ||
            !string.Equals(args[0], RestartArgument, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(args[1], out var processId))
        {
            return args;
        }

        try
        {
            using var previousProcess = Process.GetProcessById(processId);
            previousProcess.WaitForExit(15_000);
        }
        catch (ArgumentException)
        {
            // Poprzednia instancja zdążyła się już zamknąć.
        }

        return args.Skip(2).ToArray();
    }

    public static void Restart()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(
                "Nie udało się ustalić ścieżki pliku wykonywalnego aplikacji.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(RestartArgument);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Nie udało się uruchomić nowej instancji aplikacji.");

        if (Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
