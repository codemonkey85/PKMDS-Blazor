try {
    Push-Location .\Pkmds.Web
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
    dotnet watch --non-interactive run -c Debug -v n
}
finally {
    Pop-Location
}
