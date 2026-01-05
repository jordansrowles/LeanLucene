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
    Show this help message.

.PARAMETER BenchmarkArgs
    Additional arguments passed through to BenchmarkDotNet
    (e.g. --filter "*Lean*").

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
    .\scripts\benchmark.ps1 --help
    Shows usage information.
#>
param(
    [ValidateSet('all', 'index', 'query', 'analysis', 'boolean', 'phrase', 'smallindex')]
    [string]$Suite = 'all',

    [switch]$Help,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

if ($Help) {
    Write-Host @"

  LeanLucene Benchmark Runner
  ============================

  Usage:
    .\scripts\benchmark.ps1 [--suite <name>] [--help] [BenchmarkDotNet args...]

  Options:
    --suite <name>   Run a specific benchmark suite (default: all)
    --help           Show this help message

  Suites:
    all              Run all benchmark suites (default)
    index            IndexingBenchmarks - bulk indexing throughput (3K docs)
    query            TermQueryBenchmarks - single-term search with competitors
    analysis         AnalysisBenchmarks - tokenisation pipeline throughput
    boolean          BooleanQueryBenchmarks - Must/Should/MustNot queries
    phrase           PhraseQueryBenchmarks - exact and slop phrase matching
    smallindex       SmallIndexBenchmarks - 100-doc roundtrip overhead

  Output:
    Results are written to bench/data/<timestamp>-<commit>/<suite>/
    Each suite produces JSON, Markdown, and HTML reports.
    A consolidated report + index.json feed the viewer at bench/index.html.

  Examples:
    .\scripts\benchmark.ps1                                # all suites
    .\scripts\benchmark.ps1 --suite query                  # query only
    .\scripts\benchmark.ps1 --suite boolean --filter "*Must*"  # filtered
    .\scripts\benchmark.ps1 --help                         # this message

"@
    exit 0
}

$projectPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\example\Rowles.LeanLucene.Example.Benchmarks\Rowles.LeanLucene.Example.Benchmarks.csproj")
)

if (-not (Test-Path $projectPath)) {
    Write-Error "Benchmark project not found at: $projectPath"
    exit 1
}

Write-Host "Suite:   $Suite"
Write-Host "Project: $projectPath"
if ($BenchmarkArgs) {
    Write-Host "Extra:   $($BenchmarkArgs -join ' ')"
}
Write-Host ""

dotnet run -c Release --project $projectPath -- --suite $Suite @BenchmarkArgs
