<#
.SYNOPSIS
    Runs the Blazor WASM dev server pointing at a local PKHeX.Core source checkout.

.DESCRIPTION
    Use this instead of watch.ps1 when you need to manually test the UI against a
    local PKHeX dev build. Passes -p:UseLocalPKHeX=true to dotnet watch run, which
    swaps the NuGet package for a ProjectReference (see Directory.Build.targets).

.PARAMETER PKHeXSourcePath
    Optional path to PKHeX.Core.csproj. Defaults to the OS-appropriate path
    defined in Directory.Build.targets (C:\Code\PKHeX on Windows,
    ~/Code/codemonkey85/PKHeX on macOS/Linux).

.EXAMPLE
    # Use default path
    ./watch-local-pkhex.ps1

.EXAMPLE
    # Use a custom path (e.g. a different branch checkout)
    ./watch-local-pkhex.ps1 -PKHeXSourcePath C:\Code\PKHeX-dev\PKHeX.Core\PKHeX.Core.csproj
#>
param(
    [string]$PKHeXSourcePath = ""
)

$extraArgs = @("-p:UseLocalPKHeX=true")
if ($PKHeXSourcePath -ne "") {
    $extraArgs += "-p:PKHeXSourcePath=$PKHeXSourcePath"
}

try {
    Push-Location .\Pkmds.Web
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
    dotnet watch --non-interactive run -c Debug -v n @extraArgs
}
finally {
    Pop-Location
}
