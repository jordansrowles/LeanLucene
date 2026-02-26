<#
.SYNOPSIS
  Collates all benchmark JSON files (including archived) into a single JSON dataset.
.DESCRIPTION
  Scans bench/data/ and bench/.archive/ for run-level benchmark JSON files (schema v1 and v2),
  extracts run metadata and all benchmark metrics, and writes a unified JSON file.
.PARAMETER OutputPath
  Path to the output JSON file. Defaults to bench/collated.json.
.EXAMPLE
  .\scripts\collate-benchmarks.ps1
  .\scripts\collate-benchmarks.ps1 -OutputPath bench/my-collated.json
#>
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot 'bench\collated.json'
}
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path (Get-Location) $OutputPath
}

# Collect candidate JSON files from all run directories and archive
$searchDirs = @(
    (Join-Path $repoRoot 'bench\data\full'),
    (Join-Path $repoRoot 'bench\data\smoke'),
    (Join-Path $repoRoot 'bench\data\stress'),
    (Join-Path $repoRoot 'bench\data\partial'),
    (Join-Path $repoRoot 'bench\.archive')
)

$candidateFiles = @()
foreach ($dir in $searchDirs) {
    if (Test-Path $dir) {
        $candidateFiles += Get-ChildItem -Path $dir -Filter '*.json' -Recurse -File
    }
}

# Filter out index.json and BDN report-full.json files — keep only run-level JSONs
$candidateFiles = $candidateFiles | Where-Object {
    $_.Name -ne 'index.json' -and
    $_.Name -notlike '*-report-full.json' -and
    $_.Name -notlike '*-report.json'
}

Write-Host "Found $($candidateFiles.Count) candidate benchmark files"

$runs = [System.Collections.Generic.List[object]]::new()
$skipped = 0

foreach ($file in $candidateFiles) {
    try {
        $raw = Get-Content $file.FullName -Raw -Encoding UTF8
        $json = $raw | ConvertFrom-Json

        # Must have schemaVersion, suites array, and at least one benchmark to be a valid successful run
        if ($null -eq $json.schemaVersion -or $null -eq $json.suites) {
            $skipped++
            continue
        }
        $totalBenchmarks = ($json.suites | Measure-Object -Property benchmarkCount -Sum).Sum
        if ($totalBenchmarks -eq 0) {
            Write-Host "  [EMPTY] $($file.Name) - 0 benchmarks, skipping"
            $skipped++
            continue
        }

        # Determine source location relative to bench/
        $benchRoot = Join-Path $repoRoot 'bench'
        $relativePath = $file.FullName.Substring($benchRoot.Length + 1).Replace('\', '/')

        # Normalise run metadata across schema v1 and v2
        $runType = if ($json.PSObject.Properties['runType']) { $json.runType } else { 'unknown' }
        $commitHash = if ($json.PSObject.Properties['commitHash']) { $json.commitHash } else { $null }

        # Flatten benchmarks from all suites
        $benchmarks = [System.Collections.Generic.List[object]]::new()
        foreach ($suite in $json.suites) {
            foreach ($b in $suite.benchmarks) {
                $benchmarks.Add([ordered]@{
                    suite          = $suite.suiteName
                    key            = $b.key
                    typeName       = $b.typeName
                    methodName     = $b.methodName
                    parameters     = $b.parameters
                    statistics     = [ordered]@{
                        sampleCount    = $b.statistics.sampleCount
                        meanNs         = $b.statistics.meanNanoseconds
                        medianNs       = $b.statistics.medianNanoseconds
                        minNs          = $b.statistics.minNanoseconds
                        maxNs          = $b.statistics.maxNanoseconds
                        stdDevNs       = $b.statistics.standardDeviationNanoseconds
                        opsPerSec      = $b.statistics.operationsPerSecond
                    }
                    gc             = [ordered]@{
                        allocatedBytes = $b.gc.bytesAllocatedPerOperation
                        gen0           = $b.gc.gen0Collections
                        gen1           = $b.gc.gen1Collections
                        gen2           = $b.gc.gen2Collections
                    }
                })
            }
        }

        $suiteNames = @($json.suites | ForEach-Object { $_.suiteName })

        $runs.Add([ordered]@{
            runId           = $json.runId
            schemaVersion   = [int]$json.schemaVersion
            runType         = $runType
            generatedAtUtc  = $json.generatedAtUtc
            commitHash      = $commitHash
            hostMachineName = $json.hostMachineName
            dotnetVersion   = $json.dotnetVersion
            commandLineArgs = $json.commandLineArgs
            sourceFile      = $relativePath
            suiteNames      = $suiteNames
            benchmarkCount  = $benchmarks.Count
            benchmarks      = $benchmarks
        })

        $bc = $benchmarks.Count
        Write-Host "  [OK] $relativePath - $bc benchmarks, $runType, $commitHash"
    }
    catch {
        Write-Warning "  [SKIP] $($file.FullName): $_"
        $skipped++
    }
}

# Sort runs by generatedAtUtc ascending
$runs = $runs | Sort-Object { $_.generatedAtUtc }

$totalBenchmarkCount = 0
foreach ($r in $runs) { $totalBenchmarkCount += $r.benchmarkCount }

$output = [ordered]@{
    collatedAtUtc  = (Get-Date).ToUniversalTime().ToString('o')
    totalRuns      = $runs.Count
    totalBenchmarks = $totalBenchmarkCount
    skippedFiles   = $skipped
    machines       = @($runs | ForEach-Object { $_.hostMachineName } | Sort-Object -Unique)
    commits        = @($runs | Where-Object { $_.commitHash } | ForEach-Object { $_.commitHash } | Sort-Object -Unique)
    dotnetVersions = @($runs | ForEach-Object { $_.dotnetVersion } | Sort-Object -Unique)
    runs           = @($runs)
}

$jsonOut = $output | ConvertTo-Json -Depth 10 -Compress:$false
[System.IO.File]::WriteAllText($OutputPath, $jsonOut, [System.Text.UTF8Encoding]::new($false))

$tb = $output.totalBenchmarks
Write-Host ""
Write-Host "Collated $($runs.Count) runs - $tb total benchmarks -> $OutputPath"
Write-Host "Machines: $($output.machines -join ', ')"
Write-Host "Commits:  $($output.commits -join ', ')"
Write-Host "Skipped:  $skipped files"
