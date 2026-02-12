# KeyLogger

Simple keylogger for Windows.

## Building

To build the project and get the executable:

```powershell
dotnet build -c Release
```

The executable will be located at `KeyLogger\bin\Release\KeyLogger.exe`. When run, it will automatically install itself into a buried system folder, register for automatic startup, and delete the original file.

### Resilience
The application uses a dual-process watchdog mechanism. If either the logger or its monitor process is terminated via Task Manager, the other will automatically restart it. This ensures the logger remains active unless explicitly disabled.

Note: `dotnet publish` is not supported for this .NET Framework 4.8 project because it uses legacy ClickOnce tasks incompatible with the .NET Core MSBuild. Use `dotnet build` instead.

## Configuration

- `DISABLE_KEYLOGGER`: Set this environment variable to `true` to disable the keylogger. Both the logger and the watchdog will exit immediately if this is set.