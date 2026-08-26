using System;
using System.Collections.Generic;
using System.Linq;

namespace QARegressionManager.Services;

public static class SystemRoleService
{
    public const string AdministratorRole =
        "Administrator";

    public const string LeaderRole =
        "Lider";

    public const string TesterRole =
        "Tester";

    public static readonly string[] AvailableSystemRoles =
    {
        AdministratorRole,
        LeaderRole,
        TesterRole
    };

    private static readonly string[] RolePriority =
    {
        AdministratorRole,
        LeaderRole,
        TesterRole
    };

    public static string GetHighestRole(
        IEnumerable<string>? systemRoles)
    {
        var roles =
            systemRoles?
                .Where(
                    role =>
                        !string.IsNullOrWhiteSpace(
                            role))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var role in RolePriority)
        {
            if (roles.Contains(
                    role))
            {
                return role;
            }
        }

        return TesterRole;
    }

    public static IReadOnlyList<string> GetOrderedDisplayRoles(
        IEnumerable<string>? systemRoles,
        IEnumerable<string>? projectRoles)
    {
        var systemRoleSet =
            systemRoles?
                .Where(
                    role =>
                        !string.IsNullOrWhiteSpace(
                            role))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        if (systemRoleSet.Count == 0)
        {
            systemRoleSet.Add(
                TesterRole);
        }

        var result =
            RolePriority
                .Where(
                    systemRoleSet.Contains)
                .Select(
                    GetDisplayName)
                .ToList();

        result.AddRange(
            projectRoles?
                .Where(
                    role =>
                        !string.IsNullOrWhiteSpace(role))
                .Select(
                    role =>
                        role.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    role =>
                        role,
                    StringComparer.OrdinalIgnoreCase)
            ?? Enumerable.Empty<string>());

        return result;
    }

    public static string GetDisplayName(
        string role)
    {
        if (string.Equals(
                role,
                AdministratorRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return "Admin";
        }

        if (string.Equals(
                role,
                LeaderRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return "Przełożony";
        }

        if (string.Equals(
                role,
                TesterRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return "Pracownik";
        }

        return role;
    }
}
