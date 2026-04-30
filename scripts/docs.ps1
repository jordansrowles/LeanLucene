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

function Clear-ApiMetadata {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) {
        New-Item -ItemType Directory -Path $apiDir | Out-Null
        return
    }

    Get-ChildItem $apiDir -Filter '*.yml' -File | Remove-Item -Force
    $tocPath = Join-Path $apiDir 'toc.yml'
    if (Test-Path $tocPath) {
        Remove-Item $tocPath -Force
    }
}

function Add-InternalApiBadges {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) { return }

    $lock = [char]::ConvertFromUtf32(0x1F512)
    $internalUids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

    foreach ($file in Get-ChildItem $apiDir -Filter '*.yml' -File) {
        if ($file.Name -eq 'toc.yml') { continue }

        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]](Get-Content $file.FullName))
        $currentUid = $null
        $fileInternalUids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

        foreach ($line in $lines) {
            if ($line -match '^- uid: (.+)$') {
                $currentUid = $Matches[1]
                continue
            }

            if ($currentUid -and $line -match '^\s+content(?:\.vb)?: .*\binternal\b') {
                [void]$fileInternalUids.Add($currentUid)
                [void]$internalUids.Add($currentUid)
            }
        }

        if ($fileInternalUids.Count -eq 0) { continue }

        $currentUid = $null
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^- uid: (.+)$') {
                $currentUid = $Matches[1]
                continue
            }

            if ($currentUid -and $fileInternalUids.Contains($currentUid) -and
                $lines[$i] -match '^(\s+name: )(.+)$' -and
                $lines[$i] -notmatch [regex]::Escape($lock)) {
                $lines[$i] = "$($Matches[1])$($Matches[2]) $lock"
            }
        }

        Set-Content -Path $file.FullName -Value $lines -Encoding utf8
    }

    if ($internalUids.Count -eq 0) { return }

    $tocPath = Join-Path $apiDir 'toc.yml'
    if (-not (Test-Path $tocPath)) { return }

    $tocLines = [System.Collections.Generic.List[string]]::new()
    $tocLines.AddRange([string[]](Get-Content $tocPath))
    $currentUid = $null
    for ($i = 0; $i -lt $tocLines.Count; $i++) {
        if ($tocLines[$i] -match '^\s+- uid: (.+)$') {
            $currentUid = $Matches[1]
            continue
        }

        if ($currentUid -and $internalUids.Contains($currentUid) -and
            $tocLines[$i] -match '^(\s+name: )(.+)$' -and
            $tocLines[$i] -notmatch [regex]::Escape($lock)) {
            $tocLines[$i] = "$($Matches[1])$($Matches[2]) $lock"
        }
    }

    Set-Content -Path $tocPath -Value $tocLines -Encoding utf8
}

function Remove-PrivateApiEntries {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) { return }

    $privateUids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $fileBlocks = @{}

    foreach ($file in Get-ChildItem $apiDir -Filter '*.yml' -File) {
        if ($file.Name -eq 'toc.yml') { continue }

        $lines = [string[]](Get-Content $file.FullName)
        $prefix = [System.Collections.Generic.List[string]]::new()
        $blocks = [System.Collections.Generic.List[object]]::new()
        $references = [System.Collections.Generic.List[string]]::new()
        $current = $null
        $inReferences = $false

        foreach ($line in $lines) {
            if ($line -eq 'references:') {
                if ($current -is [System.Collections.Generic.List[string]]) {
                    $blocks.Add($current)
                    $current = $null
                }

                $inReferences = $true
                $references.Add($line)
                continue
            }

            if ($inReferences) {
                $references.Add($line)
                continue
            }

            if ($line -match '^- uid: (.+)$') {
                if ($current -is [System.Collections.Generic.List[string]]) {
                    $blocks.Add($current)
                }

                $current = [System.Collections.Generic.List[string]]::new()
                $current.Add($line)
                continue
            }

            if ($current -is [System.Collections.Generic.List[string]]) {
                $current.Add($line)
            } else {
                $prefix.Add($line)
            }
        }

        if ($current -is [System.Collections.Generic.List[string]]) {
            $blocks.Add($current)
        }

        foreach ($block in $blocks) {
            $uid = $null
            foreach ($line in $block) {
                if ($line -match '^- uid: (.+)$') {
                    $uid = $Matches[1]
                    continue
                }

                if ($uid -and $line -match '^\s+content(?:\.vb)?: .*\b(private|Private)\b') {
                    [void]$privateUids.Add($uid)
                }
            }
        }

        $fileBlocks[$file.FullName] = [pscustomobject]@{
            Prefix = $prefix
            Blocks = $blocks
            References = $references
        }
    }

    if ($privateUids.Count -eq 0) { return }

    foreach ($entry in $fileBlocks.GetEnumerator()) {
        $out = [System.Collections.Generic.List[string]]::new()
        $out.AddRange([string[]]$entry.Value.Prefix)

        foreach ($block in $entry.Value.Blocks) {
            $uid = $null
            foreach ($line in $block) {
                if ($line -match '^- uid: (.+)$') {
                    $uid = $Matches[1]
                    break
                }
            }

            if ($uid -and $privateUids.Contains($uid)) {
                continue
            }

            foreach ($line in $block) {
                if ($line -notmatch '^\s+- (.+)$' -or -not $privateUids.Contains($Matches[1])) {
                    $out.Add($line)
                }
            }
        }

        $out.AddRange([string[]]$entry.Value.References)
        Set-Content -Path $entry.Key -Value $out -Encoding utf8
    }

    $tocPath = Join-Path $apiDir 'toc.yml'
    if (-not (Test-Path $tocPath)) { return }

    $tocLines = [string[]](Get-Content $tocPath)
    $outToc = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $tocLines.Length; $i++) {
        if ($tocLines[$i] -match '^(\s*)- uid: (.+)$' -and $privateUids.Contains($Matches[2])) {
            $indent = $Matches[1].Length
            $i++
            while ($i -lt $tocLines.Length) {
                if ($tocLines[$i] -match '^(\s*)- uid: ' -and $Matches[1].Length -le $indent) {
                    $i--
                    break
                }
                $i++
            }
            continue
        }

        $outToc.Add($tocLines[$i])
    }

    Set-Content -Path $tocPath -Value $outToc -Encoding utf8
}

function Remove-ExternalInheritedMembers {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) { return }

    foreach ($file in Get-ChildItem $apiDir -Filter '*.yml' -File) {
        if ($file.Name -eq 'toc.yml') { continue }

        $lines = [string[]](Get-Content $file.FullName)
        $out = [System.Collections.Generic.List[string]]::new()

        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -ne '  inheritedMembers:') {
                $out.Add($lines[$i])
                continue
            }

            $keptMembers = [System.Collections.Generic.List[string]]::new()
            $i++
            while ($i -lt $lines.Length -and $lines[$i] -match '^  - (.+)$') {
                if ($Matches[1].StartsWith('Rowles.LeanLucene.', [System.StringComparison]::Ordinal)) {
                    $keptMembers.Add($lines[$i])
                }
                $i++
            }

            if ($keptMembers.Count -gt 0) {
                $out.Add('  inheritedMembers:')
                $out.AddRange([string[]]$keptMembers)
            }

            $i--
        }

        Set-Content -Path $file.FullName -Value $out -Encoding utf8
    }
}

function Remove-ExternalInheritanceEntries {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) { return }

    $listNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    [void]$listNames.Add('inheritance')
    [void]$listNames.Add('implements')
    [void]$listNames.Add('derivedClasses')

    foreach ($file in Get-ChildItem $apiDir -Filter '*.yml' -File) {
        if ($file.Name -eq 'toc.yml') { continue }

        $lines = [string[]](Get-Content $file.FullName)
        $out = [System.Collections.Generic.List[string]]::new()

        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -notmatch '^  ([A-Za-z]+):$' -or -not $listNames.Contains($Matches[1])) {
                $out.Add($lines[$i])
                continue
            }

            $listName = $Matches[1]
            $keptMembers = [System.Collections.Generic.List[string]]::new()
            $i++
            while ($i -lt $lines.Length -and $lines[$i] -match '^  - (.+)$') {
                if ($Matches[1].StartsWith('Rowles.LeanLucene.', [System.StringComparison]::Ordinal)) {
                    $keptMembers.Add($lines[$i])
                }
                $i++
            }

            if ($keptMembers.Count -gt 0) {
                $out.Add("  ${listName}:")
                $out.AddRange([string[]]$keptMembers)
            }

            $i--
        }

        Set-Content -Path $file.FullName -Value $out -Encoding utf8
    }
}

function Remove-ExternalReferenceEntries {
    $apiDir = Join-Path $docsDir "api"
    if (-not (Test-Path $apiDir)) { return }

    foreach ($file in Get-ChildItem $apiDir -Filter '*.yml' -File) {
        if ($file.Name -eq 'toc.yml') { continue }

        $lines = [string[]](Get-Content $file.FullName)
        $out = [System.Collections.Generic.List[string]]::new()
        $inReferences = $false
        $currentBlock = $null
        $currentUid = $null
        $referencesAdded = $false

        foreach ($line in $lines) {
            if (-not $inReferences) {
                if ($line -eq 'references:') {
                    $inReferences = $true
                    continue
                }

                $out.Add($line)
                continue
            }

            if ($line -match '^- uid: (.+)$') {
                if ($currentBlock -is [System.Collections.Generic.List[string]] -and
                    $currentUid.StartsWith('Rowles.LeanLucene.', [System.StringComparison]::Ordinal)) {
                    if (-not $referencesAdded) {
                        $out.Add('references:')
                        $referencesAdded = $true
                    }
                    $out.AddRange([string[]]$currentBlock)
                }

                $currentBlock = [System.Collections.Generic.List[string]]::new()
                $currentBlock.Add($line)
                $currentUid = $Matches[1]
                continue
            }

            if ($currentBlock -is [System.Collections.Generic.List[string]]) {
                $currentBlock.Add($line)
            }
        }

        if ($currentBlock -is [System.Collections.Generic.List[string]] -and
            $currentUid.StartsWith('Rowles.LeanLucene.', [System.StringComparison]::Ordinal)) {
            if (-not $referencesAdded) {
                $out.Add('references:')
                $referencesAdded = $true
            }
            $out.AddRange([string[]]$currentBlock)
        }

        Set-Content -Path $file.FullName -Value $out -Encoding utf8
    }
}

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
    Clear-ApiMetadata
    docfx metadata $docfxJson
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Remove-ExternalInheritedMembers
    Remove-ExternalInheritanceEntries
    Remove-PrivateApiEntries
    Remove-ExternalReferenceEntries
    Add-InternalApiBadges
    Write-Host "Metadata written to: $docsDir\api" -ForegroundColor Green
    exit 0
}

Write-Host "Generating API metadata..." -ForegroundColor Cyan
Clear-ApiMetadata
docfx metadata $docfxJson
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Remove-ExternalInheritedMembers
Remove-ExternalInheritanceEntries
Remove-PrivateApiEntries
Remove-ExternalReferenceEntries
Add-InternalApiBadges

if ($Serve) {
    Write-Host "Building and serving docs on http://localhost:8080..." -ForegroundColor Cyan
    docfx build $docfxJson --serve
} else {
    Write-Host "Building documentation site..." -ForegroundColor Cyan
    docfx build $docfxJson
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Site written to: $docsDir\site" -ForegroundColor Green
}
