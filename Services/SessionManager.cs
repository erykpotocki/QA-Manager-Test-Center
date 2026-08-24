using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QARegressionManager.Services;

public sealed class SessionManager
{
    private readonly string _sessionFilePath;

    public static int DeleteAllLocalSessions()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QARegressionManager");

        if (!Directory.Exists(directory))
        {
            return 0;
        }

        var removed = 0;
        foreach (var path in Directory.EnumerateFiles(directory, "session*.json"))
        {
            File.Delete(path);
            removed++;
        }

        return removed;
    }

    private static readonly SemaphoreSlim SaveLock =
        new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public SessionManager(
        string? login = null)
    {
        var applicationDataDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "QARegressionManager");

        Directory.CreateDirectory(
            applicationDataDirectory);

        var sessionFileName =
            string.IsNullOrWhiteSpace(login)
                ? "session.json"
                : $"session_{CreateSafeLogin(login)}.json";

        _sessionFilePath =
            Path.Combine(
                applicationDataDirectory,
                sessionFileName);
    }

    public async Task<SessionStateModel> LoadAsync()
    {
        if (!File.Exists(
                _sessionFilePath))
        {
            return CreateNewSession();
        }

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    _sessionFilePath);

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return CreateNewSession();
            }

            var session =
                JsonSerializer.Deserialize<SessionStateModel>(
                    json,
                    _jsonOptions);

            if (session is not null)
            {
                session.AssignmentIds ??=
                    new List<Guid>();
            }

            return session ??
                   CreateNewSession();
        }
        catch
        {
            return CreateNewSession();
        }
    }

    public async Task SaveAsync(
        SessionStateModel session)
    {
        await SaveLock.WaitAsync();

        try
        {
            session.LastSaveTime =
                DateTimeOffset.Now;

            var json =
                JsonSerializer.Serialize(
                    session,
                    _jsonOptions);

            var temporaryFilePath =
                _sessionFilePath +
                ".tmp";

            await File.WriteAllTextAsync(
                temporaryFilePath,
                json);

            File.Move(
                temporaryFilePath,
                _sessionFilePath,
                true);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    public async Task MarkSessionStartedAsync(
        SessionStateModel session,
        string projectName,
        string applicationVersion,
        string testerName,
        string sessionMode = "AdHoc")
    {
        session.SessionId =
            Guid.NewGuid();

        session.SessionStartedAt =
            DateTimeOffset.Now;

        session.ProjectKey =
            projectName;

        session.ApplicationVersion =
            applicationVersion;

        session.TesterName =
            testerName;

        session.SessionMode =
            sessionMode;

        session.AssignmentIds.Clear();

        session.LastOpenedTestType =
            string.Empty;

        session.LastOpenedCollectionKey =
            string.Empty;

        session.LastOpenedTestName =
            string.Empty;

        session.HasAnyResults =
            false;

        session.IsReportGenerated =
            false;

        await SaveAsync(
            session);
    }

    public async Task UpdateLastOpenedLocationAsync(
        SessionStateModel session,
        string testTypeKey,
        string collectionKey)
    {
        session.LastOpenedTestType =
            testTypeKey;

        session.LastOpenedCollectionKey =
            collectionKey;

        await SaveAsync(
            session);
    }

    public async Task UpdateAssignmentContextAsync(
        SessionStateModel session,
        IEnumerable<Guid> assignmentIds)
    {
        session.AssignmentIds =
            assignmentIds
                .Distinct()
                .ToList();

        await SaveAsync(
            session);
    }

    public async Task InvalidateAssignedSessionAsync(
        SessionStateModel session)
    {
        session.HasAnyResults =
            false;

        session.IsReportGenerated =
            true;

        session.SessionMode =
            "AdHoc";

        session.AssignmentIds.Clear();

        await SaveAsync(
            session);
    }

    public async Task MarkResultChangedAsync(
        SessionStateModel session,
        string? testName = null)
    {
        if (session.SessionId ==
            Guid.Empty)
        {
            session.SessionId =
                Guid.NewGuid();
        }

        if (session.SessionStartedAt is null)
        {
            session.SessionStartedAt =
                DateTimeOffset.Now;
        }

        session.HasAnyResults =
            true;

        if (!string.IsNullOrWhiteSpace(
                testName))
        {
            session.LastOpenedTestName =
                testName.Trim();
        }

        session.IsReportGenerated =
            false;

        await SaveAsync(
            session);
    }

    public async Task MarkReportGeneratedAsync(
        SessionStateModel session)
    {
        session.IsReportGenerated =
            true;

        await SaveAsync(
            session);
    }

    public async Task UpdateApplicationVersionAsync(
        SessionStateModel session,
        string applicationVersion)
    {
        session.ApplicationVersion =
            applicationVersion.Trim();

        await SaveAsync(
            session);
    }

    public async Task StartNewSessionAsync(
        SessionStateModel session,
        string projectName,
        string applicationVersion,
        string testerName,
        string sessionMode = "AdHoc")
    {
        session.SessionId =
            Guid.NewGuid();

        session.SessionStartedAt =
            DateTimeOffset.Now;

        session.ProjectKey =
            projectName;

        session.ApplicationVersion =
            applicationVersion;

        session.TesterName =
            testerName;

        session.SessionMode =
            sessionMode;

        session.AssignmentIds.Clear();

        session.LastOpenedTestType =
            string.Empty;

        session.LastOpenedCollectionKey =
            string.Empty;

        session.LastOpenedTestName =
            string.Empty;

        session.HasAnyResults =
            false;

        session.IsReportGenerated =
            false;

        await SaveAsync(
            session);
    }

    public bool ShouldAskToContinue(
        SessionStateModel session)
    {
        return !session.IsReportGenerated &&
               string.Equals(
                   session.SessionMode,
                   "Assigned",
                   StringComparison.OrdinalIgnoreCase) &&
               session.AssignmentIds.Count > 0;
    }

    public static SessionStateModel CreateNewSession()
    {
        return new SessionStateModel
        {
            SessionId =
                Guid.NewGuid(),

            SessionStartedAt =
                DateTimeOffset.Now,

            LastSaveTime =
                DateTimeOffset.Now,

            HasAnyResults =
                false,

            IsReportGenerated =
                false
        };
    }

    private static string CreateSafeLogin(
        string login)
    {
        var safeCharacters =
            login
                .Trim()
                .ToLowerInvariant()
                .Where(
                    character =>
                        char.IsLetterOrDigit(character) ||
                        character is '-' or '_')
                .ToArray();

        return safeCharacters.Length == 0
            ? "unknown"
            : new string(safeCharacters);
    }
}

public sealed class SessionStateModel
{
    public Guid SessionId { get; set; }

    public DateTimeOffset? SessionStartedAt { get; set; }

    public DateTimeOffset LastSaveTime { get; set; }

    public string ProjectKey { get; set; } =
        string.Empty;

    public string ApplicationVersion { get; set; } =
        string.Empty;

    public string TesterName { get; set; } =
        string.Empty;

    public string SessionMode { get; set; } =
        "AdHoc";

    public List<Guid> AssignmentIds { get; set; } =
        new();

    public string LastOpenedTestType { get; set; } =
        string.Empty;

    public string LastOpenedCollectionKey { get; set; } =
        string.Empty;

    public string LastOpenedTestName { get; set; } =
        string.Empty;

    public bool HasAnyResults { get; set; }

    public bool IsReportGenerated { get; set; }
}
