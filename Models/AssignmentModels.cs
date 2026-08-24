using System;
using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class TestAssignmentModel
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    // One send action may contain assignments for several recipients.  Keeping
    // that relationship lets the dashboard present it as one logical session.
    public Guid BatchId { get; set; }

    public string ProjectKey { get; set; } =
        string.Empty;

    public string ProjectName { get; set; } =
        string.Empty;

    public string ApplicationVersion { get; set; } =
        string.Empty;

    public string RecipientLogin { get; set; } =
        string.Empty;

    public string AssignedByLogin { get; set; } =
        string.Empty;

    public List<Guid> TestCaseIds { get; set; } =
        new();

    public List<AssignmentCaseProgressModel> CaseProgress { get; set; } =
        new();

    public bool CompletionNotificationSent { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public bool IsPaused { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? ReportGeneratedAt { get; set; }

    public DateTimeOffset? WithdrawnAt { get; set; }

    public string WithdrawnByLogin { get; set; } =
        string.Empty;

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } =
        DateTimeOffset.Now;
}

public sealed class AssignmentCaseProgressModel
{
    public Guid TestCaseId { get; set; }

    public string Status { get; set; } =
        "InProgress";

    public string Comment { get; set; } =
        string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } =
        DateTimeOffset.Now;
}

public sealed class UserNotificationModel
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string RecipientLogin { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string Message { get; set; } =
        string.Empty;

    public Guid? AssignmentId { get; set; }

    public Guid? StructureChangeRequestId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.Now;
}

public sealed class AssignmentDataModel
{
    public List<TestAssignmentModel> Assignments { get; set; } =
        new();

    public List<UserNotificationModel> Notifications { get; set; } =
        new();

    public List<StructureChangeRequestModel> StructureChangeRequests { get; set; } =
        new();
}

public sealed class StructureChangeRequestModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProjectKey { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string RequestedByLogin { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string ResolvedByLogin { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed record AssignmentCaseOption(
    Guid Id,
    string CollectionName,
    string TestCaseName);

public sealed record AssignmentSaveRequest(
    Guid? AssignmentId,
    string ProjectKey,
    string ProjectName,
    string ApplicationVersion,
    string RecipientLogin,
    string AssignedByLogin,
    IReadOnlyCollection<Guid> TestCaseIds,
    Guid BatchId = default);
