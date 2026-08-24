# QA Manager v0.1

## Główne funkcje

- Lokalne profile użytkowników, PIN-y i role.
- Projekty, foldery, zbiory oraz przypadki testowe.
- Tryb ad hoc i wykonywanie testów przypisanych.
- Przypisywanie testów, powiadomienia i dashboard zespołu.
- Historia oraz archiwum zakończonych sesji.
- Raporty PDF i Excel.
- Automatyczny zapis i wznawianie sesji.
- Synchronizacja host–klient przez HTTPS w sieci lokalnej.

## Poprawki utrzymaniowe

- Naprawiono odczyt profili po ponownym uruchomieniu hosta.
- Host korzysta bezpośrednio z lokalnych danych, a API obsługuje klientów.
- Dodano obowiązkowy komentarz zapisywany automatycznie dla statusu Blocked.
- Zablokowano równoczesne uruchamianie wielu instancji aplikacji na jednym komputerze.
- Zabezpieczono tymczasowy zapis profili przed konfliktem plików.
