namespace QARegressionManager.Services;

public static class DemoCatalog
{
    public const string PrimaryProjectName = "ENGLISH.COM";
    public const string LegacyPolishProjectName = "TEST PROJECT — POLISH";
    public const string AdminPolishProjectName = "TEST ADMIN — POLISH";
    public const string LeaderPolishProjectName = "TEST LEADER — POLISH";
    public const string PlantsProjectName = "OGRODY.PL";
    public const string PlanetariumProjectName = "POGODA.PL";
    public const string OfficeProjectName = "E-URZĄD.PL";
    public const string SalesProjectName = "OWOCE.PL";
    public const string PaymentsProjectName = "TERMINALE.PL";
    public const string AutomotiveProjectName = "SAMOCHODY.PL";
    public const string HospitalProjectName = "SZPITAL.PL";

    public static bool IsDemoProject(string projectName) =>
        string.Equals(projectName, PrimaryProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, AdminPolishProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, LeaderPolishProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, PlantsProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, PlanetariumProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, OfficeProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, SalesProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, PaymentsProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, AutomotiveProjectName, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(projectName, HospitalProjectName, System.StringComparison.OrdinalIgnoreCase);

    public static bool IsTestProject(string projectName) =>
        !string.IsNullOrWhiteSpace(projectName) &&
        (projectName.TrimStart().StartsWith(
             "TEST ",
             System.StringComparison.OrdinalIgnoreCase) ||
         IsEnglishProject(projectName));

    public static bool IsEnglishProject(string projectName) =>
        string.Equals(
            projectName,
            PrimaryProjectName,
            System.StringComparison.OrdinalIgnoreCase);
}
