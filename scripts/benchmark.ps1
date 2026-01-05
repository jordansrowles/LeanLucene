<#
.SYNOPSIS
    Unified LeanLucene benchmark runner.

.DESCRIPTION
    Runs BenchmarkDotNet suites for LeanLucene. Results are written to
    bench/data/<timestamp>-<commit>/<suite>/ with JSON, Markdown, and HTML
    output. A consolidated report and index.json are generated for the
    web-based benchmark viewer (bench/index.html).

.PARAMETER Suite
    Which benchmark suite to run. Default: all.
    Valid values: all, index, query, analysis, boolean, phrase, smallindex.

.PARAMETER Help
    Show usage information and exit.

.PARAMETER List
    List available benchmark suites and exit.

.PARAMETER Dry
    Print the dotnet command that would be run without executing it.

.PARAMETER BenchmarkArgs
    Additional arguments passed through to BenchmarkDotNet
    (e.g. --filter "*Lean*", --job short, --memory true).

.EXAMPLE
    .\scripts\benchmark.ps1
    Runs all benchmark suites.

.EXAMPLE
    .\scripts\benchmark.ps1 --suite query
    Runs only the TermQuery benchmark suite.

.EXAMPLE
    .\scripts\benchmark.ps1 --suite boolean --filter "*Must*"
    Runs BooleanQuery benchmarks filtered to methods containing "Must".

.EXAMPLE
    .\scripts\benchmark.ps1 --list
    Lists all available benchmark suites.

.EXAMPLE
    .\scripts\benchmark.ps1 --dry --suite index
    Prints the command that would run the index suite without executing it.
#>
param(
    [ValidateSet('all', 'index', 'query', 'analysis', 'boolean', 'phrase', 'smallindex')]
    [string]$Suite = 'all',

    [switch]$Help,

    [switch]$List,

    [switch]$Dry,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$suiteDescriptions = [ordered]@{
    all        = 'Run all benchmark suites (default)'
    index      = 'IndexingBenchmarks       — bulk indexing throughput (3K docs, vs Lucene.NET + Lifti)'
    query      = 'TermQueryBenchmarks      — single-term search (2K docs, vs Lucene.NET + Lifti)'
    analysis   = 'AnalysisBenchmarks       — tokenisation + stop-word pipeline throughput (1K docs)'
    boolean    = 'BooleanQueryBenchmarks   — Must / Should / MustNot queries (2K docs)'
    phrase     = 'PhraseQueryBenchmarks    — exact and slop phrase matching (2K docs)'
    smallindex = 'SmallIndexBenchmarks     — 100-doc roundtrip overhead (index + search)'
}

if ($Help) {
    Write-Host @"

  LeanLucene Benchmark Runner
  ============================

  Usage:
    .\scripts\benchmark.ps1 [options] [-- BenchmarkDotNet args]

  Options:
    --suite <name>   Run a specific benchmark suite (default: all)
    --list           List available suites and exit
    --dry            Print the command that would run without executing it
    --help           Show this help message and exit

  Suites:
"@
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $suiteDescriptions[$name])
    }
    Write-Host @"

  Output:
    bench/data/<timestamp>-<commit>/<suite>/
      *.json      Full BenchmarkDotNet results (JsonExporterAttribute.Full)
      *.md        GitHub-flavoured Markdown table (MarkdownExporterAttribute.GitHub)
      *.html      BenchmarkDotNet HTML report (HtmlExporter)
    bench/data/<timestamp>-<commit>.json   Consolidated run report
    bench/data/index.json                  Run index for the web viewer

  Viewer:
    Start with:  node bench/server.js
    Open:        http://localhost:4173

  BenchmarkDotNet pass-through examples:
    --filter "*Lean*"          Run only methods whose name contains "Lean"
    --job short                Use the Short job instead of Default
    --runtimes net10.0         Override the target runtime
    --memory true              Force memory diagnoser on (already enabled)
    --exporters json           Add extra exporters (json, html, csv, markdown)
    --artifacts <path>         Override output directory

  Examples:
    .\scripts\benchmark.ps1                                   # all suites
    .\scripts\benchmark.ps1 --suite query                     # query only
    .\scripts\benchmark.ps1 --suite boolean --filter "*Must*" # filtered
    .\scripts\benchmark.ps1 --list                            # list suites
    .\scripts\benchmark.ps1 --dry --suite index               # dry run

"@
    exit 0
}

if ($List) {
    Write-Host ""
    Write-Host "  Available benchmark suites:"
    Write-Host ""
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $suiteDescriptions[$name])
    }
    Write-Host ""
    exit 0
}

$projectPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\example\Rowles.LeanLucene.Example.Benchmarks\Rowles.LeanLucene.Example.Benchmarks.csproj")
)

if (-not (Test-Path $projectPath)) {
    Write-Error "Benchmark project not found at: $projectPath"
    exit 1
}

$cmd = "dotnet run -c Release --project `"$projectPath`" -- --suite $Suite"
if ($BenchmarkArgs) { $cmd += " $($BenchmarkArgs -join ' ')" }

Write-Host "Suite:   $Suite"
if ($BenchmarkArgs) {
    Write-Host "Extra:   $($BenchmarkArgs -join ' ')"
}

if ($Dry) {
    Write-Host ""
    Write-Host "Dry run — command that would execute:"
    Write-Host "  $cmd"
    Write-Host ""
    exit 0
}

Write-Host ""
dotnet run -c Release --project $projectPath -- --suite $Suite @BenchmarkArgs

