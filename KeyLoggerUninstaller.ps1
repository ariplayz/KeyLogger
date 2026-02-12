$ErrorActionPreference = "SilentlyContinue"

$installDir = Join-Path $env:LOCALAPPDATA "WindowsSystemUtility"
$registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$registryValue = "WinSysUtils"

Write-Host "Starting KeyLogger (WinSysUtils) Uninstallation..." -ForegroundColor Cyan

Write-Host "Terminating processes..." -ForegroundColor Yellow

$mainProcesses = Get-Process -Name "WinSysUtils"
foreach ($p in $mainProcesses) {
    try {
        if ($p.Path -like "$installDir*") {
            Stop-Process -Id $p.Id -Force
            Write-Host "Killed main process: $($p.Id)"
        }
    } catch {}
}

$allProcesses = Get-Process
foreach ($p in $allProcesses) {
    try {
        if ($p.ProcessName -match "^\d{12}$") {
            if ($p.Path -like "$installDir*") {
                Stop-Process -Id $p.Id -Force
                Write-Host "Killed watchdog process: $($p.ProcessName) ($($p.Id))"
            }
        }
    } catch {}
}

Start-Sleep -Seconds 1

Write-Host "Removing registry startup entry..." -ForegroundColor Yellow
if (Get-ItemProperty -Path $registryPath -Name $registryValue) {
    Remove-ItemProperty -Path $registryPath -Name $registryValue
    Write-Host "Removed registry value: $registryValue"
}

Write-Host "Removing installation files..." -ForegroundColor Yellow
if (Test-Path $installDir) {
    Remove-Item -Path "$installDir\*" -Force -Recurse
    Remove-Item -Path $installDir -Force -Recurse
    Write-Host "Deleted directory: $installDir"
}

Write-Host "`nUninstallation complete!" -ForegroundColor Green
