using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class DemoDataSeedService
{
    private const string MigrationPrefix = "public-bilingual-demo-v2";

    public static bool EnsureSeeded(
        UserTestDataModel data,
        string projectKey,
        string projectName)
    {
        data.AppliedDataMigrations ??= new();

        if (!string.Equals(projectName, DemoCatalog.PrimaryProjectName, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(projectName, DemoCatalog.SecondaryProjectName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var migrationId = $"{MigrationPrefix}:{projectKey}";
        if (data.AppliedDataMigrations.Contains(migrationId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var seed =
            Build(
                projectKey,
                DemoCatalog.IsEnglishProject(projectName));

        ApplySeed(
            data,
            seed);
        data.AppliedDataMigrations.Add(migrationId);
        return true;
    }

    private static SeedData Build(
        string projectKey,
        bool english)
    {
        var seed = new SeedData();
        var functionalAreas = english
            ? BuildEnglishAreas()
            : BuildPolishAreas();

        AddTestType(seed, projectKey, "functional", "functional-root", functionalAreas, false, english);

        var regressionAreas = functionalAreas.Select(area =>
            new Area(
                area.Name,
                english
                    ? $"A compact regression check for the {area.Name} area."
                    : $"Skrócona kontrola regresji obszaru {area.Name}.",
                new[]
                {
                    new Collection(
                        english
                            ? $"{area.Name} regression"
                            : $"Regresja {area.Name}",
                        new[]
                        {
                            english
                                ? $"Critical {area.Name} flow"
                                : $"Kluczowy przebieg obszaru {area.Name}"
                        })
                }))
            .ToArray();

        AddTestType(seed, projectKey, "regression", "regression-root", regressionAreas, true, english);
        return seed;
    }

    private static Area[] BuildPolishAreas() =>
        new[]
        {
            new Area("Interfejs użytkownika", "Czytelność i obsługa podstawowych ekranów płatności.", new[]
            {
                new Collection("Ekran płatności", new[] { "Rozpoczęcie płatności", "Anulowanie operacji", "Komunikat wyniku" })
            }),
            new Area("Płatności kartowe", "Podstawowe sposoby realizacji płatności kartą.", new[]
            {
                new Collection("Karta zbliżeniowa", new[] { "Płatność zbliżeniowa", "Płatność zbliżeniowa z potwierdzeniem PIN" }),
                new Collection("Karta z chipem", new[] { "Płatność kartą chipową", "Odrzucenie niepoprawnego PIN" }),
                new Collection("Pasek magnetyczny", new[] { "Płatność kartą magnetyczną" })
            }),
            new Area("Portfele cyfrowe", "Płatności realizowane urządzeniami i portfelami cyfrowymi.", new[]
            {
                new Collection("Płatności mobilne", new[] { "Płatność telefonem", "Płatność zegarkiem" }),
                new Collection("Portfele internetowe", new[] { "Płatność portfelem cyfrowym", "Anulowanie płatności portfelem" })
            }),
            new Area("Obsługa transakcji", "Operacje wykonywane po autoryzacji płatności.", new[]
            {
                new Collection("Zwroty", new[] { "Zwrot pełny", "Zwrot częściowy" }),
                new Collection("Raporty", new[] { "Raport dzienny", "Historia transakcji" })
            }),
            new Area("Łączność i odporność", "Zachowanie aplikacji w sytuacjach przerwania pracy.", new[]
            {
                new Collection("Połączenie", new[] { "Brak połączenia podczas płatności", "Ponowienie połączenia" }),
                new Collection("Restart", new[] { "Zachowanie danych po restarcie" })
            }),
            new Area("Bezpieczeństwo", "Podstawowe kontrole autoryzacji użytkownika.", new[]
            {
                new Collection("Autoryzacja", new[] { "Wymaganie kodu PIN", "Odrzucenie błędnego kodu PIN" })
            })
        };

    private static Area[] BuildEnglishAreas() =>
        new[]
        {
            new Area("User interface", "Readability and operation of the primary payment screens.", new[]
            {
                new Collection("Payment screen", new[] { "Start a payment", "Cancel an operation", "Result message" })
            }),
            new Area("Card payments", "Primary card payment methods.", new[]
            {
                new Collection("Contactless card", new[] { "Contactless payment", "Contactless payment with PIN confirmation" }),
                new Collection("Chip card", new[] { "Chip card payment", "Reject an invalid PIN" }),
                new Collection("Magnetic stripe", new[] { "Magnetic stripe payment" })
            }),
            new Area("Digital wallets", "Payments made with devices and digital wallets.", new[]
            {
                new Collection("Mobile payments", new[] { "Phone payment", "Smartwatch payment" }),
                new Collection("Online wallets", new[] { "Digital wallet payment", "Cancel a wallet payment" })
            }),
            new Area("Transaction handling", "Operations performed after payment authorization.", new[]
            {
                new Collection("Refunds", new[] { "Full refund", "Partial refund" }),
                new Collection("Reports", new[] { "Daily report", "Transaction history" })
            }),
            new Area("Connectivity and resilience", "Application behavior when work is interrupted.", new[]
            {
                new Collection("Connection", new[] { "Connection unavailable during payment", "Retry the connection" }),
                new Collection("Restart", new[] { "Preserve data after restart" })
            }),
            new Area("Security", "Primary user authorization controls.", new[]
            {
                new Collection("Authorization", new[] { "Require a PIN", "Reject an invalid PIN" })
            })
        };

    private static void AddTestType(
        SeedData seed,
        string projectKey,
        string testTypeKey,
        string rootKey,
        IReadOnlyList<Area> areas,
        bool regression,
        bool english)
    {
        for (var areaIndex = 0; areaIndex < areas.Count; areaIndex++)
        {
            var area = areas[areaIndex];
            var folderKey = $"demo-{testTypeKey}-area-{areaIndex + 1}";
            seed.Folders.Add(new TestSectionModel
            {
                Id = StableId($"folder:{projectKey}:{folderKey}"),
                ProjectKey = projectKey,
                TestTypeKey = testTypeKey,
                SectionKey = folderKey,
                ParentSectionKey = rootKey,
                Name = area.Name,
                CreatedByLogin = "admin",
                IsSystem = true,
                RequiresManagerRole = true,
                SortOrder = (areaIndex + 1) * 1000
            });

            for (var collectionIndex = 0; collectionIndex < area.Collections.Count; collectionIndex++)
            {
                var collection = area.Collections[collectionIndex];
                var collectionKey = $"{folderKey}-collection-{collectionIndex + 1}";
                seed.Collections.Add(new TestCollectionModel
                {
                    Id = StableId($"collection:{projectKey}:{collectionKey}"),
                    ProjectKey = projectKey,
                    TestTypeKey = testTypeKey,
                    ParentFolderKey = folderKey,
                    CollectionKey = collectionKey,
                    Name = collection.Name,
                    Description = area.Description,
                    CreatedByLogin = "admin",
                    IsSystem = true,
                    RequiresManagerRole = true,
                    SortOrder = (collectionIndex + 1) * 1000
                });

                for (var caseIndex = 0; caseIndex < collection.Cases.Count; caseIndex++)
                {
                    var caseName = collection.Cases[caseIndex];
                    seed.TestCases.Add(new TestCaseModel
                    {
                        Id = StableId($"case:{projectKey}:{collectionKey}:{caseIndex + 1}"),
                        ProjectKey = projectKey,
                        TestTypeKey = testTypeKey,
                        SectionKey = collectionKey,
                        Name = caseName,
                        CreatedByLogin = "admin",
                        SortOrder = (caseIndex + 1) * 1000,
                        Summary = regression
                            ? english
                                ? $"Confirm the critical flow: {caseName}."
                                : $"Potwierdź najważniejszy przebieg. {caseName}."
                            : english
                                ? $"Verify the scenario: {caseName}."
                                : $"Zweryfikuj scenariusz. {caseName}.",
                        Preconditions = english
                            ? "The demonstration application is running and ready for testing."
                            : "Aplikacja demonstracyjna jest uruchomiona i gotowa do testu.",
                        Importance = regression
                            ? english ? "High" : "Wysoki"
                            : english ? "Medium" : "Średni",
                        ExecutionType = english ? "Manual" : "Manualny",
                        Platforms = new List<string> { "Demo Terminal" },
                        Steps = new List<TestStepModel>
                        {
                            new()
                            {
                                Number = 1,
                                Actions = english
                                    ? $"Execute the scenario: {caseName}."
                                    : $"Wykonaj scenariusz. {caseName}.",
                                ExpectedResults = english
                                    ? "The operation finishes with the expected result and a clear message."
                                    : "Operacja kończy się przewidywalnym wynikiem i czytelnym komunikatem.",
                                ExecutionType = english ? "Manual" : "Manualny"
                            }
                        }
                    });
                }
            }
        }
    }

    private static Guid StableId(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static void ApplySeed(
        UserTestDataModel data,
        SeedData seed)
    {
        foreach (var desired in seed.Folders)
        {
            var existing =
                data.Folders.FirstOrDefault(
                    item => item.Id == desired.Id);

            if (existing is null)
            {
                data.Folders.Add(desired);
                continue;
            }

            existing.ProjectKey = desired.ProjectKey;
            existing.TestTypeKey = desired.TestTypeKey;
            existing.SectionKey = desired.SectionKey;
            existing.ParentSectionKey = desired.ParentSectionKey;
            existing.Name = desired.Name;
            existing.CreatedByLogin = desired.CreatedByLogin;
            existing.IsSystem = desired.IsSystem;
            existing.RequiresManagerRole = desired.RequiresManagerRole;
            existing.SortOrder = desired.SortOrder;
        }

        foreach (var desired in seed.Collections)
        {
            var existing =
                data.Collections.FirstOrDefault(
                    item => item.Id == desired.Id);

            if (existing is null)
            {
                data.Collections.Add(desired);
                continue;
            }

            existing.ProjectKey = desired.ProjectKey;
            existing.TestTypeKey = desired.TestTypeKey;
            existing.ParentFolderKey = desired.ParentFolderKey;
            existing.CollectionKey = desired.CollectionKey;
            existing.Name = desired.Name;
            existing.Description = desired.Description;
            existing.CreatedByLogin = desired.CreatedByLogin;
            existing.IsSystem = desired.IsSystem;
            existing.RequiresManagerRole = desired.RequiresManagerRole;
            existing.SortOrder = desired.SortOrder;
        }

        foreach (var desired in seed.TestCases)
        {
            var existing =
                data.TestCases.FirstOrDefault(
                    item => item.Id == desired.Id);

            if (existing is null)
            {
                data.TestCases.Add(desired);
                continue;
            }

            existing.ProjectKey = desired.ProjectKey;
            existing.TestTypeKey = desired.TestTypeKey;
            existing.SectionKey = desired.SectionKey;
            existing.Name = desired.Name;
            existing.CreatedByLogin = desired.CreatedByLogin;
            existing.SortOrder = desired.SortOrder;
            existing.Summary = desired.Summary;
            existing.Preconditions = desired.Preconditions;
            existing.Importance = desired.Importance;
            existing.ExecutionType = desired.ExecutionType;
            existing.Platforms = desired.Platforms.ToList();
            existing.Steps = desired.Steps
                .Select(
                    step => new TestStepModel
                    {
                        Number = step.Number,
                        Actions = step.Actions,
                        ExpectedResults = step.ExpectedResults,
                        ExecutionType = step.ExecutionType
                    })
                .ToList();
        }
    }

    private sealed record Area(string Name, string Description, IReadOnlyList<Collection> Collections);
    private sealed record Collection(string Name, IReadOnlyList<string> Cases);

    private sealed class SeedData
    {
        public List<TestSectionModel> Folders { get; } = new();
        public List<TestCollectionModel> Collections { get; } = new();
        public List<TestCaseModel> TestCases { get; } = new();
    }
}
