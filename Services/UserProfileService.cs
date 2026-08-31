using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class UserProfileService
{
    private const string RoleAndProjectDefinitionsMigration =
        "role-and-project-definitions-v1";
    private const string DemoQaRolesMigration =
        "demo-qa-roles-v1";
    private const string ProfessionalRolesMigration =
        "professional-roles-v1";
    private const string DemoProjectAccessMigration =
        "demo-project-access-v1";
    private const string DefaultDemoProfileProjectsMigration =
        "default-demo-profile-projects-v1";
    private const string ExplicitRoleCategoriesMigration =
        "explicit-role-categories-v1";
    private const string ThematicDemoProjectsMigration =
        "thematic-demo-projects-v4-company-domains";
    private const string DefaultRoleColorsMigration =
        "default-role-colors-v1";
    private const string PaymentsProjectRoleScopeMigration =
        "payments-project-role-scope-v1";
    private const string EnglishProjectRolesMigration =
        "english-project-roles-v1";
    private const string EnglishDemoProjectKey =
        "test-project-english";
    private const string LegacyPolishDemoProjectKey =
        "test-project-polish";
    private const string AdminPolishDemoProjectKey =
        "test-admin-polish";
    private const string LeaderPolishDemoProjectKey =
        "test-leader-polish";
    private const string PlantsProjectKey = "plants-polish";
    private const string PlanetariumProjectKey = "planetarium-polish";
    private const string OfficeProjectKey = "office-polish";
    private const string SalesProjectKey = "sales-polish";
    private const string PaymentsProjectKey = "payments-polish";
    private const string AutomotiveProjectKey = "automotive-polish";
    private const string HospitalProjectKey = "hospital-polish";
    private const string DemoLeaderProjectRole =
        "Project Lead";

    private static readonly ProjectRoleDefinitionModel[] DemoQaRoles =
    {
        new() { Name = "QA Analyst", BorderColor = "#2E86D1", IsProfessionalRole = true },
        new() { Name = "Automation Engineer", BorderColor = "#28A06A", IsProfessionalRole = true },
        new() { Name = "Test Architect", BorderColor = "#9A6BE8", IsProfessionalRole = true },
        new() { Name = "Quality Observer", BorderColor = "#F0A34A", IsProfessionalRole = true }
    };

    private static readonly ProjectRoleDefinitionModel[] ProfessionalRoles =
    {
        new()
        {
            Name = "Developer",
            BorderColor = "#1F5F96",
            BackgroundColor = "#2E86D1",
            TextColor = "#FFFFFF",
            IsProfessionalRole = true
        },
        new()
        {
            Name = "Automation Tester",
            BorderColor = "#1C704B",
            BackgroundColor = "#28A06A",
            TextColor = "#FFFFFF",
            IsProfessionalRole = true
        },
        new()
        {
            Name = "Manager",
            BorderColor = "#9B6D10",
            BackgroundColor = "#D99A18",
            TextColor = "#17212B",
            IsProfessionalRole = true
        }
    };

    private static readonly ProjectRoleDefinitionModel[] ThematicProjectRoles =
    {
        new() { Name = "Botanik", BorderColor = "#1C3E31", BackgroundColor = "#315A49", TextColor = "#C8E9DA", IsProfessionalRole = true, ProjectKeys = new() { PlantsProjectKey } },
        new() { Name = "Ogrodnik", BorderColor = "#214448", BackgroundColor = "#35666A", TextColor = "#D5F0F1", IsProfessionalRole = true, ProjectKeys = new() { PlantsProjectKey } },
        new() { Name = "Arborysta", BorderColor = "#2E363C", BackgroundColor = "#4B555D", TextColor = "#E8EDF0", IsProfessionalRole = true, ProjectKeys = new() { PlantsProjectKey } },
        new() { Name = "Liść", BorderColor = "#1C3E31", BackgroundColor = "#315A49", TextColor = "#C8E9DA", IsProfessionalRole = true, ProjectKeys = new() { PlantsProjectKey } },
        new() { Name = "Meteorolog", BorderColor = "#243C69", BackgroundColor = "#3B5683", TextColor = "#E8EEFC", IsProfessionalRole = true, ProjectKeys = new() { PlanetariumProjectKey } },
        new() { Name = "Synoptyk", BorderColor = "#2C4C67", BackgroundColor = "#4B7395", TextColor = "#E0EFFB", IsProfessionalRole = true, ProjectKeys = new() { PlanetariumProjectKey } },
        new() { Name = "Technik stacji pogodowej", BorderColor = "#164F66", BackgroundColor = "#2B6B82", TextColor = "#DEF5FC", IsProfessionalRole = true, ProjectKeys = new() { PlanetariumProjectKey } },
        new() { Name = "Łowca burz", BorderColor = "#3D2D63", BackgroundColor = "#5A4782", TextColor = "#EFE8FA", IsProfessionalRole = true, ProjectKeys = new() { PlanetariumProjectKey } },
        new() { Name = "Urzędnik", BorderColor = "#2E363C", BackgroundColor = "#4B555D", TextColor = "#E8EDF0", IsProfessionalRole = true, ProjectKeys = new() { OfficeProjectKey } },
        new() { Name = "Kierownik referatu", BorderColor = "#2D2340", BackgroundColor = "#4A3A67", TextColor = "#E6DDF4", IsProfessionalRole = true, ProjectKeys = new() { OfficeProjectKey } },
        new() { Name = "Pracownik kancelarii", BorderColor = "#2C4C67", BackgroundColor = "#4B7395", TextColor = "#E0EFFB", IsProfessionalRole = true, ProjectKeys = new() { OfficeProjectKey } },
        new() { Name = "Sprzedawca", BorderColor = "#214448", BackgroundColor = "#35666A", TextColor = "#D5F0F1", IsProfessionalRole = true, ProjectKeys = new() { SalesProjectKey, PaymentsProjectKey } },
        new() { Name = "Dostawca", BorderColor = "#2C4C67", BackgroundColor = "#4B7395", TextColor = "#E0EFFB", IsProfessionalRole = true, ProjectKeys = new() { SalesProjectKey } },
        new() { Name = "Magazynier", BorderColor = "#2E363C", BackgroundColor = "#4B555D", TextColor = "#E8EDF0", IsProfessionalRole = true, ProjectKeys = new() { SalesProjectKey } },
        new() { Name = "Kierownik sklepu", BorderColor = "#2D2340", BackgroundColor = "#4A3A67", TextColor = "#E6DDF4", IsProfessionalRole = true, ProjectKeys = new() { SalesProjectKey } },
        new() { Name = "Bankier", BorderColor = "#2D2340", BackgroundColor = "#4A3A67", TextColor = "#E6DDF4", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Tester QA", BorderColor = "#2C4C67", BackgroundColor = "#4B7395", TextColor = "#E0EFFB", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Analityk płatności", BorderColor = "#1C3E31", BackgroundColor = "#315A49", TextColor = "#C8E9DA", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Kasjer", BorderColor = "#684315", BackgroundColor = "#8D6224", TextColor = "#FFF0C8", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Operator terminala", BorderColor = "#174F68", BackgroundColor = "#2C708E", TextColor = "#E1F4FC", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Specjalista systemów kasowych", BorderColor = "#31496B", BackgroundColor = "#4A668D", TextColor = "#E8F0FC", IsProfessionalRole = true, ProjectKeys = new() { PaymentsProjectKey } },
        new() { Name = "Mechanik", BorderColor = "#424B52", BackgroundColor = "#5C676F", TextColor = "#EEF2F4", IsProfessionalRole = true, ProjectKeys = new() { AutomotiveProjectKey } },
        new() { Name = "Diagnosta samochodowy", BorderColor = "#243C69", BackgroundColor = "#3B5683", TextColor = "#E8EEFC", IsProfessionalRole = true, ProjectKeys = new() { AutomotiveProjectKey } },
        new() { Name = "Doradca serwisowy", BorderColor = "#245374", BackgroundColor = "#3C6F91", TextColor = "#E5F4FC", IsProfessionalRole = true, ProjectKeys = new() { AutomotiveProjectKey } },
        new() { Name = "Kierownik serwisu", BorderColor = "#715211", BackgroundColor = "#936D20", TextColor = "#FFF2C4", IsProfessionalRole = true, ProjectKeys = new() { AutomotiveProjectKey } },
        new() { Name = "Lekarz", BorderColor = "#1E4F78", BackgroundColor = "#315F86", TextColor = "#E4F2FC", IsProfessionalRole = true, ProjectKeys = new() { HospitalProjectKey } },
        new() { Name = "Ratownik medyczny", BorderColor = "#8A2730", BackgroundColor = "#B64049", TextColor = "#FFF0F1", IsProfessionalRole = true, ProjectKeys = new() { HospitalProjectKey } },
        new() { Name = "Pielęgniarka", BorderColor = "#176451", BackgroundColor = "#2D7B67", TextColor = "#E1F7EF", IsProfessionalRole = true, ProjectKeys = new() { HospitalProjectKey } },
        new() { Name = "Rejestrator medyczny", BorderColor = "#3E4B57", BackgroundColor = "#566673", TextColor = "#EDF2F5", IsProfessionalRole = true, ProjectKeys = new() { HospitalProjectKey } }
    };

    private static readonly ProjectRoleDefinitionModel[] EnglishProjectRoles =
    {
        new() { Name = "Localization Tester", BorderColor = "#174F68", BackgroundColor = "#2C708E", TextColor = "#E1F4FC", IsProfessionalRole = true, ProjectKeys = new() { EnglishDemoProjectKey } },
        new() { Name = "English Content Reviewer", BorderColor = "#1C3E31", BackgroundColor = "#315A49", TextColor = "#C8E9DA", IsProfessionalRole = true, ProjectKeys = new() { EnglishDemoProjectKey } },
        new() { Name = "Translation QA", BorderColor = "#3D2D63", BackgroundColor = "#5A4782", TextColor = "#EFE8FA", IsProfessionalRole = true, ProjectKeys = new() { EnglishDemoProjectKey } },
        new() { Name = "Internationalization Engineer", BorderColor = "#243C69", BackgroundColor = "#3B5683", TextColor = "#E8EEFC", IsProfessionalRole = true, ProjectKeys = new() { EnglishDemoProjectKey } },
        new() { Name = "Accessibility Reviewer", BorderColor = "#684315", BackgroundColor = "#8D6224", TextColor = "#FFF0C8", IsProfessionalRole = true, ProjectKeys = new() { EnglishDemoProjectKey } }
    };

    private static readonly string[] PaymentsProjectRoleNames =
    {
        "Analityk płatności",
        "Bankier",
        "Kasjer",
        "Operator terminala",
        "Specjalista systemów kasowych",
        "Sprzedawca",
        "Tester QA"
    };

    private static readonly (string Name, string Border, string Background, string Text)[] DefaultRoleVisuals =
    {
        ("Automation Engineer", "#145A46", "#27745E", "#DDF5EC"),
        ("Automation Tester", "#176451", "#2D7B67", "#E1F7EF"),
        ("Developer", "#1E4F78", "#315F86", "#E4F2FC"),
        ("Manager", "#6B4C0F", "#8C671C", "#FFF0BD"),
        ("Project Lead", "#70500E", "#967022", "#FFF1BE"),
        ("QA Analyst", "#28527A", "#3D6C96", "#E5F2FD"),
        ("Quality Observer", "#5D4A1E", "#7A6532", "#FFF2C7"),
        ("Test Architect", "#3D2D63", "#5A4782", "#EFE8FA"),

        ("Botanik", "#1B5135", "#2F6B49", "#DDF4E6"),
        ("Ogrodnik", "#43551D", "#61752E", "#F0F6D5"),
        ("Arborysta", "#4B3824", "#685039", "#F3E7D8"),
        ("Liść", "#17613A", "#2C7B4E", "#DDF7E8"),

        ("Astronom", "#202F66", "#344983", "#E5EAFF"),
        ("Prezenter planetarium", "#293D76", "#435A96", "#E8EDFF"),
        ("Obsługa planetarium", "#164F66", "#2B6B82", "#DEF5FC"),

        ("Urzędnik", "#3E4B57", "#566673", "#EDF2F5"),
        ("Kierownik referatu", "#38445F", "#53617D", "#EEF1FA"),
        ("Pracownik kancelarii", "#46545E", "#64727C", "#F0F4F6"),

        ("Sprzedawca", "#744019", "#985C2A", "#FFF0E2"),
        ("Dostawca", "#245374", "#3C6F91", "#E5F4FC"),
        ("Magazynier", "#424B52", "#5C676F", "#EEF2F4"),
        ("Kierownik sklepu", "#715211", "#936D20", "#FFF2C4"),

        ("Bankier", "#243C69", "#3B5683", "#E8EEFC"),
        ("Tester QA", "#155B59", "#297673", "#DDF7F4"),
        ("Analityk płatności", "#433060", "#60477C", "#F0E7F8")
    };

    public const string InitialPin =
        "000000";

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly string _profilesFilePath;

    private static readonly SemaphoreSlim SaveLock =
        new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public UserProfileService()
    {
        var applicationDataDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "QARegressionManager");

        Directory.CreateDirectory(
            applicationDataDirectory);

        _profilesFilePath =
            Path.Combine(
                applicationDataDirectory,
                "profiles.json");
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string login,
        string pin)
    {
        var normalizedLogin =
            NormalizeLogin(login);

        if (string.IsNullOrWhiteSpace(normalizedLogin) ||
            !IsValidPin(pin))
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidCredentials,
                null);
        }

        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    string.Equals(
                        item.Login,
                        normalizedLogin,
                        StringComparison.OrdinalIgnoreCase));

        if (profile is null)
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidCredentials,
                null);
        }
        else if (!VerifyPin(
                     pin,
                     profile.PinSalt,
                     profile.PinHash))
        {
            return new AuthenticationResult(
                profile.WasPinReset
                    ? AuthenticationStatus.PinWasReset
                    : AuthenticationStatus.InvalidCredentials,
                profile.WasPinReset
                    ? profile
                    : null);
        }

        profile.LastLoginAt =
            DateTimeOffset.Now;

        EnsureDedicatedAdminRole(
            profile);

        await SaveAsync(
            data);

        return new AuthenticationResult(
            AuthenticationStatus.Success,
            profile);
    }

    public async Task EnsureTestProfilesAsync(
        int count = 9)
    {
        if (count is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Liczba profili testowych musi mieścić się w zakresie 1-99.");
        }

        var data =
            await LoadAsync();

        var profilesAdded =
            false;

        for (var index = 1;
             index <= count;
             index++)
        {
            var login =
                $"tester{index}";

            var profileExists =
                data.Profiles.Any(
                    profile =>
                        string.Equals(
                            profile.Login,
                            login,
                            StringComparison.OrdinalIgnoreCase));

            if (profileExists)
            {
                continue;
            }

            data.Profiles.Add(
                CreateProfile(
                    login,
                    InitialPin));

            profilesAdded =
                true;
        }

        if (profilesAdded)
        {
            await SaveAsync(
                data);
        }
    }

    public async Task EnsureDemoProfilesAsync()
    {
        var data =
            await LoadAsync();

        var demoProfiles =
            new[]
            {
                (Login: "admin", DisplayName: "Admin", Role: SystemRoleService.AdministratorRole),
                (Login: "leader", DisplayName: "Demo Leader", Role: SystemRoleService.LeaderRole),
                (Login: "tester1", DisplayName: "Demo Tester 1", Role: SystemRoleService.TesterRole),
                (Login: "tester2", DisplayName: "Demo Tester 2", Role: SystemRoleService.TesterRole),
                (Login: "tester3", DisplayName: "Demo Tester 3", Role: SystemRoleService.TesterRole)
            };

        data.AppliedDataMigrations ??=
            new();
        var assignDemoQaRoles =
            !data.AppliedDataMigrations.Contains(
                DemoQaRolesMigration,
                StringComparer.OrdinalIgnoreCase);
        var assignDemoProjectAccess =
            !data.AppliedDataMigrations.Contains(
                DemoProjectAccessMigration,
                StringComparer.OrdinalIgnoreCase);
        var assignDefaultDemoProfileProjects =
            !data.AppliedDataMigrations.Contains(
                DefaultDemoProfileProjectsMigration,
                StringComparer.OrdinalIgnoreCase);
        EnsureRoleAndProjectDefinitions(data);

        foreach (var demoProfile in demoProfiles)
        {
            var profile =
                data.Profiles.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Login,
                            demoProfile.Login,
                            StringComparison.OrdinalIgnoreCase));
            var profileCreated =
                profile is null;

            if (profile is null)
            {
                profile =
                    CreateProfile(
                        demoProfile.Login,
                        InitialPin);

                data.Profiles.Add(
                    profile);
            }

            profile.DisplayName =
                demoProfile.DisplayName;

            profile.SystemRoles = string.Equals(
                    profile.Login,
                    "admin",
                    StringComparison.OrdinalIgnoreCase)
                ? SystemRoleService.AvailableSystemRoles.ToList()
                : new() { demoProfile.Role };

            profile.ProjectRoles ??=
                new();

            EnsureDedicatedAdminRole(
                profile);

            if (assignDemoQaRoles &&
                string.Equals(profile.Login, "admin", StringComparison.OrdinalIgnoreCase))
            {
                profile.ProjectRoles = DemoQaRoles
                    .Select(role => role.Name)
                    .ToList();
            }

            if (assignDemoProjectAccess)
            {
                profile.ProjectRoles = profile.Login.ToLowerInvariant() switch
                {
                    "admin" => data.ProjectRoleDefinitions
                        .Select(role => role.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    "leader" => new() { DemoLeaderProjectRole },
                    "tester1" or "tester2" => new(),
                    _ => profile.ProjectRoles
                };
            }

            if (assignDefaultDemoProfileProjects ||
                profileCreated)
            {
                profile.ProjectRoles = profile.Login.ToLowerInvariant() switch
                {
                    "admin" => data.ProjectRoleDefinitions
                        .Select(role => role.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    "leader" => new() { "Specjalista systemów kasowych", "Botanik" },
                    "tester1" => new() { "Tester QA", "Meteorolog" },
                    "tester2" => new() { "Operator terminala", "Mechanik" },
                    "tester3" => new() { "Kasjer", "Lekarz" },
                    _ => profile.ProjectRoles
                };
            }
        }

        var ownerProfile =
            data.Profiles.FirstOrDefault(
                profile =>
                    string.Equals(
                        profile.Login,
                        "epotocki",
                        StringComparison.OrdinalIgnoreCase));
        if (ownerProfile is not null)
        {
            ownerProfile.DisplayName =
                "Eryk Potocki";
        }

        if (assignDemoProjectAccess)
        {
            data.AppliedDataMigrations.Add(
                DemoProjectAccessMigration);
        }

        if (assignDefaultDemoProfileProjects)
        {
            data.AppliedDataMigrations.Add(
                DefaultDemoProfileProjectsMigration);
        }

        await SaveAsync(
            data);
    }

    public async Task<(ProjectDefinitionModel[] Projects, ProjectRoleDefinitionModel[] Roles)> GetRoleAndProjectDefinitionsAsync()
    {
        var data = await LoadAsync();
        EnsureRoleAndProjectDefinitions(data);
        return (
            data.Projects
                .OrderBy(item =>
                    string.Equals(
                        item.Name,
                        DemoCatalog.PaymentsProjectName,
                        StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : DemoCatalog.IsTestProject(item.Name)
                            ? 2
                            : 1)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            data.ProjectRoleDefinitions.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public async Task SaveRoleAndProjectDefinitionsAsync(
        IEnumerable<ProjectDefinitionModel> projects,
        IEnumerable<ProjectRoleDefinitionModel> roles)
    {
        var data = await LoadAsync();
        data.Projects = projects.ToList();
        data.ProjectRoleDefinitions = roles.ToList();

        var validProjectKeys = data.Projects
            .Select(item => item.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var role in data.ProjectRoleDefinitions)
        {
            role.ProjectKeys = role.ProjectKeys
                .Where(validProjectKeys.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var validRoleNames = data.ProjectRoleDefinitions
            .Select(item => item.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in data.Profiles)
        {
            profile.ProjectRoles = profile.ProjectRoles
                .Where(validRoleNames.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        await SaveAsync(data);
    }

    public async Task SetProjectRoleMembersAsync(
        string roleName,
        IEnumerable<string> memberLogins)
    {
        var data = await LoadAsync();
        var members = memberLogins.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in data.Profiles)
        {
            profile.ProjectRoles ??= new();
            profile.ProjectRoles.RemoveAll(role =>
                string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));

            if (members.Contains(profile.Login))
            {
                profile.ProjectRoles.Add(roleName);
            }
        }

        await SaveAsync(data);
    }

    private static void EnsureRoleAndProjectDefinitions(UserProfilesDataModel data)
    {
        data.Projects ??= new();
        data.ProjectRoleDefinitions ??= new();
        data.AppliedDataMigrations ??= new();

        EnsureDemoProjectDefinition(
            data,
            EnglishDemoProjectKey,
            DemoCatalog.PrimaryProjectName);
        EnsureDemoProjectDefinition(data, PlantsProjectKey, DemoCatalog.PlantsProjectName);
        EnsureDemoProjectDefinition(data, PlanetariumProjectKey, DemoCatalog.PlanetariumProjectName);
        EnsureDemoProjectDefinition(data, OfficeProjectKey, DemoCatalog.OfficeProjectName);
        EnsureDemoProjectDefinition(data, SalesProjectKey, DemoCatalog.SalesProjectName);
        EnsureDemoProjectDefinition(data, PaymentsProjectKey, DemoCatalog.PaymentsProjectName);
        EnsureDemoProjectDefinition(data, AutomotiveProjectKey, DemoCatalog.AutomotiveProjectName);
        EnsureDemoProjectDefinition(data, HospitalProjectKey, DemoCatalog.HospitalProjectName);

        data.Projects.RemoveAll(project =>
            string.Equals(
                project.Key,
                LegacyPolishDemoProjectKey,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                project.Key,
                AdminPolishDemoProjectKey,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                project.Key,
                LeaderPolishDemoProjectKey,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                project.Name,
                DemoCatalog.LegacyPolishProjectName,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                project.Name,
                DemoCatalog.AdminPolishProjectName,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                project.Name,
                DemoCatalog.LeaderPolishProjectName,
                StringComparison.OrdinalIgnoreCase));

        foreach (var role in data.ProjectRoleDefinitions)
        {
            role.ProjectKeys.RemoveAll(key =>
                string.Equals(
                    key,
                    LegacyPolishDemoProjectKey,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    key,
                    AdminPolishDemoProjectKey,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    key,
                    LeaderPolishDemoProjectKey,
                    StringComparison.OrdinalIgnoreCase));
        }

        var leaderRole = data.ProjectRoleDefinitions.FirstOrDefault(role =>
            string.Equals(
                role.Name,
                DemoLeaderProjectRole,
                StringComparison.OrdinalIgnoreCase));

        if (leaderRole is null)
        {
            leaderRole = new ProjectRoleDefinitionModel
            {
                Name = DemoLeaderProjectRole,
                BorderColor = "#D99A18",
                IsProfessionalRole = true
            };
            data.ProjectRoleDefinitions.Add(leaderRole);
        }

        if (!data.AppliedDataMigrations.Contains(
                DemoProjectAccessMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            leaderRole.ProjectKeys = new()
            {
                LeaderPolishDemoProjectKey
            };
        }

        if (!data.AppliedDataMigrations.Contains(
                DemoQaRolesMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            foreach (var demoRole in DemoQaRoles)
            {
                if (data.ProjectRoleDefinitions.Any(role =>
                        string.Equals(role.Name, demoRole.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                data.ProjectRoleDefinitions.Add(
                    new ProjectRoleDefinitionModel
                    {
                        Name = demoRole.Name,
                        BorderColor = demoRole.BorderColor,
                        IsProfessionalRole = true,
                        ProjectKeys = new()
                    });
            }

            data.AppliedDataMigrations.Add(DemoQaRolesMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                ProfessionalRolesMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            foreach (var professionalRole in ProfessionalRoles)
            {
                if (!data.ProjectRoleDefinitions.Any(role =>
                        string.Equals(
                            role.Name,
                            professionalRole.Name,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    data.ProjectRoleDefinitions.Add(
                        new ProjectRoleDefinitionModel
                        {
                            Name = professionalRole.Name,
                            BorderColor = professionalRole.BorderColor,
                            BackgroundColor = professionalRole.BackgroundColor,
                            TextColor = professionalRole.TextColor,
                            IsProfessionalRole = true,
                            ProjectKeys = new()
                        });
                }
            }

            var admin = data.Profiles.FirstOrDefault(profile =>
                string.Equals(profile.Login, "admin", StringComparison.OrdinalIgnoreCase));
            if (admin is not null)
            {
                admin.ProjectRoles ??= new();
                foreach (var professionalRole in ProfessionalRoles)
                {
                    if (!admin.ProjectRoles.Contains(
                            professionalRole.Name,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        admin.ProjectRoles.Add(professionalRole.Name);
                    }
                }
            }

            data.AppliedDataMigrations.Add(ProfessionalRolesMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                ExplicitRoleCategoriesMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            foreach (var role in data.ProjectRoleDefinitions)
            {
                role.IsProfessionalRole = true;
            }

            data.AppliedDataMigrations.Add(ExplicitRoleCategoriesMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                ThematicDemoProjectsMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            var replacedPlanetariumRoles = new[]
            {
                "Astronom",
                "Prezenter planetarium",
                "Obsługa planetarium"
            };

            data.ProjectRoleDefinitions.RemoveAll(role =>
                replacedPlanetariumRoles.Contains(
                    role.Name,
                    StringComparer.OrdinalIgnoreCase));

            foreach (var profile in data.Profiles)
            {
                profile.ProjectRoles ??= new();
                profile.ProjectRoles.RemoveAll(role =>
                    replacedPlanetariumRoles.Contains(
                        role,
                        StringComparer.OrdinalIgnoreCase));
            }

            foreach (var thematicRole in ThematicProjectRoles)
            {
                var role = data.ProjectRoleDefinitions.FirstOrDefault(item =>
                    string.Equals(item.Name, thematicRole.Name, StringComparison.OrdinalIgnoreCase));

                if (role is null)
                {
                    role = new ProjectRoleDefinitionModel
                    {
                        Name = thematicRole.Name,
                        BorderColor = thematicRole.BorderColor,
                        BackgroundColor = thematicRole.BackgroundColor,
                        TextColor = thematicRole.TextColor,
                        IsProfessionalRole = true
                    };
                    data.ProjectRoleDefinitions.Add(role);
                }

                role.IsProfessionalRole = true;
                role.ProjectKeys = thematicRole.ProjectKeys.ToList();
            }

            var admin = data.Profiles.FirstOrDefault(profile =>
                string.Equals(profile.Login, "admin", StringComparison.OrdinalIgnoreCase));
            if (admin is not null)
            {
                admin.ProjectRoles ??= new();
                foreach (var role in ThematicProjectRoles)
                {
                    if (!admin.ProjectRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        admin.ProjectRoles.Add(role.Name);
                    }
                }
            }

            var englishRoleNames = new[]
            {
                "Automation Engineer",
                "Automation Tester",
                "Developer",
                "Manager",
                "Project Lead",
                "QA Analyst",
                "Quality Observer",
                "Test Architect"
            };

            foreach (var role in data.ProjectRoleDefinitions.Where(role =>
                         englishRoleNames.Contains(
                             role.Name,
                             StringComparer.OrdinalIgnoreCase)))
            {
                role.ProjectKeys = new() { EnglishDemoProjectKey };
            }

            data.AppliedDataMigrations.Add(ThematicDemoProjectsMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                EnglishProjectRolesMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            var previousEnglishRoleNames = new[]
            {
                "Automation Engineer",
                "Automation Tester",
                "Developer",
                "Manager",
                "Project Lead",
                "QA Analyst",
                "Quality Observer",
                "Test Architect"
            };

            var removedRoleNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var role in data.ProjectRoleDefinitions.Where(role =>
                         previousEnglishRoleNames.Contains(
                             role.Name,
                             StringComparer.OrdinalIgnoreCase)))
            {
                role.ProjectKeys.RemoveAll(projectKey =>
                    string.Equals(
                        projectKey,
                        EnglishDemoProjectKey,
                        StringComparison.OrdinalIgnoreCase));

                if (role.ProjectKeys.Count == 0)
                {
                    removedRoleNames.Add(role.Name);
                }
            }

            data.ProjectRoleDefinitions.RemoveAll(role =>
                removedRoleNames.Contains(role.Name));

            foreach (var profile in data.Profiles)
            {
                profile.ProjectRoles ??= new();
                profile.ProjectRoles.RemoveAll(roleName =>
                    removedRoleNames.Contains(roleName));
            }

            foreach (var template in EnglishProjectRoles)
            {
                var role = data.ProjectRoleDefinitions.FirstOrDefault(item =>
                    string.Equals(
                        item.Name,
                        template.Name,
                        StringComparison.OrdinalIgnoreCase));

                if (role is null)
                {
                    role = new ProjectRoleDefinitionModel
                    {
                        Name = template.Name,
                        BorderColor = template.BorderColor,
                        BackgroundColor = template.BackgroundColor,
                        TextColor = template.TextColor,
                        IsProfessionalRole = true,
                        ProjectKeys = new() { EnglishDemoProjectKey }
                    };
                    data.ProjectRoleDefinitions.Add(role);
                }
                else if (!role.ProjectKeys.Contains(
                             EnglishDemoProjectKey,
                             StringComparer.OrdinalIgnoreCase))
                {
                    role.ProjectKeys.Add(EnglishDemoProjectKey);
                }
            }

            var admin = data.Profiles.FirstOrDefault(profile =>
                string.Equals(
                    profile.Login,
                    "admin",
                    StringComparison.OrdinalIgnoreCase));
            if (admin is not null)
            {
                admin.ProjectRoles ??= new();
                foreach (var role in EnglishProjectRoles)
                {
                    if (!admin.ProjectRoles.Contains(
                            role.Name,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        admin.ProjectRoles.Add(role.Name);
                    }
                }
            }

            data.AppliedDataMigrations.Add(
                EnglishProjectRolesMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                DefaultRoleColorsMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            foreach (var visual in DefaultRoleVisuals)
            {
                var role = data.ProjectRoleDefinitions.FirstOrDefault(item =>
                    string.Equals(item.Name, visual.Name, StringComparison.OrdinalIgnoreCase));
                if (role is null)
                {
                    continue;
                }

                role.BorderColor = visual.Border;
                role.BackgroundColor = visual.Background;
                role.TextColor = visual.Text;
            }

            data.AppliedDataMigrations.Add(DefaultRoleColorsMigration);
        }

        if (!data.AppliedDataMigrations.Contains(
                PaymentsProjectRoleScopeMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            var paymentRoleNames =
                PaymentsProjectRoleNames.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            // Starsze dane demonstracyjne mogły przypisać niemal każdą rolę
            // do PŁATNOŚCI. Najpierw czyścimy ten zakres, a następnie dodajemy
            // wyłącznie stanowiska związane z płatnościami, terminalami i kasą.
            foreach (var role in data.ProjectRoleDefinitions)
            {
                role.ProjectKeys.RemoveAll(key =>
                    string.Equals(
                        key,
                        PaymentsProjectKey,
                        StringComparison.OrdinalIgnoreCase));
            }

            foreach (var template in ThematicProjectRoles.Where(role =>
                         paymentRoleNames.Contains(role.Name)))
            {
                var role = data.ProjectRoleDefinitions.FirstOrDefault(item =>
                    string.Equals(
                        item.Name,
                        template.Name,
                        StringComparison.OrdinalIgnoreCase));

                if (role is null)
                {
                    role = new ProjectRoleDefinitionModel
                    {
                        Name = template.Name,
                        BorderColor = template.BorderColor,
                        BackgroundColor = template.BackgroundColor,
                        TextColor = template.TextColor,
                        IsProfessionalRole = true
                    };
                    data.ProjectRoleDefinitions.Add(role);
                }

                if (!role.ProjectKeys.Contains(
                        PaymentsProjectKey,
                        StringComparer.OrdinalIgnoreCase))
                {
                    role.ProjectKeys.Add(PaymentsProjectKey);
                }
            }

            var admin = data.Profiles.FirstOrDefault(profile =>
                string.Equals(
                    profile.Login,
                    "admin",
                    StringComparison.OrdinalIgnoreCase));

            if (admin is not null)
            {
                admin.ProjectRoles ??= new();
                foreach (var roleName in PaymentsProjectRoleNames)
                {
                    if (!admin.ProjectRoles.Contains(
                            roleName,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        admin.ProjectRoles.Add(roleName);
                    }
                }
            }

            data.AppliedDataMigrations.Add(
                PaymentsProjectRoleScopeMigration);
        }

        data.AppliedDataMigrations.Add(RoleAndProjectDefinitionsMigration);
    }

    private static void EnsureDemoProjectDefinition(
        UserProfilesDataModel data,
        string projectKey,
        string projectName)
    {
        var project = data.Projects.FirstOrDefault(item =>
            string.Equals(item.Name, projectName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Key, projectKey, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            data.Projects.Add(
                new ProjectDefinitionModel
                {
                    Key = projectKey,
                    Name = projectName
                });
            return;
        }

        var previousKey = project.Key;
        project.Key = projectKey;
        project.Name = projectName;

        if (string.Equals(previousKey, projectKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var role in data.ProjectRoleDefinitions)
        {
            for (var index = 0; index < role.ProjectKeys.Count; index++)
            {
                if (string.Equals(
                        role.ProjectKeys[index],
                        previousKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    role.ProjectKeys[index] = projectKey;
                }
            }
        }
    }

    public async Task ChangePinAsync(
        Guid profileId,
        string newPin)
    {
        if (!IsValidPin(newPin))
        {
            throw new ArgumentException(
                "PIN musi składać się z dokładnie 6 cyfr.",
                nameof(newPin));
        }

        if (newPin == InitialPin)
        {
            throw new ArgumentException(
                "Nowy PIN nie może być domyślnym PIN-em 000000.",
                nameof(newPin));
        }

        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    item.Id == profileId)
            ?? throw new InvalidOperationException(
                "Nie znaleziono profilu użytkownika.");

        SetPin(
            profile,
            newPin);

        profile.RequiresPinChange =
            false;

        profile.WasPinReset =
            false;

        await SaveAsync(
            data);
    }

    public async Task<UserProfileModel[]> GetProfilesAsync()
    {
        var data =
            await LoadAsync();

        var rolesUpdated =
            false;

        foreach (var profile in data.Profiles)
        {
            rolesUpdated |=
                EnsureDedicatedAdminRole(
                    profile);
        }

        if (rolesUpdated)
        {
            await SaveAsync(
                data);
        }

        return data.Profiles
            .OrderBy(
                profile =>
                    string.Equals(
                        profile.Login,
                        "epotocki",
                        StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : 1)
            .ThenBy(
                profile =>
                    profile.Login,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<UserProfileModel[]> GetProfilesWithAccessToProjectAsync(
        string projectKey)
    {
        if (string.IsNullOrWhiteSpace(projectKey))
        {
            return Array.Empty<UserProfileModel>();
        }

        var data =
            await LoadAsync();
        var normalizedProjectKey =
            projectKey.Trim();
        var rolesUpdated =
            false;

        foreach (var profile in data.Profiles)
        {
            rolesUpdated |=
                EnsureDedicatedAdminRole(
                    profile);
        }

        if (rolesUpdated)
        {
            await SaveAsync(
                data);
        }

        return data.Profiles
            .Where(profile =>
                HasAccessToProject(
                    profile,
                    data.ProjectRoleDefinitions,
                    normalizedProjectKey))
            .OrderBy(profile =>
                string.Equals(
                    profile.Login,
                    "epotocki",
                    StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .ThenBy(
                profile => profile.Login,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool HasAccessToProject(
        UserProfileModel profile,
        IEnumerable<ProjectRoleDefinitionModel> roleDefinitions,
        string projectKey)
    {
        if (profile.SystemRoles?.Contains(
                SystemRoleService.AdministratorRole,
                StringComparer.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        var profileRoleNames =
            (profile.ProjectRoles ?? new List<string>()).ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        return roleDefinitions.Any(role =>
            profileRoleNames.Contains(role.Name) &&
            role.ProjectKeys?.Contains(
                projectKey,
                StringComparer.OrdinalIgnoreCase) == true);
    }

    public async Task<bool> GetSuppressAssignmentCompletionConfirmationAsync(
        string login)
    {
        var data =
            await LoadAsync();

        return data.Profiles
                   .FirstOrDefault(
                       profile =>
                           string.Equals(
                               profile.Login,
                               NormalizeLogin(login),
                               StringComparison.OrdinalIgnoreCase))
                   ?.SuppressAssignmentCompletionConfirmation == true;
    }

    public async Task SetSuppressAssignmentCompletionConfirmationAsync(
        string login,
        bool suppress)
    {
        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    string.Equals(
                        item.Login,
                        NormalizeLogin(login),
                        StringComparison.OrdinalIgnoreCase));

        if (profile is null)
        {
            return;
        }

        profile.SuppressAssignmentCompletionConfirmation =
            suppress;

        await SaveAsync(
            data);
    }

    public async Task<bool> GetSuppressAssignedTestsTutorialAsync(
        string login)
    {
        var data = await LoadAsync();

        return data.Profiles
                   .FirstOrDefault(
                       profile =>
                           string.Equals(
                               profile.Login,
                               NormalizeLogin(login),
                               StringComparison.OrdinalIgnoreCase))
                   ?.SuppressAssignedTestsTutorial == true;
    }

    public async Task SetSuppressAssignedTestsTutorialAsync(
        string login,
        bool suppress)
    {
        var data = await LoadAsync();
        var profile = data.Profiles.FirstOrDefault(
            item =>
                string.Equals(
                    item.Login,
                    NormalizeLogin(login),
                    StringComparison.OrdinalIgnoreCase));

        if (profile is null)
        {
            return;
        }

        profile.SuppressAssignedTestsTutorial = suppress;
        await SaveAsync(data);
    }

    public async Task ResetPinAsync(
        Guid profileId)
    {
        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    item.Id ==
                    profileId)
            ?? throw new InvalidOperationException(
                "Nie znaleziono profilu użytkownika.");

        SetPin(
            profile,
            InitialPin);

        profile.RequiresPinChange =
            true;

        profile.WasPinReset =
            true;

        await SaveAsync(
            data);
    }

    public async Task UpdateRolesAsync(
        Guid profileId,
        string[] systemRoles,
        string[] projectRoles)
    {
        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    item.Id ==
                    profileId)
            ?? throw new InvalidOperationException(
                "Nie znaleziono profilu użytkownika.");

        var normalizedSystemRoles =
            systemRoles
                .Where(
                    role =>
                        SystemRoleService.AvailableSystemRoles.Contains(
                            role,
                            StringComparer.OrdinalIgnoreCase))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (normalizedSystemRoles.Count == 0)
        {
            normalizedSystemRoles.Add(
                SystemRoleService.TesterRole);
        }

        if (string.Equals(
                profile.Login,
                "admin",
                StringComparison.OrdinalIgnoreCase) &&
            !normalizedSystemRoles.Contains(
                SystemRoleService.AdministratorRole,
                StringComparer.OrdinalIgnoreCase))
        {
            normalizedSystemRoles.Insert(
                0,
                SystemRoleService.AdministratorRole);
        }

        profile.SystemRoles =
            normalizedSystemRoles;

        profile.ProjectRoles =
            projectRoles
                .Where(
                    role =>
                        !string.IsNullOrWhiteSpace(role))
                .Select(
                    role =>
                        role.Trim().ToUpperInvariant())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        await SaveAsync(
            data);
    }

    public async Task<int> ResetAllPinsAsync()
    {
        var data =
            await LoadAsync();

        foreach (var profile in data.Profiles)
        {
            SetPin(
                profile,
                InitialPin);

            profile.RequiresPinChange =
                true;

            profile.WasPinReset =
                true;
        }

        await SaveAsync(
            data);

        return data.Profiles.Count;
    }

    public async Task<int> ResetAllProfilesForTestAsync()
    {
        var data = await LoadAsync();

        foreach (var profile in data.Profiles)
        {
            SetPin(profile, InitialPin);
            profile.RequiresPinChange = true;
            profile.WasPinReset = true;
            profile.SuppressAssignmentCompletionConfirmation = false;
            profile.SuppressAssignedTestsTutorial = false;
            profile.LastLoginAt = null;
        }

        foreach (var project in data.Projects)
        {
            if (string.Equals(project.Key, EnglishDemoProjectKey, StringComparison.OrdinalIgnoreCase))
            {
                project.Name = DemoCatalog.PrimaryProjectName;
            }
        }

        await SaveAsync(data);
        return data.Profiles.Count;
    }

    public async Task<UserProfileModel> CreateUserAsync(
        string login)
    {
        var normalizedLogin =
            NormalizeLogin(
                login);

        if (normalizedLogin.Length is < 3 or > 40 ||
            normalizedLogin.Any(
                character =>
                    !char.IsLetterOrDigit(
                        character) &&
                    character is not '.' and not '-' and not '_'))
        {
            throw new ArgumentException(
                "Login musi mieć 3-40 znaków i może zawierać litery, cyfry, kropkę, myślnik lub podkreślenie.",
                nameof(login));
        }

        var data =
            await LoadAsync();

        if (data.Profiles.Any(
                profile =>
                    string.Equals(
                        profile.Login,
                        normalizedLogin,
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Profil o takim loginie już istnieje.");
        }

        var profile =
            CreateProfile(
                normalizedLogin,
                InitialPin);

        data.Profiles.Add(
            profile);

        await SaveAsync(
            data);

        return profile;
    }

    public async Task DeleteUserAsync(
        Guid profileId,
        string currentLogin)
    {
        var data =
            await LoadAsync();

        var profile =
            data.Profiles.FirstOrDefault(
                item =>
                    item.Id == profileId)
            ?? throw new InvalidOperationException(
                "Nie znaleziono profilu użytkownika.");

        if (string.Equals(
                profile.Login,
                "admin",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                profile.Login,
                "epotocki",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Konta admin i epotocki są chronione i nie mogą zostać usunięte.");
        }

        if (string.Equals(
                profile.Login,
                currentLogin,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Nie można usunąć aktualnie zalogowanego profilu.");
        }

        data.Profiles.Remove(
            profile);

        await SaveAsync(
            data);
    }

    public static bool IsValidPin(
        string pin)
    {
        return pin.Length == 6 &&
               pin.All(char.IsDigit);
    }

    private async Task<UserProfilesDataModel> LoadAsync()
    {
        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            return await SharedDocumentStore.LoadAsync<UserProfilesDataModel>(
                SharedDocumentStore.ProfilesDocument,
                _profilesFilePath);
        }

        if (!File.Exists(
                _profilesFilePath))
        {
            return new UserProfilesDataModel();
        }

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    _profilesFilePath);

            return JsonSerializer.Deserialize<UserProfilesDataModel>(
                       json,
                       _jsonOptions)
                   ?? new UserProfilesDataModel();
        }
        catch (JsonException)
        {
            return new UserProfilesDataModel();
        }
    }

    private async Task SaveAsync(
        UserProfilesDataModel data)
    {
        if (await SharedDocumentStore.UsesNetworkAsync())
        {
            await SharedDocumentStore.SaveAsync(
                SharedDocumentStore.ProfilesDocument,
                _profilesFilePath,
                data);
            return;
        }

        await SaveLock.WaitAsync();

        try
        {
            var json =
                JsonSerializer.Serialize(
                    data,
                    _jsonOptions);

            var temporaryFilePath =
                _profilesFilePath +
                "." +
                Guid.NewGuid().ToString("N") +
                ".tmp";

            try
            {
                await File.WriteAllTextAsync(
                    temporaryFilePath,
                    json);

                File.Move(
                    temporaryFilePath,
                    _profilesFilePath,
                    true);
            }
            finally
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
        }
        finally
        {
            SaveLock.Release();
        }
    }

    private static UserProfileModel CreateProfile(
        string login,
        string initialPin)
    {
        var profile =
            new UserProfileModel
            {
                Login =
                    login,

                DisplayName =
                    login,

                ProjectRoles = new()
            };

        SetPin(
            profile,
            initialPin);

        return profile;
    }

    private static bool EnsureDedicatedAdminRole(
        UserProfileModel profile)
    {
        profile.SystemRoles ??=
            new();

        if (!string.Equals(
                profile.Login,
                "admin",
                StringComparison.OrdinalIgnoreCase) ||
            profile.SystemRoles.Contains(
                "Administrator",
                StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        profile.SystemRoles.Add(
            "Administrator");

        return true;
    }

    private static void SetPin(
        UserProfileModel profile,
        string pin)
    {
        var salt =
            RandomNumberGenerator.GetBytes(
                SaltSize);

        var hash =
            HashPin(
                pin,
                salt);

        profile.PinSalt =
            Convert.ToBase64String(
                salt);

        profile.PinHash =
            Convert.ToBase64String(
                hash);
    }

    private static bool VerifyPin(
        string pin,
        string saltText,
        string hashText)
    {
        try
        {
            var salt =
                Convert.FromBase64String(
                    saltText);

            var expectedHash =
                Convert.FromBase64String(
                    hashText);

            var actualHash =
                HashPin(
                    pin,
                    salt);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] HashPin(
        string pin,
        byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }

    private static string NormalizeLogin(
        string login)
    {
        return login
            .Trim()
            .ToLowerInvariant();
    }
}
