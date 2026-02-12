# KeyLogger Uninstaller Script
# This script must be run as Administrator to effectively kill processes and remove files.

$ErrorActionPreference = "SilentlyContinue"

# 1. Define paths
$installDir = Join-Path $env:LOCALAPPDATA "WindowsSystemUtility"
$registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$registryValue = "WinSysUtils"

Write-Host "Starting KeyLogger (WinSysUtils) Uninstallation..." -ForegroundColor Cyan

# 2. Kill Processes
Write-Host "Terminating processes..." -ForegroundColor Yellow

# Kill main process
$mainProcesses = Get-Process -Name "WinSysUtils"
foreach ($p in $mainProcesses) {
    try {
        if ($p.Path -like "$installDir*") {
            Stop-Process -Id $p.Id -Force
            Write-Host "Killed main process: $($p.Id)"
        }
    } catch {}
}

# Kill randomized watchdog processes (12-digit names)
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

# Wait a moment for processes to exit
Start-Sleep -Seconds 1

# 3. Remove Registry Startup Entry
Write-Host "Removing registry startup entry..." -ForegroundColor Yellow
if (Get-ItemProperty -Path $registryPath -Name $registryValue) {
    Remove-ItemProperty -Path $registryPath -Name $registryValue
    Write-Host "Removed registry value: $registryValue"
}

# 4. Remove Files and Directory
Write-Host "Removing installation files..." -ForegroundColor Yellow
if (Test-Path $installDir) {
    # Attempt to delete contents first
    Remove-Item -Path "$installDir\*" -Force -Recurse
    # Remove the directory itself
    Remove-Item -Path $installDir -Force -Recurse
    Write-Host "Deleted directory: $installDir"
}

Write-Host "`nUninstallation complete!" -ForegroundColor Green
