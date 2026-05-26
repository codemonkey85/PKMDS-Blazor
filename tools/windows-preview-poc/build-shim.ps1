<#
.SYNOPSIS
    Builds the native C++ preview-handler shim (PkmdsPreviewShim.dll).

.DESCRIPTION
    Locates a Visual Studio / Build Tools install that has the C++ workload (via vswhere),
    sets up the MSVC environment with vcvars64.bat, and compiles PkmdsPreviewShim.cpp with
    cl.exe into a self-contained (static-CRT) DLL. No admin required.

    We invoke cl directly rather than MSBuild on the .vcxproj because VS 2026's platform
    toolset (v180) isn't resolved by a bare `MSBuild PkmdsPreviewShim.vcxproj` without the
    dev environment; vcvars sets it up correctly. The .vcxproj is kept for IDE users.

.PARAMETER OutDir
    Where to place PkmdsPreviewShim.dll. Default: PkmdsPreviewShim\bin.
#>
[CmdletBinding()]
param([string]$OutDir)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$src = Join-Path $root 'PkmdsPreviewShim'
if (-not $OutDir) { $OutDir = Join-Path $src 'bin' }
New-Item -ItemType Directory -Force $OutDir | Out-Null

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw "vswhere not found. Install Visual Studio (or Build Tools) with the 'Desktop development with C++' workload."
}
$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) {
    throw "No VS install with the C++ toolset (VC.Tools.x86.x64) was found. Add 'Desktop development with C++' in the VS Installer."
}
$vcvars = Join-Path $vsPath 'VC\Auxiliary\Build\vcvars64.bat'
if (-not (Test-Path $vcvars)) { throw "vcvars64.bat not found under $vsPath" }

$cpp = Join-Path $src 'PkmdsPreviewShim.cpp'
$def = Join-Path $src 'PkmdsPreviewShim.def'

# A temp batch avoids PowerShell/cmd nested-quote issues with spaced paths.
$bat = @"
@echo off
call "$vcvars" >nul
cl /nologo /W3 /EHsc /std:c++17 /MT /O2 /DUNICODE /D_UNICODE /LD "$cpp" /Fo"$OutDir\PkmdsPreviewShim.obj" /Fe"$OutDir\PkmdsPreviewShim.dll" /link /DEF:"$def"
"@
$tmp = Join-Path $env:TEMP "build-pkmds-shim-$([guid]::NewGuid().ToString('N')).bat"
Set-Content -Path $tmp -Value $bat -Encoding ascii
try { cmd /c "`"$tmp`"" } finally { Remove-Item $tmp -ErrorAction SilentlyContinue }
if ($LASTEXITCODE -ne 0) { throw "Shim build failed (exit $LASTEXITCODE)." }

Write-Host "Built $OutDir\PkmdsPreviewShim.dll (using $vsPath)" -ForegroundColor Green
