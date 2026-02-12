# KeyLogger

Simple keylogger for Windows.

## Building

To build the project and get the executable:

```powershell
dotnet build -c Release
```

The executable will be located at `KeyLogger\bin\Release\KeyLogger.exe`.

Note: `dotnet publish` is not supported for this .NET Framework 4.8 project because it uses legacy ClickOnce tasks incompatible with the .NET Core MSBuild. Use `dotnet build` instead.

## Configuration

- `DISABLE_KEYLOGGER`: Set this environment variable to `true` to disable the keylogger.