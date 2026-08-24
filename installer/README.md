# Installpack QA Manager

Konfiguracja tworzy instalator Windows dla bieżącej wersji aplikacji. Instalator zawiera opublikowaną aplikację, ikonę QAM, README oraz changelog. Tworzy skróty na pulpicie i w menu Start oraz instaluje program bez wymagania uprawnień administratora.

Installpack należy generować wyłącznie na wyraźne żądanie użytkownika:

```powershell
.\scripts\Build-Installer.ps1 -Version 0.2.3
```

Do kompilacji instalatora wymagany jest Inno Setup 6. Samo istnienie tej konfiguracji nie zmienia zwykłej kompilacji aplikacji i nie generuje żadnej paczki.
