try {
    Push-Location .\Pkmds.Web
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
    dotnet watch run -c Debug -v n --no-hot-reload
}
finally {
    Pop-Location
}
