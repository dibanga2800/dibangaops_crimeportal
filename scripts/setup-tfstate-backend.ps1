# Creates a dedicated Terraform remote state backend in Azure Storage.
# Use when TF_STATE_* secrets point at a deleted resource group (e.g. legacy crimeportal-rg).
#
# After running, set GitHub secrets:
#   TF_STATE_RESOURCE_GROUP  = output ResourceGroup
#   TF_STATE_STORAGE_ACCOUNT = output StorageAccount
#   TF_STATE_CONTAINER       = tfstate
#   TF_STATE_KEY_V2          = crimeportal-prod-v2.tfstate
#
# WARNING: A new empty state blob does NOT restore prod-v2 infrastructure state.
# If the old blob was deleted with legacy crimeportal-rg, recover it from Portal
# (Storage accounts → Manage deleted accounts) BEFORE running Deploy Full Stack.
# Applying with empty state against live prod-v2 can plan duplicate resources.

param(
	[string]$ResourceGroup = 'crimeportal-tfstate-rg',
	[string]$Location = 'uksouth',
	[string]$ContainerName = 'tfstate',
	[switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

function Get-RandomSuffix {
	$chars = 'abcdefghijklmnopqrstuvwxyz0123456789'.ToCharArray()
	-join (1..5 | ForEach-Object { $chars | Get-Random })
}

$suffix = Get-RandomSuffix
$storageAccount = "crimeportaltfstate$suffix".Substring(0, [Math]::Min(24, "crimeportaltfstate$suffix".Length))

Write-Host "Terraform state backend setup" -ForegroundColor Cyan
Write-Host "  Resource group : $ResourceGroup"
Write-Host "  Location       : $Location"
Write-Host "  Storage account: $storageAccount"
Write-Host "  Container      : $ContainerName"

if ($WhatIf) {
	Write-Host '[WhatIf] Would create RG, storage account, and container.' -ForegroundColor Yellow
	exit 0
}

if (-not (az group show --name $ResourceGroup 2>$null)) {
	Write-Host "Creating resource group $ResourceGroup..." -ForegroundColor Cyan
	az group create --name $ResourceGroup --location $Location | Out-Null
}

Write-Host "Creating storage account $storageAccount..." -ForegroundColor Cyan
az storage account create `
	--resource-group $ResourceGroup `
	--name $storageAccount `
	--location $Location `
	--sku Standard_LRS `
	--kind StorageV2 `
	--min-tls-version TLS1_2 `
	--allow-blob-public-access false | Out-Null

$key = az storage account keys list --resource-group $ResourceGroup --account-name $storageAccount --query '[0].value' -o tsv

Write-Host "Creating container $ContainerName..." -ForegroundColor Cyan
az storage container create `
	--name $ContainerName `
	--account-name $storageAccount `
	--account-key $key | Out-Null

Write-Host ''
Write-Host '=== GitHub secrets (Settings → Secrets → Actions) ===' -ForegroundColor Green
Write-Host "TF_STATE_RESOURCE_GROUP  = $ResourceGroup"
Write-Host "TF_STATE_STORAGE_ACCOUNT = $storageAccount"
Write-Host "TF_STATE_CONTAINER       = $ContainerName"
Write-Host 'TF_STATE_KEY_V2          = crimeportal-prod-v2.tfstate'
Write-Host ''
Write-Host 'Next: recover crimeportal-prod-v2.tfstate into this container if possible, then Deploy Full Stack → prod-v2.' -ForegroundColor Yellow
