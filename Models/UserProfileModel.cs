using System;
using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class UserProfileModel
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string Login { get; set; } =
        string.Empty;

    public string DisplayName { get; set; } =
        string.Empty;

    public string PinSalt { get; set; } =
        string.Empty;

    public string PinHash { get; set; } =
        string.Empty;

    public bool RequiresPinChange { get; set; } =
        true;

    public bool WasPinReset { get; set; }

    public bool SuppressAssignmentCompletionConfirmation { get; set; }

    public bool SuppressAssignedTestsTutorial { get; set; }

    public List<string> SystemRoles { get; set; } =
        new()
        {
            "Tester"
        };

    public List<string> ProjectRoles { get; set; } =
        new();

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.Now;

    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class UserProfilesDataModel
{
    public List<UserProfileModel> Profiles { get; set; } =
        new();

    public List<string> AppliedDataMigrations { get; set; } =
        new();

    public List<ProjectDefinitionModel> Projects { get; set; } =
        new();

    public List<ProjectRoleDefinitionModel> ProjectRoleDefinitions { get; set; } =
        new();
}

public sealed class ProjectDefinitionModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class ProjectRoleDefinitionModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BorderColor { get; set; } = "#7154B8";
    public List<string> ProjectKeys { get; set; } = new();
}

public enum AuthenticationStatus
{
    Success,
    PinWasReset,
    InvalidCredentials
}

public sealed record AuthenticationResult(
    AuthenticationStatus Status,
    UserProfileModel? Profile);
