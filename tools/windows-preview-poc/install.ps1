#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Builds, deploys, and registers the PKMDS Windows Explorer preview handler.

.DESCRIPTION
    PowerToys-style split (see README "The working architecture"):
      1. Build the native C++ shim (PkmdsPreviewShim.dll) — the COM IPreviewHandler that
         loads into prevhost. Requires the MSVC/C++ toolchain (build-shim.ps1 finds it).
      2. Publish the .NET worker (PkmdsPreviewWorker.exe) self-contained into dist\.
      3. Copy the shim DLL next to the worker (the shim launches the worker from its own dir).
      4. Register InprocServer32 -> dist\PkmdsPreviewShim.dll (+ AppID, approved list, ShellEx).
      5. Restart Explorer.

    Registration writes to HKLM, so run from an elevated (Administrator) terminal.

.PARAMETER Runtime
    RID for the self-contained worker publish. Default: win-x64.

.PARAMETER NoRestartExplorer
    Skip the Explorer restart at the end.

.EXAMPLE
    ./install.ps1
#>
[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [switch]$NoRestartExplorer
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$worker = Join-Path $root 'PkmdsPreviewWorker\PkmdsPreviewWorker.csproj'
$dist = Join-Path $root 'dist'

# Stop the handler hosts first — prevhost (preview) / dllhost (thumbnail) and any lingering worker
# keep the DLLs in dist\ locked, which would fail the clean+republish below.
Write-Host "Stopping handler hosts (prevhost / dllhost / worker)..." -ForegroundColor DarkGray
Stop-Process -Name prevhost, dllhost, PkmdsPreviewWorker -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

Write-Host "[1/5] Building native shim (PkmdsPreviewShim.dll)..." -ForegroundColor Cyan
& (Join-Path $root 'build-shim.ps1')
$shim = Join-Path $root 'PkmdsPreviewShim\bin\PkmdsPreviewShim.dll'
if (-not (Test-Path $shim)) { throw "Shim not built: $shim" }

Write-Host "[2/5] Publishing worker (self-contained, $Runtime) -> dist\ ..." -ForegroundColor Cyan
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
dotnet publish $worker -c Release -r $Runtime --self-contained -o $dist --nologo
if ($LASTEXITCODE -ne 0) { throw "Worker publish failed." }

Write-Host "[3/5] Co-locating shim with worker..." -ForegroundColor Cyan
Copy-Item $shim $dist -Force
$shimDeployed = (Resolve-Path (Join-Path $dist 'PkmdsPreviewShim.dll')).Path

Write-Host "[4/5] Registering -> $shimDeployed" -ForegroundColor Cyan
dotnet run (Join-Path $root 'register.cs') -- --register $shimDeployed
if ($LASTEXITCODE -ne 0) { throw "Registration failed." }

if (-not $NoRestartExplorer) {
    Write-Host "[5/5] Restarting Explorer..." -ForegroundColor Cyan
    Stop-Process -Name prevhost -Force -ErrorAction SilentlyContinue
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    if (-not (Get-Process explorer -ErrorAction SilentlyContinue)) { Start-Process explorer.exe }
}

Write-Host "Done. Open a PKHeX file in Explorer with the Preview Pane (Alt+P) enabled." -ForegroundColor Green
