param(
    [string]$CredentialPath = (Join-Path $PSScriptRoot '../../artifacts/sprites/r2-credentials.xml'),
    [Parameter(Mandatory = $true)]
    [string]$RclonePath,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$PublisherArguments
)

$ErrorActionPreference = 'Stop'
$credential = Import-Clixml -LiteralPath $CredentialPath
$settings = @{
    RCLONE_CONFIG_PKMDSSPRITES_TYPE = 's3'
    RCLONE_CONFIG_PKMDSSPRITES_PROVIDER = 'Cloudflare'
    RCLONE_CONFIG_PKMDSSPRITES_ACCESS_KEY_ID = $credential.UserName
    RCLONE_CONFIG_PKMDSSPRITES_SECRET_ACCESS_KEY = $credential.GetNetworkCredential().Password
    RCLONE_CONFIG_PKMDSSPRITES_ENDPOINT = 'https://5a09f006ed56baba01325764d8215ca7.r2.cloudflarestorage.com'
    RCLONE_CONFIG_PKMDSSPRITES_REGION = 'auto'
    RCLONE_CONFIG_PKMDSSPRITES_NO_CHECK_BUCKET = 'true'
}
$previous = @{}
try {
    foreach ($name in $settings.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $settings[$name], 'Process')
    }
    & node (Join-Path $PSScriptRoot 'publish.mjs') @PublisherArguments --rclone $RclonePath
    if ($LASTEXITCODE -ne 0) { throw "Sprite publisher exited with code $LASTEXITCODE" }
}
finally {
    foreach ($name in $settings.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
