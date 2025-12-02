# keyboard-auto-switcher

[![Tests](https://github.com/jdeniau/keyboard-auto-switcher/actions/workflows/tests.yml/badge.svg)](https://github.com/jdeniau/keyboard-auto-switcher/actions/workflows/tests.yml)
[![codecov](https://codecov.io/gh/jdeniau/keyboard-auto-switcher/graph/badge.svg)](https://codecov.io/gh/jdeniau/keyboard-auto-switcher)

Switch automatically between azerty and dvorak if a TypeMatrix keyboard is connected.

## Features

- 🔄 Automatic keyboard layout switching (Dvorak ↔ AZERTY)
- 🖥️ System tray icon with current layout indicator
- 📋 Log viewer with syntax highlighting
- 🌓 Dark/Light theme support (follows Windows settings)
- 🚀 Launch at Windows startup option
- 🔄 Auto-updates via GitHub Releases

## Installation

### Option 1: Installer (recommended)

1. Download the latest `KeyboardAutoSwitcher-win-x64-Setup.exe` from [Releases](https://github.com/jdeniau/keyboard-auto-switcher/releases)
2. Run the installer
3. Done! The app will start automatically with Windows and check for updates.

### Option 2: Manual installation

1. Publish a Release build (self-contained):

```pwsh
dotnet publish -c Release -r win-x64 --self-contained true
```

Publish output: `bin/Release/net7.0-windows/win-x64/publish/keyboard-auto-switcher.exe`

**Note:** Logs are written to `C:\ProgramData\KeyboardAutoSwitcher\logs\log-YYYYMMDD.txt` for troubleshooting.

2. Right-click on the system tray icon and enable "Lancer au démarrage de Windows" to start automatically.

## Building the installer

To release a new version :

- Update the version number in `keyboard-auto-switcher.csproj`
- push a new git tag `vX.Y.Z` (e.g. `v1.0.1`)
- create a release on GitHub

<details>

<summary>Detailled memo on how to release a new version manually (prefer the GitHub Actions workflow)</summary>

This project uses [Velopack](https://velopack.io/) for packaging and auto-updates.

### Prerequisites

1. Install the Velopack CLI:

```pwsh
dotnet tool install -g vpk
```

### Build and package

1. Build the release:

```pwsh
dotnet publish -c Release -r win-x64 --self-contained true
```

2. Create the Velopack package:

```pwsh
vpk pack --packId KeyboardAutoSwitcher --packVersion 1.0.0 --packDir .\bin\Release\net7.0-windows\win-x64\publish\ --mainExe KeyboardAutoSwitcher.exe
```

The installer and update files will be created in the `Releases/` folder:

- `KeyboardAutoSwitcher-win-x64-Setup.exe` - Installer
- `KeyboardAutoSwitcher-1.0.0-win-x64-full.nupkg` - Full package for updates
- `RELEASES` - Release manifest

### Publishing updates

Upload all files from the `Releases/` folder to GitHub Releases. The app will automatically detect and offer updates to users.

</details>

## Contribute

```sh
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1; dotnet run
```

## Tests

### Run tests

```pwsh
dotnet test tests
```

### Run tests with coverage (HTML report)

```pwsh
# 1. Run tests with coverage collection
dotnet test tests --collect:"XPlat Code Coverage" --results-directory:"tests/TestResults"

# 2. Generate the HTML report (requires reportgenerator)
reportgenerator -reports:"tests/TestResults/*/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# 3. Open the report
start coverage-report/index.html
```

**Installation of ReportGenerator** (one-time):

```pwsh
dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.2.0
```

Open the coverage report:

```pwsh
start coverage-report/index.html
```
