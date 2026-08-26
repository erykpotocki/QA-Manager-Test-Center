using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public static class DemoDataSeedService
{
    private const string MigrationPrefix = "company-domain-demo-v4";

    public static bool EnsureSeeded(
        UserTestDataModel data,
        string projectKey,
        string projectName)
    {
        data.AppliedDataMigrations ??= new();

        if (!DemoCatalog.IsDemoProject(projectName))
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
        var functionalAreas = projectKey.ToLowerInvariant() switch
        {
            "plants-polish" => BuildPlantsAreas(),
            "planetarium-polish" => BuildPlanetariumAreas(),
            "office-polish" => BuildOfficeAreas(),
            "sales-polish" => BuildSalesAreas(),
            "automotive-polish" => BuildAutomotiveAreas(),
            "hospital-polish" => BuildHospitalAreas(),
            _ => english
                ? BuildEnglishAreas()
                : BuildPolishAreas()
        };

        AddTestType(seed, projectKey, "functional", "functional-root", functionalAreas, false, english);

        var regressionAreas = functionalAreas.Select((area, areaIndex) =>
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
                        BuildRegressionCases(area.Name, english, areaIndex < 5))
                }))
            .ToArray();

        AddTestType(seed, projectKey, "regression", "regression-root", regressionAreas, true, english);
        return seed;
    }

    private static Area[] BuildPolishAreas() =>
        ExpandFirstFiveFunctionalCollections(new[]
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
        }, false);

    private static Area[] BuildEnglishAreas() =>
        ExpandFirstFiveFunctionalCollections(new[]
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
        }, true);

    private static Area[] BuildPlantsAreas() =>
        new[]
        {
            new Area("Drzewa", "Rozpoznawanie drzew i kontrola danych katalogowych.", new[]
            {
                new Collection("Drzewa liściaste", new[] { "Rozpoznanie dębu po liściach", "Rozpoznanie klonu po liściach", "Porównanie brzozy i buka" }),
                new Collection("Drzewa iglaste", new[] { "Rozpoznanie sosny", "Rozpoznanie świerku", "Porównanie szyszek" })
            }),
            new Area("Liście", "Opis wyglądu, kondycji i sezonowych zmian liści.", new[]
            {
                new Collection("Identyfikacja liścia", new[] { "Weryfikacja kształtu liścia", "Weryfikacja unerwienia", "Weryfikacja krawędzi blaszki" }),
                new Collection("Stan rośliny", new[] { "Wykrycie przebarwień", "Wykrycie przesuszenia", "Ocena uszkodzeń liścia" })
            }),
            new Area("Pielęgnacja", "Czynności związane z podlewaniem i warunkami wzrostu.", new[]
            {
                new Collection("Podlewanie", new[] { "Ustalenie częstotliwości podlewania", "Kontrola wilgotności gleby", "Ostrzeżenie przed przelaniem" }),
                new Collection("Stanowisko", new[] { "Dobór nasłonecznienia", "Dobór temperatury", "Dobór rodzaju gleby" })
            })
        };

    private static Area[] BuildPlanetariumAreas() =>
        new[]
        {
            new Area("Prognozy", "Tworzenie i publikowanie prognoz pogody dla wybranych lokalizacji.", new[]
            {
                new Collection("Prognoza godzinowa", new[] { "Wyświetlenie temperatury", "Prezentacja opadów", "Aktualizacja siły wiatru", "Zmiana lokalizacji" }),
                new Collection("Prognoza długoterminowa", new[] { "Prognoza na siedem dni", "Ostrzeżenie o zmianie pogody", "Porównanie dwóch lokalizacji" })
            }),
            new Area("Pomiary", "Odczytywanie danych ze stacji meteorologicznych i czujników.", new[]
            {
                new Collection("Stacja pogodowa", new[] { "Odczyt temperatury", "Odczyt ciśnienia", "Odczyt wilgotności", "Brak danych z czujnika" }),
                new Collection("Radar opadów", new[] { "Wyświetlenie mapy opadów", "Zmiana zakresu czasu", "Odświeżenie obrazu radaru" })
            }),
            new Area("Alerty pogodowe", "Publikowanie ostrzeżeń o niebezpiecznych zjawiskach pogodowych.", new[]
            {
                new Collection("Ostrzeżenia", new[] { "Alert burzowy", "Alert o silnym wietrze", "Alert o oblodzeniu", "Odwołanie ostrzeżenia" })
            })
        };

    private static Area[] BuildOfficeAreas() =>
        new[]
        {
            new Area("Obsługa mieszkańca", "Podstawowe czynności wykonywane przez pracownika urzędu.", new[]
            {
                new Collection("Wnioski", new[] { "Przyjęcie nowego wniosku", "Weryfikacja kompletności dokumentów", "Odrzucenie niekompletnego wniosku", "Przekazanie wniosku do realizacji" }),
                new Collection("Kolejka", new[] { "Pobranie kolejnego numeru", "Wywołanie mieszkańca", "Przekierowanie do innego stanowiska" })
            }),
            new Area("Dokumenty", "Rejestracja, wyszukiwanie i wydawanie dokumentów.", new[]
            {
                new Collection("Rejestr", new[] { "Dodanie dokumentu", "Wyszukanie dokumentu", "Aktualizacja danych dokumentu", "Archiwizacja dokumentu" }),
                new Collection("Wydanie", new[] { "Potwierdzenie tożsamości", "Rejestracja odbioru", "Odmowa wydania osobie nieuprawnionej" })
            }),
            new Area("Opłaty", "Obsługa opłat administracyjnych.", new[]
            {
                new Collection("Płatność", new[] { "Naliczanie opłaty", "Rejestracja płatności", "Wydruk potwierdzenia", "Zwrot nadpłaty" })
            })
        };

    private static Area[] BuildSalesAreas() =>
        new[]
        {
            new Area("Sprzedaż", "Codzienna obsługa klienta i koszyka sprzedażowego.", new[]
            {
                new Collection("Koszyk", new[] { "Dodanie produktu", "Zmiana ilości produktu", "Usunięcie produktu", "Zastosowanie rabatu" }),
                new Collection("Finalizacja", new[] { "Sprzedaż gotówkowa", "Sprzedaż kartą", "Wydruk paragonu", "Anulowanie transakcji" })
            }),
            new Area("Produkty", "Wyszukiwanie produktów oraz kontrola ceny i dostępności.", new[]
            {
                new Collection("Katalog", new[] { "Wyszukanie produktu po nazwie", "Skanowanie kodu kreskowego", "Sprawdzenie ceny", "Sprawdzenie stanu magazynowego" }),
                new Collection("Braki", new[] { "Produkt niedostępny", "Nieznany kod kreskowy", "Brak ceny produktu" })
            }),
            new Area("Obsługa posprzedażowa", "Zwroty, reklamacje i korekty sprzedaży.", new[]
            {
                new Collection("Zwroty", new[] { "Zwrot produktu z paragonem", "Odmowa zwrotu bez podstawy", "Korekta płatności" }),
                new Collection("Reklamacje", new[] { "Przyjęcie reklamacji", "Dodanie opisu usterki", "Przekazanie reklamacji do rozpatrzenia" })
            })
        };

    private static Area[] BuildAutomotiveAreas() =>
        new[]
        {
            new Area("Przyjęcie pojazdu", "Rejestracja samochodu i ustalenie zakresu wizyty serwisowej.", new[]
            {
                new Collection("Zlecenie serwisowe", new[] { "Dodanie pojazdu po numerze VIN", "Wprowadzenie przebiegu", "Opis zgłoszonej usterki", "Przypisanie mechanika" }),
                new Collection("Termin wizyty", new[] { "Wybór wolnego terminu", "Zmiana terminu", "Anulowanie wizyty" })
            }),
            new Area("Diagnostyka", "Kontrola stanu technicznego i rejestrowanie wykrytych usterek.", new[]
            {
                new Collection("Diagnostyka komputerowa", new[] { "Odczyt kodów błędów", "Kasowanie błędu", "Zapis raportu diagnostycznego" }),
                new Collection("Przegląd techniczny", new[] { "Kontrola układu hamulcowego", "Kontrola oświetlenia", "Kontrola zawieszenia", "Wynik negatywny przeglądu" })
            }),
            new Area("Naprawa", "Realizacja naprawy oraz rozliczenie części i czasu pracy.", new[]
            {
                new Collection("Realizacja naprawy", new[] { "Dodanie części zamiennej", "Rejestracja czasu pracy", "Zmiana statusu naprawy", "Odbiór samochodu" })
            })
        };

    private static Area[] BuildHospitalAreas() =>
        new[]
        {
            new Area("Rejestracja pacjenta", "Przyjęcie pacjenta i skierowanie go do odpowiedniej jednostki.", new[]
            {
                new Collection("Dane pacjenta", new[] { "Rejestracja nowego pacjenta", "Wyszukanie istniejącego pacjenta", "Aktualizacja danych kontaktowych", "Weryfikacja ubezpieczenia" }),
                new Collection("Wizyta", new[] { "Umówienie wizyty", "Zmiana terminu wizyty", "Odwołanie wizyty" })
            }),
            new Area("Ratownictwo", "Obsługa zgłoszeń pilnych i przekazywanie pacjentów na oddział.", new[]
            {
                new Collection("Zgłoszenie ratunkowe", new[] { "Przyjęcie zgłoszenia", "Nadanie priorytetu", "Wysłanie zespołu ratowniczego", "Przekazanie pacjenta do szpitala" }),
                new Collection("SOR", new[] { "Triage pacjenta", "Rejestracja parametrów życiowych", "Skierowanie do lekarza" })
            }),
            new Area("Leczenie", "Dokumentowanie diagnozy, zaleceń i podawanych leków.", new[]
            {
                new Collection("Dokumentacja medyczna", new[] { "Dodanie rozpoznania", "Wystawienie zlecenia", "Podanie leku", "Wypis pacjenta" })
            })
        };

    private static Area[] ExpandFirstFiveFunctionalCollections(
        IReadOnlyList<Area> areas,
        bool english)
    {
        var expandedCollectionCount = 0;
        return areas
            .Select(area =>
                new Area(
                    area.Name,
                    area.Description,
                    area.Collections
                        .Select(collection =>
                        {
                            if (expandedCollectionCount++ >= 5)
                            {
                                return collection;
                            }

                            var cases = collection.Cases.ToList();
                            while (cases.Count < 12)
                            {
                                var scenarioIndex = cases.Count;
                                cases.Add(
                                    english
                                        ? $"{collection.Name}: {EnglishFunctionalScenarioNames[scenarioIndex]}"
                                        : $"{collection.Name}: {PolishFunctionalScenarioNames[scenarioIndex]}");
                            }

                            return new Collection(collection.Name, cases);
                        })
                        .ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildRegressionCases(
        string areaName,
        bool english,
        bool expand)
    {
        if (!expand)
        {
            return new[]
            {
                english
                    ? $"Critical {areaName} flow"
                    : $"Kluczowy przebieg obszaru {areaName}"
            };
        }

        var scenarios = english
            ? EnglishRegressionScenarioNames
            : PolishRegressionScenarioNames;
        return scenarios
            .Select(scenario => $"{areaName}: {scenario}")
            .ToArray();
    }

    private static readonly string[] EnglishFunctionalScenarioNames =
    {
        "standard flow",
        "valid data",
        "cancel before confirmation",
        "retry after interruption",
        "required-field validation",
        "invalid-data handling",
        "timeout handling",
        "repeat the operation",
        "confirmation message",
        "back navigation",
        "keyboard navigation",
        "recovery after restart"
    };

    private static readonly string[] PolishFunctionalScenarioNames =
    {
        "standardowy przebieg",
        "poprawne dane",
        "anulowanie przed potwierdzeniem",
        "ponowienie po przerwaniu",
        "walidacja wymaganych pól",
        "obsługa niepoprawnych danych",
        "obsługa przekroczenia czasu",
        "powtórzenie operacji",
        "komunikat potwierdzenia",
        "nawigacja wstecz",
        "obsługa klawiaturą",
        "przywrócenie po restarcie"
    };

    private static readonly string[] EnglishRegressionScenarioNames =
    {
        "critical flow",
        "standard successful flow",
        "cancellation before confirmation",
        "repeated operation",
        "valid-data verification",
        "invalid-data rejection",
        "recovery after interruption",
        "timeout behavior",
        "result message verification",
        "back navigation",
        "data persistence after restart",
        "final status verification"
    };

    private static readonly string[] PolishRegressionScenarioNames =
    {
        "kluczowy przebieg",
        "standardowy poprawny przebieg",
        "anulowanie przed potwierdzeniem",
        "powtórzenie operacji",
        "weryfikacja poprawnych danych",
        "odrzucenie niepoprawnych danych",
        "przywrócenie po przerwaniu",
        "zachowanie po przekroczeniu czasu",
        "weryfikacja komunikatu wyniku",
        "nawigacja wstecz",
        "zachowanie danych po restarcie",
        "weryfikacja statusu końcowego"
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
