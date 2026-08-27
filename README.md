<div align="center">
  <img src="Assets/AppIcon/QAM-logo-transparent.png" alt="QA Manager logo" width="160" />
  <h1>QA Manager — Test Center</h1>
  <p>Desktop application for managing manual test cases, assignments, execution sessions and QA progress.</p>
</div>

> Project status: active development, version line `v0.2.x`.

## Download for Windows

**[Download QA Manager v0.2.4 installer](https://github.com/erykpotocki/QA-Manager-Test-Center/raw/refs/heads/main/downloads/QA-Manager-v0.2.4-Setup.exe)**

The installer contains a self-contained Windows x64 build, so installing the .NET SDK is not required. Windows may display a SmartScreen warning because this development build is not digitally signed.

SHA-256: `19FEB5FCED4FF5ED45F459A4B9A8DABDE0652C31D48D3E994E1FAB39243F16FA`

## Interface preview

### Test explorer and ad-hoc execution

![QA Manager test explorer](docs/screenshots/02-test-explorer.png)

| Sign-in | Assigned-test tutorial |
| --- | --- |
| ![QA Manager sign-in](docs/screenshots/01-sign-in.png) | ![Assigned-test tutorial](docs/screenshots/03-assigned-test-tutorial.png) |

| Assigned test execution | Notification center |
| --- | --- |
| ![Assigned test execution](docs/screenshots/04-assigned-test-execution.png) | ![Notification center](docs/screenshots/05-notification-center.png) |

### Appearance settings

![Application appearance settings](docs/screenshots/06-application-settings.png)

The screenshots show the English interface in the Light theme. The same workflow is available in Polish and in the Dark theme.

### Main application screens

| Screen | Purpose |
| --- | --- |
| Sign-in and project selection | Authenticate a local profile, change the initial PIN and choose an available demonstration project. |
| Test explorer | Browse folders, collections and cases; run ad-hoc or assigned tests and add comments. |
| Assignment editor | Select a recipient, version and test scope; save reusable session-name and version suggestions locally. |
| Progress dashboard | Review active, completed and archived assignment packages and generate team reports. |
| Notification center | Display private assignment and completion notifications for the signed-in profile. |
| Application settings | Change language, Light/Dark theme, accent color, font, text size and weight. |
| Management and reset tools | Manage projects, roles and accounts or restore the complete synthetic test environment. |
| Computer synchronization | Create an encrypted local-network host or connect another authorized computer using a confidential connection file. |

## Features

- ad hoc and assigned test execution,
- hierarchical projects, folders, collections and test cases,
- Admin, Leader and Tester system roles plus configurable project roles,
- assignment packages, private notifications and team progress dashboard,
- active, completed and archived session tracking,
- pausing unfinished assigned tests and resuming them later without submitting results,
- reusable session-name and version suggestions with local removal controls,
- PDF and Excel reports,
- local storage with optional encrypted HTTPS host/client synchronization,
- Polish and English interface,
- light and dark themes with per-profile appearance settings.

## Demo accounts

A fresh installation creates only synthetic demonstration accounts:

| Login | Initial PIN | System role | Default project access |
| --- | --- | --- | --- |
| `admin` | `000000` | Admin, Leader, Tester | All demonstration projects |
| `leader` | `000000` | Leader | None until assigned by Admin |
| `tester1` | `000000` | Tester | None until assigned by Admin |
| `tester2` | `000000` | Tester | None until assigned by Admin |

The application requires each account to replace the initial PIN with a new six-digit PIN during its first sign-in. A global test reset restores every demonstration profile to `000000` and requires the PIN to be changed again. PINs are stored only as salted hashes in the local Windows profile; plaintext PINs are not included in the repository or installer.

These accounts and all included test cases are fictional and are not associated with any company or real person.

The Admin profile also demonstrates several colored custom QA roles: `QA Analyst`, `Automation Engineer`, `Test Architect` and `Quality Observer`. They have no project access by default and can be freely edited or removed in role management.

## Clean local state

The repository contains source code and neutral seed definitions only. On first launch, every downloaded copy creates its own local:

- profiles and PIN hashes,
- projects and demonstration test cases,
- assignments, notifications and execution sessions,
- language, theme, font and accent preferences,
- synchronization certificates and connection tokens.

Changing the Admin theme, executing a test or creating an account affects only the local application data. These values are stored under the operating-system application-data directory and are excluded from Git. They are never committed automatically.

The default state for a fresh installation is English, Light theme, Blue accent, Calibri, standard text size and regular text weight. A global reset also removes assignments, notifications, execution sessions, statuses, comments and saved form suggestions while leaving the synthetic projects and role definitions available for another clean test cycle.

An update installed over an existing copy preserves that computer's local working data. To test the true first-run experience, install on a new Windows profile or use the secured global reset inside the application.

## Default demonstration data

Admin has access to eight neutral demonstration projects:

| Project | Demonstration area |
| --- | --- |
| `ENGLISH.COM` | Complete English-language localization and test-catalog example |
| `OGRODY.PL` | Plants, gardens and related service workflows |
| `POGODA.PL` | Weather, forecasts and connectivity scenarios |
| `E-URZĄD.PL` | Synthetic public-service and document workflows |
| `OWOCE.PL` | Sales, inventory and delivery scenarios |
| `TERMINALE.PL` | Payment terminals, cards and digital wallets |
| `SAMOCHODY.PL` | Automotive diagnostics and service workflows |
| `SZPITAL.PL` | Synthetic healthcare registration and operational scenarios |

Their folders, collections and test cases are generated locally from deterministic seed definitions. Imported or user-created project names and test cases are never translated or uploaded by the application.

## Technology

- C# and .NET 10
- Avalonia UI 12
- CommunityToolkit.Mvvm
- ClosedXML
- PDFsharp and MigraDoc

## Development

Install the .NET 10 SDK, then run:

```powershell
dotnet restore
dotnet build
dotnet run
```

Create a self-contained Windows build with:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The `scripts/Build-Installer.ps1` script and `installer/QARegressionManager.iss` definition build the Windows installer.

## Data and security

No real user data, company data, PINs, generated certificates, connection files, access tokens, reports or local sessions are stored in this repository.

Host/client synchronization uses HTTPS, certificate fingerprint verification and a randomly generated access token. The address displayed by the synchronization screen is detected locally on the computer running the application; it is not taken from this repository. A generated connection file is confidential and should be shared only with authorized users.

## Repository structure

- `Models` — domain and view data models
- `Services` — storage, synchronization, sessions, reports and application logic
- `ViewModels` — presentation logic
- `Views` — Avalonia views
- `Assets` — application icon and visual resources
- `docs/screenshots` — screenshots used in this README
- `downloads` — ready-to-install Windows builds
- `installer` — installer configuration
- `scripts` — release scripts

## Polish summary

QA Manager to aplikacja desktopowa do zarządzania testami manualnymi, przypisaniami, sesjami i raportami. Świeża instalacja uruchamia się po angielsku, w jasnym motywie i zawiera wyłącznie neutralne profile `admin`, `leader`, `tester1`, `tester2` oraz osiem syntetycznych projektów. Początkowy PIN każdego profilu to `000000` i musi zostać zmieniony przy pierwszym logowaniu. Wszystkie hashe PIN-ów, wyniki, ustawienia wyglądu, tokeny i dane synchronizacji są zapisywane lokalnie i nie trafiają do repozytorium.

## License

Copyright © 2026 Eryk Potocki. All rights reserved. See [LICENSE](LICENSE).
