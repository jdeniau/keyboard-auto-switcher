# keyboard-auto-switcher

[![Tests](https://github.com/jdeniau/keyboard-auto-switcher/actions/workflows/tests.yml/badge.svg)](https://github.com/jdeniau/keyboard-auto-switcher/actions/workflows/tests.yml)

Switch automatically between azerty and dvorak if a typematrix keyboard is connected

## Usage

```sh
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1;  dotnet run
```

## Windows Service

Build and install as a Windows Service (no external tools required):

**Note:** Logs are written to `C:\ProgramData\KeyboardAutoSwitcher\logs\log-YYYYMMDD.txt` for troubleshooting.

1. Publish a Release build (self-contained optional):

```pwsh
dotnet publish -c Release -r win-x64 --self-contained true
```

Publish output: `bin/Release/net7.0-windows/win-x64/publish/keyboard-auto-switcher.exe`

2. Install the service (run PowerShell as Administrator):

```pwsh
$svcName = "KeyboardAutoSwitcher"
$exe = (Resolve-Path "$PWD/bin/Release/net7.0-windows/win-x64/publish/keyboard-auto-switcher.exe").Path
# IMPORTANT: run this in an elevated (Administrator) PowerShell, otherwise you'll get "Accès refusé" (Access denied)
sc.exe create "$svcName" binPath= "`"$exe`"" start= auto DisplayName= "Keyboard Auto Switcher"
sc.exe description $svcName "Automatically switch between AZERTY and Dvorak when Typematrix is connected"
```

3. Start/Stop the service:

```pwsh
Start-Service KeyboardAutoSwitcher
# Stop-Service KeyboardAutoSwitcher
```

4. Uninstall the service:

```pwsh
sc.exe stop KeyboardAutoSwitcher
sc.exe delete KeyboardAutoSwitcher
```

### Alternative: Scheduled Task (recommended for desktop interaction)

Windows Services run in Session 0 and cannot interact with the user desktop (foreground window). Since this app needs to switch the active input language for the signed-in user, a Scheduled Task set to run at logon is often more reliable.

Create a task that runs at user logon (no admin required):

```pwsh
$taskName = "KeyboardAutoSwitcher"
$exe = (Resolve-Path "$PWD/bin/Release/net7.0-windows/win-x64/publish/keyboard-auto-switcher.exe").Path
$action = New-ScheduledTaskAction -Execute $exe -WorkingDirectory (Split-Path $exe)
$trigger = New-ScheduledTaskTrigger -AtLogOn
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Description "Auto-switch keyboard layout when Typematrix is connected" -RunLevel Highest -User $env:USERNAME
```

Start/Stop the task:

```pwsh
Start-ScheduledTask -TaskName KeyboardAutoSwitcher
Stop-ScheduledTask -TaskName KeyboardAutoSwitcher
Unregister-ScheduledTask -TaskName KeyboardAutoSwitcher -Confirm:$false
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
