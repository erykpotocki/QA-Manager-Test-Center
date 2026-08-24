namespace QARegressionManager.Services;

public static class DemoCatalog
{
    public const string PrimaryProjectName = "TEST PROJECT — ENGLISH";
    public const string SecondaryProjectName = "TEST PROJECT — POLISH";

    public static bool IsEnglishProject(string projectName) =>
        string.Equals(
            projectName,
            PrimaryProjectName,
            System.StringComparison.OrdinalIgnoreCase);
}
