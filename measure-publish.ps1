<#
.SYNOPSIS
    Publishes Pkmds.Web in Release with optional MSBuild args and measures
    publish time and output size.

.DESCRIPTION
    Used to compare the effect of WASM perf knobs (AOT, SIMD, full trim,
    InvariantGlobalization, lazy loading, ...) on build time and deploy size.
    Writes a labeled markdown report to measurements/<label>.md so successive
    runs can be diffed.

    Use -RunBenchmark for repeated browser measurements after publishing.
    SDK/NuGet/OS caches remain warm; this measures a clean Release build,
    not an empty-machine-cache build.

.PARAMETER Label
    Short name for this measurement (e.g. 'baseline', 'simd-on', 'aot-on').
    The report is written to measurements/<Label>.md.

.PARAMETER MSBuildArgs
    Optional array of extra MSBuild property args.

.PARAMETER OutputDir
    Publish directory relative to ProjectRoot: release or a subdirectory of measurements.

.PARAMETER ProjectRoot
    Source checkout to publish. Defaults to this script's directory. Reports
    always go under this script's measurements directory.

.PARAMETER DotNetPath
    dotnet executable used in ProjectRoot (including for SDK selection).

.PARAMETER BenchmarkRuns
    Number of fresh-context browser repetitions when RunBenchmark is set.

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

    [switch]$RunBenchmark,

    [ValidateRange(1, 100)]
    [int]$BenchmarkRuns = 3,

    [string]$ProjectRoot,

    [string]$DotNetPath = 'dotnet'
)

$ErrorActionPreference = "Stop"

$repoRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $PSScriptRoot } else { (Resolve-Path -LiteralPath $ProjectRoot).Path }
$project = Join-Path $repoRoot "Pkmds.Web/Pkmds.Web.csproj"
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "PKMDS Web project not found under $repoRoot"
}
if ($Label -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]*$') {
    throw 'Label must be a filename-safe identifier.'
}
$publishDir = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir))
$rootPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $publishDir.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDir must be a directory inside ProjectRoot.'
}
# Refuse to clean source/control directories, including paths through junctions.
$relativeOutput = $publishDir.Substring($rootPrefix.Length)
if (($relativeOutput -split '[/\\]')[0] -notin @('release', 'measurements') -or $relativeOutput.TrimEnd('/','\') -eq 'measurements') {
    throw 'OutputDir must be release or a directory under measurements.'
}
for ($ancestor = $publishDir; $ancestor.Length -ge $repoRoot.Length; $ancestor = Split-Path $ancestor -Parent) {
    if ((Test-Path -LiteralPath $ancestor) -and
        ((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing cleanup through a reparse point: $ancestor"
    }
    if ($ancestor -eq $repoRoot) { break }
}
$measurementsDir = Join-Path $PSScriptRoot "measurements"
$reportPath = Join-Path $measurementsDir "$Label.md"
$jsonPath = Join-Path $measurementsDir "$Label.publish.json"
if (Test-Path -LiteralPath $jsonPath) {
    throw "Measurement already exists: $jsonPath. Choose a new Label to preserve prior results."
}

if (-not (Test-Path $measurementsDir)) {
    New-Item -ItemType Directory -Path $measurementsDir | Out-Null
}

# Clean prior output so we don't measure stale files.
if (Test-Path $publishDir) {
    Write-Host "Cleaning $publishDir..."
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

# Wipe Release obj/ and bin/ so project outputs and native-object caching do
# not make a re-run artificially fast. SDK, NuGet, and OS caches remain warm.
$objDirs = @(
    (Join-Path $repoRoot 'Pkmds.Web' 'obj' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Web' 'bin' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Rcl' 'obj' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Rcl' 'bin' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Core' 'obj' 'Release'),
    (Join-Path $repoRoot 'Pkmds.Core' 'bin' 'Release')
) | Where-Object { Test-Path $_ }
# Validate every cleanup target before removing any project output.
foreach ($d in $objDirs) {
    $resolved = (Resolve-Path -LiteralPath $d).Path
    if (-not $resolved.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Release output escaped ProjectRoot: $resolved"
    }
    for ($ancestor = $resolved; $ancestor -ne $repoRoot; $ancestor = Split-Path $ancestor -Parent) {
        if ((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Refusing cleanup through a reparse point: $ancestor"
        }
    }
}
foreach ($d in $objDirs) {
    Write-Host "Cleaning $d..."
    Remove-Item -LiteralPath $d -Recurse -Force
}

$gitHead = "$(git -C $repoRoot rev-parse --short HEAD)".Trim()
$gitBranch = "$(git -C $repoRoot rev-parse --abbrev-ref HEAD)".Trim()
# git status --porcelain returns no output (null in PowerShell) when the tree
# is clean — coerce to string before checking so .Trim() doesn't NRE.
$gitStatus = "$(git -C $repoRoot status --porcelain)".Trim()
$gitDirty = if ($gitStatus) { "yes" } else { "no" }

Write-Host "Publishing '$Label'..." -ForegroundColor Cyan
Write-Host "  HEAD:  $gitHead ($gitBranch, dirty=$gitDirty)"
if ($MSBuildArgs.Count -gt 0) {
    Write-Host "  Extra: $($MSBuildArgs -join ' ')"
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
Push-Location $repoRoot
try {
    $dotnetExecutable = (Get-Command $DotNetPath -ErrorAction Stop).Source
    $sdkVersion = (& $dotnetExecutable --version | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify the selected SDK.' }
    $dotnetInfo = (& $dotnetExecutable --info | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not collect SDK/workload information.' }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $dotnetExecutable publish $project -c Release -o $publishDir --nologo @MSBuildArgs
    $exitCode = $LASTEXITCODE
    $sw.Stop()
    if ($exitCode -eq 0) {
        $propertyJson = (& $dotnetExecutable msbuild $project -p:Configuration=Release @MSBuildArgs '-getProperty:TargetFramework,UseMonoRuntime,RunAOTCompilation,WasmEnableSIMD,TrimMode,PublishTrimmed,BlazorEnableCompression' | Out-String)
        if ($LASTEXITCODE -ne 0) { throw 'Could not collect resolved publish properties.' }
        $publishProperties = ($propertyJson | ConvertFrom-Json).Properties
    }
}
finally {
    Pop-Location
}

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

$frameworkAll = Get-ChildItem $framework -Recurse -File
$frameworkRawFiles = $frameworkAll | Where-Object { $_.Extension -notin '.br','.gz' }
$frameworkBrFiles  = $frameworkAll | Where-Object { $_.Extension -eq '.br' }
$frameworkGzFiles  = $frameworkAll | Where-Object { $_.Extension -eq '.gz' }

$frameworkRaw = Get-SumLength $frameworkRawFiles
$frameworkBr  = Get-SumLength $frameworkBrFiles
$frameworkGz  = Get-SumLength $frameworkGzFiles

$assets = Get-Content -LiteralPath (Join-Path $repoRoot 'Pkmds.Web/obj/project.assets.json') -Raw | ConvertFrom-Json
$packages = @($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | ForEach-Object { $_.Name } | Sort-Object)
$measurement = [ordered]@{
    SchemaVersion = 1
    Label = $Label
    Date = [DateTimeOffset]::Now.ToString('o')
    GitCommit = (git -C $repoRoot rev-parse HEAD | Out-String).Trim()
    GitBranch = $gitBranch
    GitDirty = $gitDirty
    GitStatus = $gitStatus
    ProjectRoot = $repoRoot
    DotNetPath = $dotnetExecutable
    SdkVersion = $sdkVersion
    DotNetInfo = $dotnetInfo
    OS = [Environment]::OSVersion.ToString()
    LogicalProcessors = [Environment]::ProcessorCount
    MSBuildArgs = @($MSBuildArgs)
    PublishProperties = $publishProperties
    CachePolicy = 'Release bin/obj and publish output removed; SDK/NuGet/build-server/OS caches retained; implicit restore timed.'
    PublishSeconds = $sw.Elapsed.TotalSeconds
    Sizes = [ordered]@{ Wwwroot = $wwwrootSize; FrameworkRaw = $frameworkRaw; FrameworkBrotli = $frameworkBr; FrameworkGzip = $frameworkGz }
    Packages = $packages
}
[IO.File]::WriteAllText($jsonPath, ($measurement | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))

# Top 20 largest framework files by raw size, with brotli sibling for comparison.
$topFiles = $frameworkRawFiles |
    Sort-Object Length -Descending |
    Select-Object -First 20 |
    ForEach-Object {
        $brPath = "$($_.FullName).br"
        $brSize = if (Test-Path $brPath) { (Get-Item $brPath).Length } else { 0 }
        [pscustomobject]@{
            Name = $_.FullName.Substring($framework.Length + 1).Replace('\', '/')
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
[void]$sb.AppendLine("- Selected SDK: ``$sdkVersion``")
[void]$sb.AppendLine("- Source directory: ``$repoRoot``")
[void]$sb.AppendLine("- Cache policy: $($measurement.CachePolicy)")
[void]$sb.AppendLine("- Exact sizes, resolved packages, and SDK/workload details: ``$Label.publish.json``")
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
    $benchDir = Join-Path $PSScriptRoot 'tools' 'bench'
    Push-Location $benchDir
    try {
        node run-bench.mjs --label $Label --site $wwwroot --output $reportPath --runs $BenchmarkRuns
        if ($LASTEXITCODE -ne 0) {
            throw "Bench runner failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}
