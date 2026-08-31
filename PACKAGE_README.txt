QA Manager v0.2.4

Copyright © 2026 Eryk Potocki. Wszelkie prawa zastrzeżone.

Uruchom plik QAManager.exe.
Instalacja środowiska .NET nie jest wymagana.

Pierwsze logowanie
Profile demonstracyjne: admin, leader, tester1, tester2 i tester3.
Początkowy PIN każdego profilu to 000000. Przy pierwszym logowaniu aplikacja wymaga ustawienia własnego 6-cyfrowego PIN-u.

Domyślny dostęp
admin: wszystkie projekty demonstracyjne
leader: TERMINALE.PL i OGRODY.PL
tester1: TERMINALE.PL i POGODA.PL
tester2: TERMINALE.PL i SAMOCHODY.PL
tester3: TERMINALE.PL i SZPITAL.PL
Testy można przypisać wyłącznie profilowi mającemu dostęp do danego projektu.

Projekty demonstracyjne
Świeża instalacja tworzy wyłącznie neutralne projekty: ENGLISH.COM, OGRODY.PL, POGODA.PL, E-URZĄD.PL, OWOCE.PL, TERMINALE.PL, SAMOCHODY.PL i SZPITAL.PL.
Przy pierwszym otwarciu projektu aplikacja przygotowuje odpowiedni syntetyczny katalog testów.
Ponowne uruchomienie aplikacji nie tworzy duplikatów.

Synchronizacja
Jeden komputer pełni rolę hosta. Pozostałe komputery wczytują plik połączenia wygenerowany na hoście.
Plik połączenia zawiera poufny token i powinien być przekazywany wyłącznie uprawnionym użytkownikom.

Dane lokalne
Dane użytkownika nie są dołączone do tej paczki. Aplikacja tworzy je w profilu Windows przy pierwszym uruchomieniu. Nowa instalacja rozpoczyna pracę w języku angielskim, jasnym motywie i z niebieskim kolorem dominującym.

Zawartość paczki
QAManager.exe
CHANGELOG.md
