<#
.SYNOPSIS
    Publishes Pkmds.Web in Release with optional MSBuild args and measures
    publish time and output size.

.DESCRIPTION
    Used to compare the effect of WASM perf knobs (AOT, SIMD, full trim,
    InvariantGlobalization, lazy loading, ...) on build time and deploy size.
    Writes a labeled markdown report to measurements/<label>.md so successive
    runs can be diffed.

    This script does NOT measure runtime performance. Open the published site
    in a browser for that.

.PARAMETER Label
    Short name for this measurement (e.g. 'baseline', 'simd-on', 'aot-on').
    The report is written to measurements/<Label>.md.

.PARAMETER MSBuildArgs
    Optional array of extra MSBuild property args.

.PARAMETER OutputDir
    Publish output directory. Defaults to 'release'.

.EXAMPLE
    ./measure-publish.ps1 -Label baseline

.EXAMPLE
    ./measure-publish.ps1 -Label simd-on -MSBuildArgs '-p:WasmEnableSIMD=true'

.EXAMPLE
    ./measure-publish.ps1 -Label aot-simd `
        -MSBuildArgs '-p:RunAOTCompilation=true','-p:WasmEnableSIMD=true'
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Label,

    [string[]]$MSBuildArgs = @(),

    [string]$OutputDir = "release",

    [switch]$RunBenchmark
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot "Pkmds.Web/Pkmds.Web.csproj"
$publishDir = Join-Path $repoRoot $OutputDir
$measurementsDir = Join-Path $repoRoot "measurements"
$reportPath = Join-Path $measurementsDir "$Label.md"

if (-not (Test-Path $measurementsDir)) {
    New-Item -ItemType Directory -Path $measurementsDir | Out-Null
}

# Clean prior output so we don't measure stale files.
if (Test-Path $publishDir) {
    Write-Host "Cleaning $publishDir..."
    Remove-Item -Recurse -Force $publishDir
}

# Wipe Release obj/ for all projects so emcc native-object caching doesn't
# make a re-run look artificially fast. This forces a true cold build.
$objDirs = @(
    (Join-Path $repoRoot 'Pkmds.Web' 'obj' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Rcl' 'obj' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Core' 'obj' 'Release')
) | Where-Object { Test-Path $_ }
foreach ($d in $objDirs) {
    Write-Host "Cleaning $d..."
    Remove-Item -Recurse -Force $d
}

$gitHead = (git -C $repoRoot rev-parse --short HEAD).Trim()
$gitBranch = (git -C $repoRoot rev-parse --abbrev-ref HEAD).Trim()
$gitStatus = (git -C $repoRoot status --porcelain).Trim()
$gitDirty = if ($gitStatus) { "yes" } else { "no" }

Write-Host "Publishing '$Label'..." -ForegroundColor Cyan
Write-Host "  HEAD:  $gitHead ($gitBranch, dirty=$gitDirty)"
if ($MSBuildArgs.Count -gt 0) {
    Write-Host "  Extra: $($MSBuildArgs -join ' ')"
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet publish $project -c Release -o $publishDir --nologo @MSBuildArgs
$exitCode = $LASTEXITCODE
$sw.Stop()

if ($exitCode -ne 0) {
    throw "dotnet publish failed with exit code $exitCode"
}

$publishSeconds = [math]::Round($sw.Elapsed.TotalSeconds, 1)
Write-Host "Publish completed in ${publishSeconds}s" -ForegroundColor Green

function Format-Size {
    param([long]$Bytes)
    if ($Bytes -ge 1MB) { "{0:N2} MB" -f ($Bytes / 1MB) }
    elseif ($Bytes -ge 1KB) { "{0:N2} KB" -f ($Bytes / 1KB) }
    else { "$Bytes B" }
}

function Get-SumLength {
    param([System.IO.FileInfo[]]$Files)
    if (-not $Files) { return 0 }
    $sum = ($Files | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $sum) { 0 } else { [long]$sum }
}

$wwwroot = Join-Path $publishDir "wwwroot"
$framework = Join-Path $wwwroot "_framework"

$wwwrootFiles = Get-ChildItem $wwwroot -Recurse -File
$wwwrootSize = Get-SumLength $wwwrootFiles

$frameworkAll = Get-ChildItem $framework -File
$frameworkRawFiles = $frameworkAll | Where-Object { $_.Extension -notin '.br','.gz' }
$frameworkBrFiles  = $frameworkAll | Where-Object { $_.Extension -eq '.br' }
$frameworkGzFiles  = $frameworkAll | Where-Object { $_.Extension -eq '.gz' }

$frameworkRaw = Get-SumLength $frameworkRawFiles
$frameworkBr  = Get-SumLength $frameworkBrFiles
$frameworkGz  = Get-SumLength $frameworkGzFiles

# Top 20 largest framework files by raw size, with brotli sibling for comparison.
$topFiles = $frameworkRawFiles |
    Sort-Object Length -Descending |
    Select-Object -First 20 |
    ForEach-Object {
        $brPath = "$($_.FullName).br"
        $brSize = if (Test-Path $brPath) { (Get-Item $brPath).Length } else { 0 }
        [pscustomobject]@{
            Name = $_.Name
            Raw = $_.Length
            Brotli = $brSize
        }
    }

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Publish measurement: $Label")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("- Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
[void]$sb.AppendLine("- Git HEAD: ``$gitHead`` ($gitBranch, dirty=$gitDirty)")
$argsCell = if ($MSBuildArgs.Count -gt 0) { '`' + ($MSBuildArgs -join ' ') + '`' } else { '_(none)_' }
[void]$sb.AppendLine("- MSBuild args: $argsCell")
[void]$sb.AppendLine("- Publish time: **${publishSeconds}s**")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Output size")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Bucket | Size |")
[void]$sb.AppendLine("| --- | ---: |")
[void]$sb.AppendLine("| Total ``wwwroot/`` (incl. .br/.gz) | $(Format-Size $wwwrootSize) |")
[void]$sb.AppendLine("| ``_framework/`` raw (no .br/.gz) | $(Format-Size $frameworkRaw) |")
[void]$sb.AppendLine("| ``_framework/`` brotli | $(Format-Size $frameworkBr) |")
[void]$sb.AppendLine("| ``_framework/`` gzip | $(Format-Size $frameworkGz) |")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Top 20 largest framework files (raw)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| File | Raw | Brotli |")
[void]$sb.AppendLine("| --- | ---: | ---: |")
foreach ($f in $topFiles) {
    $brCell = if ($f.Brotli -gt 0) { Format-Size $f.Brotli } else { '—' }
    [void]$sb.AppendLine("| ``$($f.Name)`` | $(Format-Size $f.Raw) | $brCell |")
}

Set-Content -Path $reportPath -Value $sb.ToString() -Encoding UTF8

Write-Host ""
Write-Host "Report:  $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:"
Write-Host ("  Publish time:       {0}s" -f $publishSeconds)
Write-Host ("  wwwroot total:      {0}" -f (Format-Size $wwwrootSize))
Write-Host ("  _framework raw:     {0}" -f (Format-Size $frameworkRaw))
Write-Host ("  _framework brotli:  {0}" -f (Format-Size $frameworkBr))
Write-Host ("  _framework gzip:    {0}" -f (Format-Size $frameworkGz))

if ($RunBenchmark) {
    Write-Host ""
    Write-Host "Running runtime benchmark via Playwright..." -ForegroundColor Cyan
    $benchDir = Join-Path $repoRoot 'tools' 'bench'
    Push-Location $benchDir
    try {
        node run-bench.mjs --label $Label --site $wwwroot --output $reportPath
        if ($LASTEXITCODE -ne 0) {
            throw "Bench runner failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}
