using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class DemoDataSeedService
{
    private const string MigrationPrefix = "nova-pay-public-demo-v1";

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

        var seed = Build(projectKey);
        var existingFolderKeys = data.Folders
            .Where(item => string.Equals(item.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.SectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCollectionKeys = data.Collections
            .Where(item => string.Equals(item.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.CollectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCaseIds = data.TestCases
            .Where(item => string.Equals(item.ProjectKey, projectKey, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Id)
            .ToHashSet();

        // Migracja katalogu demonstracyjnego nie może usuwać elementów
        // utworzonych wcześniej przez użytkownika. Dodajemy wyłącznie brakujące
        // elementy o stabilnych kluczach i identyfikatorach.
        data.Folders.AddRange(seed.Folders.Where(item => existingFolderKeys.Add(item.SectionKey)));
        data.Collections.AddRange(seed.Collections.Where(item => existingCollectionKeys.Add(item.CollectionKey)));
        data.TestCases.AddRange(seed.TestCases.Where(item => existingCaseIds.Add(item.Id)));
        data.AppliedDataMigrations.Add(migrationId);
        return true;
    }

    private static SeedData Build(string projectKey)
    {
        var seed = new SeedData();
        var functionalAreas = new[]
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

        AddTestType(seed, projectKey, "functional", "functional-root", functionalAreas, false);

        var regressionAreas = functionalAreas.Select(area =>
            new Area(
                area.Name,
                $"Skrócona kontrola regresji obszaru {area.Name}.",
                new[]
                {
                    new Collection(
                        $"Regresja {area.Name}",
                        new[] { $"Kluczowy przebieg obszaru {area.Name}" })
                }))
            .ToArray();

        AddTestType(seed, projectKey, "regression", "regression-root", regressionAreas, true);
        return seed;
    }

    private static void AddTestType(
        SeedData seed,
        string projectKey,
        string testTypeKey,
        string rootKey,
        IReadOnlyList<Area> areas,
        bool regression)
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
                            ? $"Potwierdź najważniejszy przebieg. {caseName}."
                            : $"Zweryfikuj scenariusz. {caseName}.",
                        Preconditions = "Aplikacja demonstracyjna jest uruchomiona i gotowa do testu.",
                        Importance = regression ? "Wysoki" : "Średni",
                        ExecutionType = "Manualny",
                        Platforms = new List<string> { "Demo Terminal" },
                        Steps = new List<TestStepModel>
                        {
                            new()
                            {
                                Number = 1,
                                Actions = $"Wykonaj scenariusz. {caseName}.",
                                ExpectedResults = "Operacja kończy się przewidywalnym wynikiem i czytelnym komunikatem.",
                                ExecutionType = "Manualny"
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

    private sealed record Area(string Name, string Description, IReadOnlyList<Collection> Collections);
    private sealed record Collection(string Name, IReadOnlyList<string> Cases);

    private sealed class SeedData
    {
        public List<TestSectionModel> Folders { get; } = new();
        public List<TestCollectionModel> Collections { get; } = new();
        public List<TestCaseModel> TestCases { get; } = new();
    }
}
