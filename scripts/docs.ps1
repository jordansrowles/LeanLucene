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

.PARAMETER SkipBenchmarks
    Skip benchmark doc generation (faster builds when bench data has not changed).

.EXAMPLE
    .\scripts\docs.ps1
    Builds the documentation site into ./docs/site.

.EXAMPLE
    .\scripts\docs.ps1 -Serve
    Builds the site and serves it locally on http://localhost:8080.

.EXAMPLE
    .\scripts\docs.ps1 -MetadataOnly
    Generates API YAML metadata without building the full site.

.EXAMPLE
    .\scripts\docs.ps1 -SkipBenchmarks
    Builds the site without regenerating benchmark pages.
#>
param(
    [switch]$Serve,
    [switch]$MetadataOnly,
    [switch]$SkipBenchmarks
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
    $version = @(docfx --version 2>&1)[0]
    Write-Host "docfx found: $version" -ForegroundColor DarkGray
}

# ── Generate benchmark docs ───────────────────────────────────────────────────

if (-not $SkipBenchmarks) {
    Write-Host "Generating benchmark pages..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'generate-benchmark-docs.ps1')
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Benchmark doc generation failed (exit $LASTEXITCODE). Continuing without benchmark pages."
    }
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
