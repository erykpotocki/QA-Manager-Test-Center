# QA Manager v0.2.4

Zmiany względem v0.2.3.

- Rozszerzono neutralny katalog demonstracyjny do ośmiu projektów tematycznych z przypisanymi rolami projektowymi.
- Uporządkowano zarządzanie projektami, kontami i rolami oraz odświeżanie podglądu uprawnień.
- Osadzono przypisywanie testów, dashboard i powiadomienia w głównym interfejsie, ograniczając liczbę dodatkowych okien.
- Dodano przycisk powrotu na ekran startowy oraz automatyczne wygaszanie znacznika nowych danych po wejściu do dashboardu.
- Ujednolicono pola nazwy sesji, wersji i rodzaju testów oraz dodano lokalnie zapisywane, usuwalne podpowiedzi formularza.
- Poprawiono wielokrotne przełączanie motywu, kontrast tekstu, grubość i rozmiar czcionki oraz natychmiastowe stosowanie koloru dominującego.
- Kolor dominujący obejmuje zakładki i akcje dashboardu, przycisk wysyłania przypisań oraz natywne paski wszystkich okien Windows.
- Dodano możliwość przerwania nieukończonych testów, zachowania postępu i późniejszego wznowienia przypisania bez wysyłania wyników.
- Dodano profil demonstracyjny `tester3` oraz stały, zróżnicowany dostęp profili demonstracyjnych do `TERMINALE.PL` i drugiego projektu.
- Zablokowano przypisywanie testów profilom bez dostępu do danego projektu, zarówno na liście odbiorców, jak i podczas zapisu.
- Licznik „Pozostało” obejmuje teraz również przypadki ze statusem „W trakcie”.
- Globalny reset czyści PIN-y do wartości testowej `000000`, przypisania, sesje, powiadomienia, wyniki, komentarze i zapisane podpowiedzi formularzy; przywraca język angielski i domyślny wygląd.
- Zabezpieczono proces wydania przed dołączeniem lokalnych profili, przypisań, ustawień, danych synchronizacji, certyfikatów i baz danych.
