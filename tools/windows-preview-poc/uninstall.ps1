#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Unregisters the PKMDS Windows Explorer preview handler (machine-wide).

.DESCRIPTION
    Removes every registry key install.ps1 wrote: the per-extension shell associations,
    the entry in the approved PreviewHandlers list, and the CLSID. Must run elevated.

.PARAMETER NoRestartExplorer
    Skip the Explorer restart at the end.

.EXAMPLE
    # From an elevated PowerShell prompt:
    ./uninstall.ps1
#>
[CmdletBinding()]
param([switch]$NoRestartExplorer)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host "Unregistering preview handler..." -ForegroundColor Cyan
dotnet run (Join-Path $root 'register.cs') -- --unregister
if ($LASTEXITCODE -ne 0) { throw "Unregistration failed." }

if (-not $NoRestartExplorer) {
    Write-Host "Restarting Explorer..." -ForegroundColor Cyan
    # Stop prevhost too — it hosts the preview handler, so the DLL stays loaded until it exits.
    Stop-Process -Name prevhost -Force -ErrorAction SilentlyContinue
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    if (-not (Get-Process explorer -ErrorAction SilentlyContinue)) { Start-Process explorer.exe }
}

Write-Host "Done." -ForegroundColor Green
