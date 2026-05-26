# Extract AzureFrontDoor.Backend CIDR blocks from Microsoft's weekly Service Tags JSON.
# Use output to populate backend_ingress_allowed_cidrs in terraform.prod.tfvars / TERRAFORM_PROD_TFVARS.
#
# Usage:
#   1. Download https://www.microsoft.com/en-us/download/details.aspx?id=56519 (ServiceTags_Public_*.json)
#   2. .\scripts\extract-front-door-backend-cidrs.ps1 -ServiceTagsPath "C:\path\ServiceTags_Public_20260520.json"
#   3. Copy the HCL block into terraform.prod.tfvars, set backend_ingress_ip_restrictions_enabled = true

param(
	[Parameter(Mandatory = $true)]
	[string]$ServiceTagsPath,

	[string]$Region = "uksouth"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ServiceTagsPath)) {
	throw "Service tags file not found: $ServiceTagsPath"
}

$json = Get-Content -LiteralPath $ServiceTagsPath -Raw | ConvertFrom-Json
$tag = $json.values | Where-Object { $_.name -eq "AzureFrontDoor.Backend" } | Select-Object -First 1

if (-not $tag) {
	throw "AzureFrontDoor.Backend not found in $ServiceTagsPath"
}

$properties = $tag.properties
$prefixes = @()

if ($properties.region -and $properties.region.ToLowerInvariant() -eq $Region.ToLowerInvariant()) {
	$prefixes = $properties.addressPrefixes
}
elseif ($properties.regions) {
	$regionBlock = $properties.regions | Where-Object { $_.region.ToLowerInvariant() -eq $Region.ToLowerInvariant() } | Select-Object -First 1
	if ($regionBlock) {
		$prefixes = $regionBlock.addressPrefixes
	}
}

if ($prefixes.Count -eq 0) {
	# Fall back to global prefixes on the tag
	$prefixes = $properties.addressPrefixes
}

if ($prefixes.Count -eq 0) {
	throw "No address prefixes found for AzureFrontDoor.Backend (region: $Region)."
}

Write-Host "# AzureFrontDoor.Backend prefixes for region '$Region' ($($prefixes.Count) CIDRs)" -ForegroundColor Cyan
Write-Host "backend_ingress_ip_restrictions_enabled = true"
Write-Host "backend_ingress_allowed_cidrs = ["

$index = 0
foreach ($cidr in $prefixes) {
	$index++
	$name = "fd-backend-{0:D3}" -f $index
	Write-Host "  { name = `"$name`", cidr = `"$cidr`" },"
}

Write-Host "]"
