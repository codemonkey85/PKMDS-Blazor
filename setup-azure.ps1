<#
.SYNOPSIS
    Provisions all Azure resources needed for PKMDS bug reporting and wires up
    the GitHub webhook and Actions secret automatically.

.DESCRIPTION
    Creates:
      - Azure Resource Group
      - Azure Storage Account + private bug-reports blob container
      - Azure Functions App (Consumption plan, .NET 10 isolated)
    Configures:
      - All Function App application settings
      - CORS origins
      - Storage lifecycle rules for private attachments and contact records
    Builds and deploys:
      - Pkmds.Functions project via zip deploy
    Wires up:
      - GitHub Actions secret FUNCTION_APP_URL
      - GitHub webhook (issues events only, HMAC-SHA256 signed)

.PARAMETER ResourceGroup
    Name of the Azure resource group to create (default: pkmds-bug-reports)

.PARAMETER Location
    Azure region (default: eastus)

.PARAMETER StorageAccount
    Storage account name — must be globally unique, 3–24 lowercase alphanumeric chars
    (default: pkmdsbugreports)

.PARAMETER FunctionsApp
    Function App name — must be globally unique (default: pkmds-functions)

.PARAMETER SkipDeploy
    Skip building and deploying the Functions project (useful if re-running just
    to update settings or re-create the webhook)

.EXAMPLE
    ./setup-azure.ps1

.EXAMPLE
    ./setup-azure.ps1 -StorageAccount myuniquestorage123 -FunctionsApp my-pkmds-functions

.EXAMPLE
    ./setup-azure.ps1 -SkipDeploy
#>
param(
    [string]$ResourceGroup  = "pkmds-bug-reports",
    [string]$Location       = "eastus",
    [string]$StorageAccount = "pkmdsbugreports",
    [string]$FunctionsApp   = "pkmds-functions",
    [string]$BlobContainer  = "bug-reports",
    [string]$GitHubOwner    = "codemonkey85",
    [string]$GitHubRepo     = "PKMDS-Blazor",
    [switch]$SkipDeploy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Helpers ────────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) {
    Write-Host "`n▶ $msg" -ForegroundColor Cyan
}

function Write-Done([string]$msg) {
    Write-Host "  ✓ $msg" -ForegroundColor Green
}

function Write-Info([string]$msg) {
    Write-Host "  · $msg" -ForegroundColor Gray
}

# ── Prerequisites ──────────────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

foreach ($cmd in @("az", "gh", "dotnet")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Error "'$cmd' is not on PATH. Please install it and re-run."
    }
}

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in to Azure. Run 'az login' first."
}
Write-Done "Azure: logged in as $($account.user.name) (subscription: $($account.name))"

$ghUser = gh api user --jq .login 2>$null
if (-not $ghUser) {
    Write-Error "Not logged in to GitHub CLI. Run 'gh auth login' first."
}
Write-Done "GitHub CLI: logged in as $ghUser"

# ── Resource Group ─────────────────────────────────────────────────────────────

Write-Step "Creating resource group '$ResourceGroup' in $Location"
az group create --name $ResourceGroup --location $Location --output none
Write-Done "Resource group ready"

# ── Storage Account ────────────────────────────────────────────────────────────

Write-Step "Creating storage account '$StorageAccount'"
az storage account create `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Standard_LRS `
    --kind StorageV2 `
    --allow-blob-public-access false `
    --min-tls-version TLS1_2 `
    --output none
Write-Done "Storage account created"

Write-Step "Creating blob container '$BlobContainer'"
az storage container create `
    --name $BlobContainer `
    --account-name $StorageAccount `
    --auth-mode login `
    --output none
Write-Done "Blob container ready"

Write-Step "Configuring private report-data retention"
$RetentionPolicy = @{
    rules = @(
        @{
            enabled = $true
            name = "delete-report-attachments-after-30-days"
            type = "Lifecycle"
            definition = @{
                # Lifecycle evaluation is daily, so use a one-day buffer to keep the
                # effective deletion ceiling at 30 days.
                actions = @{ baseBlob = @{ delete = @{ daysAfterCreationGreaterThan = 29 } } }
                filters = @{
                    blobTypes = @("blockBlob")
                    prefixMatch = @("$BlobContainer/attachments/")
                }
            }
        },
        @{
            enabled = $true
            name = "delete-legacy-report-attachments-after-30-days"
            type = "Lifecycle"
            definition = @{
                actions = @{ baseBlob = @{ delete = @{ daysAfterCreationGreaterThan = 29 } } }
                filters = @{
                    blobTypes = @("blockBlob")
                    prefixMatch = @(
                        "$BlobContainer/0",
                        "$BlobContainer/1",
                        "$BlobContainer/2",
                        "$BlobContainer/3",
                        "$BlobContainer/4",
                        "$BlobContainer/5",
                        "$BlobContainer/6",
                        "$BlobContainer/7",
                        "$BlobContainer/8",
                        "$BlobContainer/9"
                    )
                }
            }
        },
        @{
            enabled = $true
            name = "delete-closed-issue-markers-after-30-days"
            type = "Lifecycle"
            definition = @{
                actions = @{ baseBlob = @{ delete = @{ daysAfterCreationGreaterThan = 29 } } }
                filters = @{
                    blobTypes = @("blockBlob")
                    prefixMatch = @("$BlobContainer/issues/closed/")
                }
            }
        },
        @{
            enabled = $true
            name = "delete-open-contact-records-after-12-months"
            type = "Lifecycle"
            definition = @{
                actions = @{ baseBlob = @{ delete = @{ daysAfterCreationGreaterThan = 364 } } }
                filters = @{
                    blobTypes = @("blockBlob")
                    prefixMatch = @("$BlobContainer/contacts/open/")
                }
            }
        },
        @{
            enabled = $true
            name = "delete-closed-contact-records-after-30-days"
            type = "Lifecycle"
            definition = @{
                actions = @{ baseBlob = @{ delete = @{ daysAfterCreationGreaterThan = 29 } } }
                filters = @{
                    blobTypes = @("blockBlob")
                    prefixMatch = @("$BlobContainer/contacts/closed/")
                }
            }
        }
    )
} | ConvertTo-Json -Depth 10 -Compress

az storage account management-policy create `
    --account-name $StorageAccount `
    --resource-group $ResourceGroup `
    --policy $RetentionPolicy `
    --output none
Write-Done "Private attachments: 30 days maximum; contact records: 30 days after close or 12 months maximum"

$StorageConnString = az storage account show-connection-string `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv
Write-Done "Got storage connection string"

# ── Functions App ──────────────────────────────────────────────────────────────

Write-Step "Creating Function App '$FunctionsApp' (Consumption plan, .NET 10 isolated)"
az functionapp create `
    --name $FunctionsApp `
    --resource-group $ResourceGroup `
    --storage-account $StorageAccount `
    --consumption-plan-location $Location `
    --runtime dotnet-isolated `
    --runtime-version 10 `
    --functions-version 4 `
    --os-type Windows `
    --output none
Write-Done "Function App created"

$FunctionAppUrl = "https://$(az functionapp show `
    --name $FunctionsApp `
    --resource-group $ResourceGroup `
    --query defaultHostName `
    --output tsv)"
Write-Info "URL: $FunctionAppUrl"

# ── GitHub PAT ─────────────────────────────────────────────────────────────────

Write-Step "GitHub Personal Access Token"
Write-Host @"

  Create a fine-grained PAT at:
    https://github.com/settings/personal-access-tokens/new

  Settings:
    Repository access : Only selected → $GitHubOwner/$GitHubRepo
    Permissions       : Issues (Read & Write), Metadata (Read)

"@ -ForegroundColor Yellow

$GitHubPatSecure = Read-Host "  Paste your PAT here" -AsSecureString
$GitHubPat = [System.Net.NetworkCredential]::new("", $GitHubPatSecure).Password
if ([string]::IsNullOrWhiteSpace($GitHubPat)) {
    Write-Error "PAT cannot be empty."
}

# ── Webhook Secret ─────────────────────────────────────────────────────────────

Write-Step "Generating webhook secret"
$randomBytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$WebhookSecret = [System.BitConverter]::ToString($randomBytes).Replace("-", "").ToLower()
Write-Done "Secret generated"

# ── App Settings ───────────────────────────────────────────────────────────────

Write-Step "Configuring Function App application settings"
az functionapp config appsettings set `
    --name $FunctionsApp `
    --resource-group $ResourceGroup `
    --settings `
        "GitHubPat=$GitHubPat" `
        "GitHubOwner=$GitHubOwner" `
        "GitHubRepo=$GitHubRepo" `
        "AzureStorageConnectionString=$StorageConnString" `
        "BlobContainerName=$BlobContainer" `
        "GitHubWebhookSecret=$WebhookSecret" `
    --output none
Write-Done "Application settings saved"

# ── CORS ───────────────────────────────────────────────────────────────────────

Write-Step "Configuring CORS"
# Platform-level CORS only supports exact origins (no wildcards for subdomains).
# UAT preview URLs (*.azurestaticapps.net) and localhost are handled by the
# code-level CORS middleware in Program.cs via SetIsOriginAllowed.
# Here we register only the stable production origin at the platform level.
az functionapp cors add `
    --name $FunctionsApp `
    --resource-group $ResourceGroup `
    --allowed-origins "https://codemonkey85.github.io" `
    --output none
Write-Done "Allowed origin: https://codemonkey85.github.io"

# ── Build & Deploy ─────────────────────────────────────────────────────────────

if (-not $SkipDeploy) {
    Write-Step "Building Pkmds.Functions"
    $publishDir = Join-Path $PSScriptRoot ".azure-deploy\functions"
    $zipPath    = Join-Path $PSScriptRoot ".azure-deploy\functions.zip"

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    if (Test-Path $zipPath)    { Remove-Item $zipPath    -Force }

    dotnet publish "$PSScriptRoot\Pkmds.Functions\Pkmds.Functions.csproj" `
        -c Release -o $publishDir --nologo
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed." }
    Write-Done "Build complete"

    Write-Step "Creating zip package"
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
    Write-Done "Zip created at $zipPath"

    Write-Step "Deploying to Azure"
    az functionapp deployment source config-zip `
        --name $FunctionsApp `
        --resource-group $ResourceGroup `
        --src $zipPath `
        --output none
    Write-Done "Functions deployed"
} else {
    Write-Info "Skipping deploy (-SkipDeploy flag set)"
}

# ── GitHub Actions Secret ──────────────────────────────────────────────────────

Write-Step "Setting GitHub Actions secret FUNCTION_APP_URL"
$FunctionAppUrl | gh secret set FUNCTION_APP_URL --repo "$GitHubOwner/$GitHubRepo"
Write-Done "Secret FUNCTION_APP_URL set on $GitHubOwner/$GitHubRepo"

# ── GitHub Webhook ─────────────────────────────────────────────────────────────

Write-Step "Creating GitHub webhook"
$webhookPayload = @{
    name   = "web"
    active = $true
    events = @("issues")
    config = @{
        url          = "$FunctionAppUrl/api/GitHubWebhook"
        content_type = "json"
        secret       = $WebhookSecret
        insecure_ssl = "0"
    }
} | ConvertTo-Json -Depth 3

$webhookPayload | gh api "repos/$GitHubOwner/$GitHubRepo/hooks" `
    --method POST `
    --input - `
    --jq ".id" | ForEach-Object { Write-Done "Webhook created (id: $_)" }

# ── Summary ────────────────────────────────────────────────────────────────────

Write-Host "`n" + ("─" * 60) -ForegroundColor DarkGray
Write-Host "  Setup complete!" -ForegroundColor Green
Write-Host ("─" * 60) -ForegroundColor DarkGray
Write-Host @"

  Resource group  : $ResourceGroup
  Storage account : $StorageAccount
  Blob container  : $BlobContainer
  Function App    : $FunctionAppUrl

  GitHub Actions secret FUNCTION_APP_URL is set.
  GitHub webhook is active on '$GitHubOwner/$GitHubRepo' (issues events).

  Next step: merge PR #619 and push to main — the deploy workflow will
  inject the Function App URL into appsettings.json automatically.

"@ -ForegroundColor White
