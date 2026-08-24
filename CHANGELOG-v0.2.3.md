# QA Manager v0.2.3

Zmiany względem v0.2.2.

- Ujednolicono nazwę produktu do `QA Manager` również w technicznej tożsamości aplikacji i identyfikatorze pojedynczej instancji; starszy katalog danych pozostaje obsługiwany dla zachowania profili.
- Uporządkowano demonstracyjną strukturę testów funkcjonalnych i regresji oraz usuwanie starszych, zduplikowanych gałęzi.
- Migracja katalogu demonstracyjnego zachowuje istniejące profile i elementy utworzone przez użytkowników.
- Dodano wybór języka polskiego lub angielskiego na ekranie logowania i w ustawieniach aplikacji.
- Zabezpieczono przeciąganie zbiorów i przypadków przed podwójną obsługą zdarzenia oraz utratą stanu po błędzie zapisu.
- Poprawiono kontrast wybranych opcji w ustawieniach dla ciemnego motywu.
- Przeniesiono powiadomienia, dashboard i ustawienia do stałej grupy przy prawej krawędzi nagłówka oraz zastąpiono ikonę ustawień wersją wektorową.
- Uporządkowano migrację starszych ról projektowych.
- Zastąpiono przyciski `EN/PL` pojedynczym przyciskiem flagi aktualnego języka; lista zawsze pokazuje najpierw `Polski`, a następnie `English`.
- Zachowano wybór języka w ustawieniach aplikacji, a przycisk flagi na ekranie logowania działa jako dodatkowy skrót; wymuszono też odświeżanie kolorów zawartości ComboBox po zmianie Light/Dark.
- Podgląd motywu Light/Dark obejmuje od razu wszystkie otwarte okna, bez zapisywania i ponownego otwierania ustawień.
- Rozszerzono lokalizację głównego interfejsu, drzewa testów, paska stanu, ekranu sesji i menu ustawień; nazwy projektów oraz dane testowe użytkownika pozostają niezmieniane.
- Dodano reguły własności elementów. Tester edytuje własne elementy, a Admin i Lider mogą zarządzać całą strukturą.
- Poprawiono czytelność i zakres regulacji lewego panelu oraz zabezpieczono go przed nieczytelnym zwężeniem.
- Dodano wieloetapowy samouczek wykonywania przypisanych testów z opcją wyłączenia dla konta.
- Pasek ról dopasowuje się do szerokości okna i przenosi nadmiarowe role do menu pod wielokropkiem.
- Uporządkowano okno przypisywania testów. Skrócono główną akcję do „Dodaj” i wyraźnie rozróżniono pozostałe przyciski.
- Dodano możliwość wcześniejszego zakończenia przypisanej sesji. Niewykonane przypadki są widoczne dla osoby zarządzającej i wracają do puli przypisań.
- Dashboard grupuje zakresy wysłane w jednym podsumowaniu jako jeden pakiet. Pakiet pozostaje aktywny do ukończenia wszystkich zakresów i wygenerowania raportu.
- Usunięcie pakietu z historii przenosi go do archiwum. Archiwum pozwala przywrócić pakiet, usunąć go trwale i automatycznie czyści dane po 60 dniach.
- Zmniejszono i wyśrodkowano znacznik stanu Archiwum, aby nie nachodził na pasek postępu.
- Powiadomienie o częściowym zakończeniu pokazuje liczbę wykonanych i niewykonanych przypadków.
- Dodano skrypt budowania instalatora Windows oraz przenośną paczkę ZIP.
