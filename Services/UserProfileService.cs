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
    private const string RemoveNovaProjectRoleMigration =
        "remove-nova-project-role-v1";

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
                (Login: "admin", DisplayName: "Demo Administrator", Role: SystemRoleService.AdministratorRole),
                (Login: "leader", DisplayName: "Demo Leader", Role: SystemRoleService.LeaderRole),
                (Login: "tester1", DisplayName: "Demo Tester 1", Role: SystemRoleService.TesterRole),
                (Login: "tester2", DisplayName: "Demo Tester 2", Role: SystemRoleService.TesterRole)
            };

        data.AppliedDataMigrations ??=
            new();
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

            profile.SystemRoles = new() { demoProfile.Role };

            profile.ProjectRoles ??=
                new();

            EnsureDedicatedAdminRole(
                profile);
        }

        if (!data.AppliedDataMigrations.Contains(
                RemoveNovaProjectRoleMigration,
                StringComparer.OrdinalIgnoreCase))
        {
            foreach (var profile in data.Profiles)
            {
                profile.ProjectRoles.RemoveAll(role =>
                    string.Equals(role, "NOVA", StringComparison.OrdinalIgnoreCase));
            }

            data.ProjectRoleDefinitions.RemoveAll(role =>
                string.Equals(role.Name, "NOVA", StringComparison.OrdinalIgnoreCase));
            data.AppliedDataMigrations.Add(RemoveNovaProjectRoleMigration);
        }

        await SaveAsync(
            data);
    }

    public async Task<(ProjectDefinitionModel[] Projects, ProjectRoleDefinitionModel[] Roles)> GetRoleAndProjectDefinitionsAsync()
    {
        var data = await LoadAsync();
        EnsureRoleAndProjectDefinitions(data);
        return (
            data.Projects.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            data.ProjectRoleDefinitions.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public async Task SaveRoleAndProjectDefinitionsAsync(
        IEnumerable<ProjectDefinitionModel> projects,
        IEnumerable<ProjectRoleDefinitionModel> roles)
    {
        var data = await LoadAsync();
        data.Projects = projects.ToList();
        data.ProjectRoleDefinitions = roles.ToList();

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

        data.Projects.AddRange(new[]
        {
            new ProjectDefinitionModel { Key = "nova-pay-demo", Name = DemoCatalog.PrimaryProjectName },
            new ProjectDefinitionModel { Key = "nova-pay-sandbox", Name = DemoCatalog.SecondaryProjectName }
        }.Where(project => data.Projects.All(existing =>
            !string.Equals(existing.Key, project.Key, StringComparison.OrdinalIgnoreCase))));

        foreach (var project in data.Projects)
        {
            if (string.Equals(project.Key, "nova-pay-demo", StringComparison.OrdinalIgnoreCase))
            {
                project.Name = DemoCatalog.PrimaryProjectName;
            }
            else if (string.Equals(project.Key, "nova-pay-sandbox", StringComparison.OrdinalIgnoreCase))
            {
                project.Name = DemoCatalog.SecondaryProjectName;
            }
        }

        data.ProjectRoleDefinitions.RemoveAll(role =>
            string.Equals(role.Name, "NOVA", StringComparison.OrdinalIgnoreCase));

        data.AppliedDataMigrations.Add(RoleAndProjectDefinitionsMigration);
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
                    profile.Login,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
            if (string.Equals(project.Key, "nova-pay-demo", StringComparison.OrdinalIgnoreCase))
            {
                project.Name = DemoCatalog.PrimaryProjectName;
            }
            else if (string.Equals(project.Key, "nova-pay-sandbox", StringComparison.OrdinalIgnoreCase))
            {
                project.Name = DemoCatalog.SecondaryProjectName;
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
