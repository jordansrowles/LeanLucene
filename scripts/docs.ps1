<#
.SYNOPSIS
    Sets up and builds the LeanLucene documentation site using DocFX.

.DESCRIPTION
    Ensures the docfx global tool is installed, then generates API metadata
    and builds the static site into ./docs/site.

.PARAMETER Serve
    After building, start a local web server at http://localhost:8080.

.PARAMETER MetadataOnly
    Only generate API metadata (docs/api/*.yml); skip the site build.

.EXAMPLE
    .\scripts\docs.ps1
    Builds the documentation site into ./docs/site.

.EXAMPLE
    .\scripts\docs.ps1 -Serve
    Builds the site and serves it locally on http://localhost:8080.

.EXAMPLE
    .\scripts\docs.ps1 -MetadataOnly
    Generates API YAML metadata without building the full site.
#>
param(
    [switch]$Serve,
    [switch]$MetadataOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$docsDir  = Join-Path $repoRoot "docs"
$docfxJson = Join-Path $docsDir "docfx.json"

# ── Ensure docfx is available ─────────────────────────────────────────────────

if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) {
    Write-Host "Installing docfx global tool..." -ForegroundColor Cyan
    dotnet tool install -g docfx
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install docfx. Ensure the .NET SDK is on PATH."
        exit 1
    }
    Write-Host "docfx installed." -ForegroundColor Green
} else {
    $version = (docfx --version 2>&1 | Select-Object -First 1)
    Write-Host "docfx found: $version" -ForegroundColor DarkGray
}

# ── Build ─────────────────────────────────────────────────────────────────────

if ($MetadataOnly) {
    Write-Host "Generating API metadata..." -ForegroundColor Cyan
    docfx metadata $docfxJson
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Metadata written to: $docsDir\api" -ForegroundColor Green
    exit 0
}

if ($Serve) {
    Write-Host "Building and serving docs on http://localhost:8080..." -ForegroundColor Cyan
    docfx $docfxJson --serve
} else {
    Write-Host "Building documentation site..." -ForegroundColor Cyan
    docfx build $docfxJson
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Site written to: $docsDir\site" -ForegroundColor Green
}
