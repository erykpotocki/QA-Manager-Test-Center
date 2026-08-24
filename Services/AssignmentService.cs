using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class AssignmentService
{
    private readonly string _dataFilePath;
    private static readonly SemaphoreSlim SaveLock =
        new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public AssignmentService()
    {
        var directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "QARegressionManager");

        Directory.CreateDirectory(
            directory);

        _dataFilePath =
            Path.Combine(
                directory,
                "assignments.json");
    }

    public async Task<TestAssignmentModel[]> GetActiveAssignmentsForUserAsync(
        string recipientLogin)
    {
        var data =
            await LoadAsync();

        EnsureProgressEntries(
            data.Assignments);

        return data.Assignments
            .Where(
                assignment =>
                    assignment.IsActive &&
                    !assignment.CompletedAt.HasValue &&
                    !assignment.WithdrawnAt.HasValue &&
                    assignment.TestCaseIds.Count > 0 &&
                    string.Equals(
                        assignment.RecipientLogin,
                        recipientLogin,
                        StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(
                assignment =>
                    assignment.UpdatedAt)
            .ToArray();
    }

    public async Task<TestAssignmentModel[]> GetAssignmentsByIdsAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return Array.Empty<TestAssignmentModel>();
        }

        var data =
            await LoadAsync();

        EnsureProgressEntries(
            data.Assignments);

        return data.Assignments
            .Where(
                assignment =>
                    ids.Contains(
                        assignment.Id))
            .ToArray();
    }

    public async Task<TestAssignmentModel[]> GetActiveAssignmentsForProjectAsync(
        string projectKey)
    {
        var data =
            await LoadAsync();

        EnsureProgressEntries(
            data.Assignments);

        return data.Assignments
            .Where(
                assignment =>
                    assignment.IsActive &&
                    string.Equals(
                        assignment.ProjectKey,
                        projectKey,
                        StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(
                assignment =>
                    assignment.UpdatedAt)
            .ToArray();
    }

    public async Task<TestAssignmentModel[]> GetAssignmentsForDashboardAsync()
    {
        var data =
            await LoadAsync();

        EnsureProgressEntries(
            data.Assignments);

        return data.Assignments
            .Where(
                assignment =>
                    !assignment.IsArchived)
            .OrderByDescending(
                assignment =>
                    assignment.IsActive)
            .ThenByDescending(
                assignment =>
                    assignment.CompletedAt ??
                    assignment.WithdrawnAt ??
                    assignment.UpdatedAt)
            .Take(100)
            .ToArray();
    }

    public async Task<TestAssignmentModel[]> GetArchivedAssignmentsAsync()
    {
        var data =
            await LoadAsync();

        var expirationThreshold =
            DateTimeOffset.Now.AddDays(-60);

        var expiredIds =
            data.Assignments
                .Where(
                    assignment =>
                        assignment.IsArchived &&
                        (assignment.ArchivedAt ?? assignment.UpdatedAt) <= expirationThreshold)
                .Select(assignment => assignment.Id)
                .ToHashSet();

        if (expiredIds.Count > 0)
        {
            data.Assignments.RemoveAll(
                assignment => expiredIds.Contains(assignment.Id));

            data.Notifications.RemoveAll(
                notification =>
                    notification.AssignmentId.HasValue &&
                    expiredIds.Contains(notification.AssignmentId.Value));

            await SaveAsync(data);
        }

        EnsureProgressEntries(
            data.Assignments);

        return data.Assignments
            .Where(
                assignment =>
                    assignment.IsArchived)
            .OrderByDescending(
                assignment =>
                    assignment.ArchivedAt ?? assignment.UpdatedAt)
                .ToArray();
    }

    public async Task<int> DeleteAllArchivedAssignmentsAsync()
    {
        var data =
            await LoadAsync();

        var archivedIds =
            data.Assignments
                .Where(assignment => assignment.IsArchived)
                .Select(assignment => assignment.Id)
                .ToHashSet();

        if (archivedIds.Count == 0)
        {
            return 0;
        }

        var removed =
            data.Assignments.RemoveAll(
                assignment => archivedIds.Contains(assignment.Id));

        data.Notifications.RemoveAll(
            notification =>
                notification.AssignmentId.HasValue &&
                archivedIds.Contains(notification.AssignmentId.Value));

        await SaveAsync(data);
        return removed;
    }

    public async Task<int> ResetAllAssignmentDataAsync()
    {
        var data =
            await LoadAsync();

        var removed =
            data.Assignments.Count;

        data.Assignments.Clear();
        data.Notifications.Clear();
        await SaveAsync(data);

        return removed;
    }

    public async Task<int> ArchiveAssignmentsAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return 0;
        }

        var data =
            await LoadAsync();

        var archivedAt =
            DateTimeOffset.Now;

        var changed =
            0;

        foreach (var assignment in data.Assignments.Where(
                     assignment =>
                         ids.Contains(assignment.Id) &&
                         !assignment.IsArchived))
        {
            assignment.IsArchived =
                true;

            assignment.ArchivedAt =
                archivedAt;

            assignment.IsActive =
                false;

            changed++;
        }

        if (changed > 0)
        {
            await SaveAsync(
                data);
        }

        return changed;
    }

    public async Task<int> RestoreArchivedAssignmentsAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids = assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return 0;
        }

        var data = await LoadAsync();
        var changed = 0;

        foreach (var assignment in data.Assignments.Where(
                     assignment => ids.Contains(assignment.Id) && assignment.IsArchived))
        {
            assignment.IsArchived = false;
            assignment.ArchivedAt = null;
            assignment.IsActive = false;
            changed++;
        }

        if (changed > 0)
        {
            await SaveAsync(data);
        }

        return changed;
    }

    public async Task<int> DeleteArchivedAssignmentsAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return 0;
        }

        var data =
            await LoadAsync();

        var removedIds =
            data.Assignments
                .Where(
                    assignment =>
                        assignment.IsArchived &&
                        ids.Contains(assignment.Id))
                .Select(
                    assignment =>
                        assignment.Id)
                .ToHashSet();

        var removed =
            data.Assignments.RemoveAll(
                assignment =>
                    removedIds.Contains(assignment.Id));

        if (removed > 0)
        {
            data.Notifications.RemoveAll(
                notification =>
                    notification.AssignmentId.HasValue &&
                    removedIds.Contains(notification.AssignmentId.Value));

            await SaveAsync(
                data);
        }

        return removed;
    }

    public async Task<TestAssignmentModel> SaveAssignmentAsync(
        Guid? assignmentId,
        string projectKey,
        string projectName,
        string applicationVersion,
        string recipientLogin,
        string assignedByLogin,
        IEnumerable<Guid> testCaseIds)
    {
        var savedAssignments =
            await SaveAssignmentsBatchAsync(
                new[]
                {
                    new AssignmentSaveRequest(
                        assignmentId,
                        projectKey,
                        projectName,
                        applicationVersion,
                        recipientLogin,
                        assignedByLogin,
                        testCaseIds.ToArray())
                });

        return savedAssignments[0];
    }

    public async Task<TestAssignmentModel[]> SaveAssignmentsBatchAsync(
        IEnumerable<AssignmentSaveRequest> requests)
    {
        var requestList =
            requests.ToArray();

        if (requestList.Length == 0)
        {
            return Array.Empty<TestAssignmentModel>();
        }

        var data =
            await LoadAsync();

        var savedAssignments =
            new List<TestAssignmentModel>();

        var reservedCaseIds =
            data.Assignments
                .Where(
                    assignment =>
                        assignment.IsActive)
                .SelectMany(
                    assignment =>
                        assignment.TestCaseIds)
                .ToHashSet();

        foreach (var request in requestList)
        {
            if (request.AssignmentId.HasValue)
            {
                var editedAssignment =
                    data.Assignments.FirstOrDefault(
                        assignment =>
                            assignment.Id ==
                            request.AssignmentId.Value);

                if (editedAssignment is not null)
                {
                    reservedCaseIds.ExceptWith(
                        editedAssignment.TestCaseIds);
                }
            }

            var requestedIds =
                request.TestCaseIds
                    .Distinct()
                    .ToArray();

            if (requestedIds.Any(
                    reservedCaseIds.Contains))
            {
                throw new InvalidOperationException(
                    "Co najmniej jeden przypadek jest już przypisany do innej aktywnej sesji.");
            }

            reservedCaseIds.UnionWith(
                requestedIds);

            savedAssignments.Add(
                ApplyAssignment(
                    data,
                    request));
        }

        await SaveAsync(
            data);

        return savedAssignments.ToArray();
    }

    private static TestAssignmentModel ApplyAssignment(
        AssignmentDataModel data,
        AssignmentSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.RecipientLogin) ||
            string.IsNullOrWhiteSpace(
                request.ApplicationVersion) ||
            request.TestCaseIds.Count == 0)
        {
            throw new InvalidOperationException(
                "Przypisanie nie zawiera odbiorcy, wersji lub przypadków.");
        }

        var assignment =
            request.AssignmentId.HasValue
                ? data.Assignments.FirstOrDefault(
                    item =>
                        item.Id == request.AssignmentId.Value)
                : null;

        var previousRecipient =
            assignment?.RecipientLogin;

        if (assignment is null)
        {
            assignment =
                new TestAssignmentModel
                {
                    ProjectKey = request.ProjectKey,
                    ProjectName = request.ProjectName,
                    CreatedAt = DateTimeOffset.Now
                };

            data.Assignments.Add(
                assignment);
        }

        if (assignment.BatchId == Guid.Empty)
        {
            assignment.BatchId =
                request.BatchId == Guid.Empty
                    ? assignment.Id
                    : request.BatchId;
        }

        assignment.ApplicationVersion =
            request.ApplicationVersion.Trim();

        assignment.RecipientLogin =
            request.RecipientLogin.Trim();

        assignment.AssignedByLogin =
            request.AssignedByLogin.Trim();

        assignment.TestCaseIds =
            request.TestCaseIds
                .Distinct()
                .ToList();

        var previousProgress =
            assignment.CaseProgress
                .GroupBy(
                    progress =>
                        progress.TestCaseId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.Last());

        assignment.CaseProgress =
            assignment.TestCaseIds
                .Select(
                    testCaseId =>
                        previousProgress.TryGetValue(
                            testCaseId,
                            out var progress)
                            ? progress
                            : new AssignmentCaseProgressModel
                            {
                                TestCaseId = testCaseId,
                                Status = "InProgress",
                                UpdatedAt = DateTimeOffset.Now
                            })
                .ToList();

        assignment.CompletedAt =
            null;

        assignment.CompletionNotificationSent =
            false;

        assignment.IsActive =
            true;

        assignment.UpdatedAt =
            DateTimeOffset.Now;

        if (!string.IsNullOrWhiteSpace(
                previousRecipient) &&
            !string.Equals(
                previousRecipient,
                assignment.RecipientLogin,
                StringComparison.OrdinalIgnoreCase))
        {
            AddNotification(
                data,
                previousRecipient,
                "Przypisanie zostało przeniesione",
                $"Sesja projektu {request.ProjectName} została przepisana na innego użytkownika.",
                assignment.Id);
        }

        AddNotification(
            data,
            assignment.RecipientLogin,
            request.AssignmentId.HasValue
                ? "Zmieniono przypisane testy"
                : "Nowe testy do wykonania",
            $"{request.AssignedByLogin} przypisał sesję projektu {request.ProjectName}, wersja {request.ApplicationVersion} ({assignment.TestCaseIds.Count} przypadków).",
            assignment.Id);

        return assignment;
    }

    public async Task<TestAssignmentModel> UpdateAssignmentCaseStatusAsync(
        Guid assignmentId,
        Guid testCaseId,
        string status,
        string comment = "")
    {
        var data =
            await LoadAsync();

        var assignment =
            data.Assignments.FirstOrDefault(
                item =>
                    item.Id == assignmentId &&
                    item.IsActive)
            ?? throw new InvalidOperationException(
                "Nie znaleziono aktywnego przypisania.");

        if (!assignment.TestCaseIds.Contains(
                testCaseId))
        {
            throw new InvalidOperationException(
                "Przypadek nie należy do tego przypisania.");
        }

        EnsureProgressEntries(
            new[]
            {
                assignment
            });

        var progress =
            assignment.CaseProgress.First(
                item =>
                    item.TestCaseId == testCaseId);

        progress.Status =
            NormalizeStatus(
                status);

        progress.Comment =
            comment?.Trim() ?? string.Empty;

        progress.UpdatedAt =
            DateTimeOffset.Now;

        assignment.UpdatedAt =
            DateTimeOffset.Now;

        var isComplete =
            assignment.CaseProgress.Count > 0 &&
            assignment.CaseProgress.All(
                item =>
                    IsFinalStatus(
                        item.Status));

        if (!isComplete)
        {
            assignment.CompletedAt =
                null;

            assignment.CompletionNotificationSent =
                false;
        }

        await SaveAsync(
            data);

        return assignment;
    }

    public async Task<bool> CompleteAssignmentAsync(
        Guid assignmentId,
        bool allowUnfinished = false)
    {
        var data =
            await LoadAsync();

        var assignment =
            data.Assignments.FirstOrDefault(
                item =>
                    item.Id == assignmentId &&
                    item.IsActive &&
                    !item.WithdrawnAt.HasValue);

        if (assignment is null)
        {
            return false;
        }

        EnsureProgressEntries(
            new[]
            {
                assignment
            });

        if (assignment.CaseProgress.Count == 0 ||
            (!allowUnfinished &&
             assignment.CaseProgress.Any(
                 progress =>
                     !IsFinalStatus(
                         progress.Status))))
        {
            return false;
        }

        assignment.CompletedAt ??=
            DateTimeOffset.Now;

        assignment.UpdatedAt =
            DateTimeOffset.Now;

        if (!assignment.CompletionNotificationSent)
        {
            assignment.CompletionNotificationSent =
                true;

            await AddCompletionNotificationsAsync(
                data,
                assignment);
        }

        await SaveAsync(
            data);

        return true;
    }

    public async Task MarkReportsGeneratedAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return;
        }

        var data =
            await LoadAsync();

        var generatedAt =
            DateTimeOffset.Now;

        var batchIds = data.Assignments
            .Where(assignment => ids.Contains(assignment.Id))
            .Select(assignment => assignment.BatchId == Guid.Empty ? assignment.Id : assignment.BatchId)
            .ToHashSet();

        foreach (var batchId in batchIds)
        {
            var batchAssignments = data.Assignments
                .Where(
                    assignment =>
                        !assignment.IsArchived &&
                        !assignment.WithdrawnAt.HasValue &&
                        (assignment.BatchId == Guid.Empty ? assignment.Id : assignment.BatchId) == batchId)
                .ToArray();

            if (batchAssignments.Length == 0 ||
                batchAssignments.Any(assignment => !assignment.CompletedAt.HasValue))
            {
                continue;
            }

            foreach (var assignment in batchAssignments)
            {
                assignment.ReportGeneratedAt = generatedAt;
                assignment.IsActive = false;
                assignment.UpdatedAt = generatedAt;
            }
        }

        await SaveAsync(
            data);
    }

    public async Task MarkAssignmentStartedAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return;
        }

        var data =
            await LoadAsync();

        var now =
            DateTimeOffset.Now;

        var changed =
            false;

        foreach (var assignment in data.Assignments.Where(
                     assignment =>
                         assignment.IsActive &&
                         ids.Contains(
                             assignment.Id)))
        {
            assignment.StartedAt ??=
                now;

            assignment.IsPaused =
                false;

            assignment.UpdatedAt =
                now;

            changed =
                true;
        }

        if (changed)
        {
            await SaveAsync(
                data);
        }
    }

    public async Task MarkAssignmentsPausedAsync(
        IEnumerable<Guid> assignmentIds)
    {
        var ids =
            assignmentIds.ToHashSet();

        if (ids.Count == 0)
        {
            return;
        }

        var data =
            await LoadAsync();

        var now =
            DateTimeOffset.Now;

        var changed =
            false;

        foreach (var assignment in data.Assignments.Where(
                     assignment =>
                         assignment.IsActive &&
                         ids.Contains(
                             assignment.Id)))
        {
            assignment.IsPaused =
                true;

            assignment.UpdatedAt =
                now;

            changed =
                true;
        }

        if (changed)
        {
            await SaveAsync(
                data);
        }
    }

    public async Task WithdrawAssignmentAsync(
        Guid assignmentId,
        string changedByLogin)
    {
        var data =
            await LoadAsync();

        var assignment =
            data.Assignments.FirstOrDefault(
                item =>
                    item.Id == assignmentId)
            ?? throw new InvalidOperationException(
                "Nie znaleziono przypisanej sesji.");

        EnsureProgressEntries(
            new[]
            {
                assignment
            });

        var withdrawnAt =
            DateTimeOffset.Now;

        assignment.IsActive =
            false;

        assignment.WithdrawnAt =
            withdrawnAt;

        assignment.WithdrawnByLogin =
            changedByLogin?.Trim() ?? string.Empty;

        assignment.UpdatedAt =
            withdrawnAt;

        AddNotification(
            data,
            assignment.RecipientLogin,
            "Wycofano przypisane testy",
            $"{changedByLogin} wycofał sesję projektu {assignment.ProjectName}.",
            assignment.Id);

        await SaveAsync(
            data);
    }

    public async Task<int> WithdrawAllActiveAssignmentsAsync(
        string changedByLogin)
    {
        var data =
            await LoadAsync();

        var activeAssignments =
            data.Assignments
                .Where(
                    assignment =>
                        assignment.IsActive)
                .ToArray();

        if (activeAssignments.Length == 0)
        {
            return 0;
        }

        foreach (var assignment in activeAssignments)
        {
            assignment.IsActive =
                false;

            assignment.WithdrawnAt =
                DateTimeOffset.Now;

            assignment.WithdrawnByLogin =
                changedByLogin?.Trim() ?? string.Empty;

            assignment.UpdatedAt =
                DateTimeOffset.Now;

            AddNotification(
                data,
                assignment.RecipientLogin,
                "Wycofano przypisane testy",
                $"{changedByLogin} wyzerował aktywne przypisanie projektu {assignment.ProjectName}, wersja {assignment.ApplicationVersion}.",
                assignment.Id);
        }

        await SaveAsync(
            data);

        return activeAssignments.Length;
    }

    public async Task RemoveLegacyTestProfileDataAsync()
    {
        var data =
            await LoadAsync();

        var assignmentIds =
            data.Assignments
                .Where(
                    assignment =>
                        IsLegacyTestLogin(
                            assignment.RecipientLogin))
                .Select(
                    assignment =>
                        assignment.Id)
                .ToHashSet();

        var changed =
            data.Assignments.RemoveAll(
                assignment =>
                    assignmentIds.Contains(
                        assignment.Id)) > 0;

        changed |=
            data.Notifications.RemoveAll(
                notification =>
                    IsLegacyTestLogin(
                        notification.RecipientLogin) ||
                    notification.AssignmentId.HasValue &&
                    assignmentIds.Contains(
                        notification.AssignmentId.Value)) > 0;

        if (changed)
        {
            await SaveAsync(
                data);
        }
    }

    public async Task<UserNotificationModel[]> GetNotificationsForUserAsync(
        string recipientLogin)
    {
        var data =
            await LoadAsync();

        return data.Notifications
            .Where(
                notification =>
                    string.Equals(
                        notification.RecipientLogin,
                        recipientLogin,
                        StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(
                notification =>
                    notification.CreatedAt)
            .ToArray();
    }

    public async Task<int> GetUnreadCountAsync(
        string recipientLogin)
    {
        var notifications =
            await GetNotificationsForUserAsync(
                recipientLogin);

        return notifications.Count(
            notification =>
                !notification.IsRead);
    }

    public async Task<int> ClearNotificationsForUserAsync(
        string recipientLogin)
    {
        var normalizedLogin =
            recipientLogin.Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedLogin))
        {
            return 0;
        }

        var data =
            await LoadAsync();

        var removedCount =
            data.Notifications.RemoveAll(
                notification =>
                    string.Equals(
                        notification.RecipientLogin,
                        normalizedLogin,
                        StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0)
        {
            await SaveAsync(
                data);
        }

        return removedCount;
    }

    public async Task SendUserNotificationAsync(
        string recipientLogin,
        string title,
        string message)
    {
        if (string.IsNullOrWhiteSpace(recipientLogin))
        {
            return;
        }

        var data =
            await LoadAsync();

        data.Notifications.Add(
            new UserNotificationModel
            {
                RecipientLogin = recipientLogin.Trim(),
                Title = title,
                Message = message,
                AssignmentId = null,
                IsRead = false,
                CreatedAt = DateTimeOffset.Now
            });

        await SaveAsync(
            data);
    }

    public async Task MarkAllNotificationsReadAsync(
        string recipientLogin)
    {
        var data =
            await LoadAsync();

        foreach (var notification in
                 data.Notifications.Where(
                     item =>
                         string.Equals(
                             item.RecipientLogin,
                             recipientLogin,
                             StringComparison.OrdinalIgnoreCase)))
        {
            notification.IsRead =
                true;
        }

        await SaveAsync(
            data);
    }

    public async Task<Guid?> RequestStructureDeletionAsync(
        string projectKey,
        string entityType,
        string entityKey,
        string entityName,
        string requestedByLogin)
    {
        var data = await LoadAsync();
        var existing = data.StructureChangeRequests.FirstOrDefault(item =>
            item.Status == "Pending" &&
            string.Equals(item.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing.Id;
        }

        var request = new StructureChangeRequestModel
        {
            ProjectKey = projectKey,
            EntityType = entityType,
            EntityKey = entityKey,
            EntityName = entityName,
            RequestedByLogin = requestedByLogin.Trim()
        };
        data.StructureChangeRequests.Add(request);

        var profiles = await new UserProfileService().GetProfilesAsync();
        var managers = profiles.Where(profile => profile.SystemRoles.Any(role =>
                string.Equals(role, SystemRoleService.AdministratorRole, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, SystemRoleService.LeaderRole, StringComparison.OrdinalIgnoreCase)))
            .Select(profile => profile.Login)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var manager in managers)
        {
            data.Notifications.Add(new UserNotificationModel
            {
                RecipientLogin = manager,
                Title = "Prośba o usunięcie dużej gałęzi",
                Message = $"{requestedByLogin} prosi o usunięcie elementu „{entityName}”. Operacja obejmie całą jego zawartość.",
                StructureChangeRequestId = request.Id,
                CreatedAt = DateTimeOffset.Now
            });
        }

        await SaveAsync(data);
        return request.Id;
    }

    public async Task<StructureChangeRequestModel?> GetStructureChangeRequestAsync(Guid id)
    {
        var data = await LoadAsync();
        return data.StructureChangeRequests.FirstOrDefault(item => item.Id == id);
    }

    public async Task<bool> ResolveStructureDeletionAsync(
        Guid requestId,
        string managerLogin,
        bool approve)
    {
        var profiles = await new UserProfileService().GetProfilesAsync();
        var isManager = profiles.Any(profile =>
            string.Equals(profile.Login, managerLogin, StringComparison.OrdinalIgnoreCase) &&
            profile.SystemRoles.Any(role =>
                string.Equals(role, SystemRoleService.AdministratorRole, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, SystemRoleService.LeaderRole, StringComparison.OrdinalIgnoreCase)));
        if (!isManager)
        {
            return false;
        }

        var assignmentData = await LoadAsync();
        var request = assignmentData.StructureChangeRequests.FirstOrDefault(item =>
            item.Id == requestId && item.Status == "Pending");
        if (request is null)
        {
            return false;
        }

        if (approve)
        {
            var storage = new JsonStorageService();
            var testData = await storage.LoadAsync();

            if (string.Equals(request.EntityType, "Folder", StringComparison.OrdinalIgnoreCase))
            {
                var folderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    request.EntityKey
                };
                bool added;
                do
                {
                    added = false;
                    foreach (var folder in testData.Folders.Where(item =>
                                 string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                                 folderKeys.Contains(item.ParentSectionKey)).ToArray())
                    {
                        added |= folderKeys.Add(folder.SectionKey);
                    }
                } while (added);

                var collectionKeys = testData.Collections.Where(item =>
                        string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                        folderKeys.Contains(item.ParentFolderKey))
                    .Select(item => item.CollectionKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                testData.TestCases.RemoveAll(item =>
                    string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                    collectionKeys.Contains(item.SectionKey));
                testData.Collections.RemoveAll(item =>
                    string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                    collectionKeys.Contains(item.CollectionKey));
                testData.Folders.RemoveAll(item =>
                    string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                    folderKeys.Contains(item.SectionKey));
            }
            else
            {
                testData.TestCases.RemoveAll(item =>
                    string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.SectionKey, request.EntityKey, StringComparison.OrdinalIgnoreCase));
                testData.Collections.RemoveAll(item =>
                    string.Equals(item.ProjectKey, request.ProjectKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.CollectionKey, request.EntityKey, StringComparison.OrdinalIgnoreCase));
            }

            await storage.SaveAsync(testData);
        }

        request.Status = approve ? "Approved" : "Rejected";
        request.ResolvedByLogin = managerLogin;
        request.ResolvedAt = DateTimeOffset.Now;
        assignmentData.Notifications.RemoveAll(item => item.StructureChangeRequestId == request.Id);
        assignmentData.Notifications.Add(new UserNotificationModel
        {
            RecipientLogin = request.RequestedByLogin,
            Title = approve ? "Usunięcie zatwierdzone" : "Usunięcie odrzucone",
            Message = approve
                ? $"{managerLogin} zatwierdził usunięcie elementu „{request.EntityName}”."
                : $"{managerLogin} odrzucił usunięcie elementu „{request.EntityName}”.",
            CreatedAt = DateTimeOffset.Now
        });
        await SaveAsync(assignmentData);
        return true;
    }

    private static void AddNotification(
        AssignmentDataModel data,
        string recipientLogin,
        string title,
        string message,
        Guid assignmentId)
    {
        data.Notifications.Add(
            new UserNotificationModel
            {
                RecipientLogin = recipientLogin.Trim(),
                Title = title,
                Message = message,
                AssignmentId = assignmentId,
                IsRead = false,
                CreatedAt = DateTimeOffset.Now
            });
    }

    private static bool IsLegacyTestLogin(
        string login)
    {
        return login.StartsWith(
                   "tester",
                   StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(
                   login["tester".Length..],
                   out _);
    }

    private static void EnsureProgressEntries(
        IEnumerable<TestAssignmentModel> assignments)
    {
        foreach (var assignment in assignments)
        {
            assignment.CaseProgress ??=
                new List<AssignmentCaseProgressModel>();

            var existingIds =
                assignment.CaseProgress
                    .Select(
                        progress =>
                            progress.TestCaseId)
                    .ToHashSet();

            foreach (var testCaseId in
                     assignment.TestCaseIds.Where(
                         id =>
                             !existingIds.Contains(
                                 id)))
            {
                assignment.CaseProgress.Add(
                    new AssignmentCaseProgressModel
                    {
                        TestCaseId = testCaseId,
                        Status = "InProgress",
                        UpdatedAt = assignment.UpdatedAt
                    });
            }

            assignment.CaseProgress =
                assignment.CaseProgress
                    .Where(
                        progress =>
                            assignment.TestCaseIds.Contains(
                                progress.TestCaseId))
                    .GroupBy(
                        progress =>
                            progress.TestCaseId)
                    .Select(
                        group =>
                            group.Last())
                    .ToList();
        }
    }

    private static string NormalizeStatus(
        string status)
    {
        return status switch
        {
            "Success" => "Success",
            "Failed" => "Failed",
            "NA" => "NA",
            "Blocked" => "Blocked",
            "None" => "InProgress",
            "Pending" => "InProgress",
            _ => "InProgress"
        };
    }

    private static bool IsFinalStatus(
        string status)
    {
        return status is
            "Success" or
            "Failed" or
            "NA" or
            "Blocked";
    }

    private static async Task AddCompletionNotificationsAsync(
        AssignmentDataModel data,
        TestAssignmentModel assignment)
    {
        var profiles =
            await new UserProfileService()
                .GetProfilesAsync();

        var managerLogins =
            profiles
                .Where(
                    profile =>
                        profile.SystemRoles.Any(
                            role =>
                                string.Equals(
                                    role,
                                    SystemRoleService.AdministratorRole,
                                    StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(
                                    role,
                                    SystemRoleService.LeaderRole,
                                    StringComparison.OrdinalIgnoreCase)))
                .Select(
                    profile =>
                        profile.Login)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase);

        var completedCount =
            assignment.CaseProgress.Count(
                progress =>
                    IsFinalStatus(
                        progress.Status));

        var unfinishedCount =
            Math.Max(
                0,
                assignment.TestCaseIds.Count -
                completedCount);

        var completionDetails =
            unfinishedCount > 0
                ? $"{assignment.RecipientLogin} zakończył sesję po wykonaniu {completedCount} z {assignment.TestCaseIds.Count} przypadków projektu {assignment.ProjectName}, wersja {assignment.ApplicationVersion}. {unfinishedCount} niewykonanych przypadków wróciło do puli przypisań."
                : $"{assignment.RecipientLogin} ukończył {assignment.TestCaseIds.Count} przypadków projektu {assignment.ProjectName}, wersja {assignment.ApplicationVersion}.";

        foreach (var managerLogin in managerLogins)
        {
            AddNotification(
                data,
                managerLogin,
                "Przypisanie ukończone",
                completionDetails,
                assignment.Id);
        }
    }

    private async Task<AssignmentDataModel> LoadAsync()
    {
        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            return await SharedDocumentStore.LoadAsync<AssignmentDataModel>(
                SharedDocumentStore.AssignmentsDocument,
                _dataFilePath);
        }

        if (!File.Exists(
                _dataFilePath))
        {
            return new AssignmentDataModel();
        }

        try
        {
            return JsonSerializer.Deserialize<AssignmentDataModel>(
                       await File.ReadAllTextAsync(
                           _dataFilePath),
                       _jsonOptions)
                   ?? new AssignmentDataModel();
        }
        catch (JsonException)
        {
            return new AssignmentDataModel();
        }
    }

    private async Task SaveAsync(
        AssignmentDataModel data)
    {
        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            await SharedDocumentStore.SaveAsync(
                SharedDocumentStore.AssignmentsDocument,
                _dataFilePath,
                data);
            return;
        }

        await SaveLock.WaitAsync();

        try
        {
            var temporaryPath =
                _dataFilePath +
                ".tmp";

            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(
                    data,
                    _jsonOptions));

            File.Move(
                temporaryPath,
                _dataFilePath,
                true);
        }
        finally
        {
            SaveLock.Release();
        }
    }
}
