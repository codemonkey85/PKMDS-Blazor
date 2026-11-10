param(
    [string]$CredentialPath = (Join-Path $PSScriptRoot '../../artifacts/sprites/r2-credentials.xml')
)

$ErrorActionPreference = 'Stop'
$accessKeyId = Read-Host 'R2 Access Key ID'
$secretAccessKey = Read-Host 'R2 Secret Access Key' -AsSecureString
if ([string]::IsNullOrWhiteSpace($accessKeyId) -or $secretAccessKey.Length -eq 0) {
    throw 'Both R2 S3 credential values are required.'
}
$credential = [pscredential]::new($accessKeyId.Trim(), $secretAccessKey)
$credentialDirectory = Split-Path -Parent $CredentialPath
New-Item -ItemType Directory -Path $credentialDirectory -Force | Out-Null
# Windows DPAPI encrypts the secret for the current Windows user and this computer.
# artifacts/ is gitignored; no credential is added to the repository or printed.
$credential | Export-Clixml -LiteralPath $CredentialPath
Write-Host "Saved encrypted R2 credentials to $([IO.Path]::GetFullPath($CredentialPath))."
