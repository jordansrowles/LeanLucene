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
    Valid values: all, index, query, analysis, boolean, phrase, prefix,
    fuzzy, wildcard, deletion, smallindex.

.PARAMETER Strat
    Predefined strategy that configures DocCount and BDN job.
    Valid values: default, fast, quick-compare, intense, stress.
      default       - No overrides, uses BDN defaults.
      fast          - 500 docs, --job dry (minimal smoke-test).
      quick-compare - 1000 docs, --job short (quick comparison).
      intense       - 10000 docs, default BDN job.
      stress        - 50000 docs, default BDN job.

.PARAMETER DocCount
    Override document count for all suites. Overrides the count set by -Strat.
    Passed as BENCH_DOC_COUNT environment variable and --doccount arg.

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
    Runs all benchmark suites with default settings.

.EXAMPLE
    .\scripts\benchmark.ps1 --suite query
    Runs only the TermQuery benchmark suite.

.EXAMPLE
    .\scripts\benchmark.ps1 --strat fast --suite boolean
    Runs BooleanQuery benchmarks with 500 docs and --job dry.

.EXAMPLE
    .\scripts\benchmark.ps1 --strat intense --doccount 20000
    Runs all suites with 20K docs (overrides intense default of 10K).

.EXAMPLE
    .\scripts\benchmark.ps1 --doccount 5000
    Runs all suites with 5K document count.

.EXAMPLE
    .\scripts\benchmark.ps1 --list
    Lists all available benchmark suites.

.EXAMPLE
    .\scripts\benchmark.ps1 --dry --suite index --strat fast
    Prints the command that would run without executing it.
#>
param(
    [ValidateSet('all', 'index', 'query', 'analysis', 'boolean', 'phrase',
                 'prefix', 'fuzzy', 'wildcard', 'deletion', 'smallindex')]
    [string]$Suite = 'all',

    [ValidateSet('default', 'fast', 'quick-compare', 'intense', 'stress')]
    [string]$Strat = 'default',

    [int]$DocCount = 0,

    [switch]$Help,

    [switch]$List,

    [switch]$Dry,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$suiteDescriptions = [ordered]@{
    all        = 'Run all benchmark suites (default)'
    index      = 'IndexingBenchmarks       - bulk indexing throughput (vs Lucene.NET + Lifti)'
    query      = 'TermQueryBenchmarks      - single-term search (vs Lucene.NET + Lifti)'
    analysis   = 'AnalysisBenchmarks       - tokenisation pipeline (vs Lucene.NET)'
    boolean    = 'BooleanQueryBenchmarks   - Must / Should / MustNot (vs Lucene.NET + Lifti)'
    phrase     = 'PhraseQueryBenchmarks    - exact and slop phrase (vs Lucene.NET)'
    prefix     = 'PrefixQueryBenchmarks    - prefix matching (vs Lucene.NET + Lifti)'
    fuzzy      = 'FuzzyQueryBenchmarks     - fuzzy/edit-distance (vs Lucene.NET + Lifti)'
    wildcard   = 'WildcardQueryBenchmarks  - wildcard patterns (vs Lucene.NET)'
    deletion   = 'DeletionBenchmarks       - delete throughput (vs Lucene.NET + Lifti)'
    smallindex = 'SmallIndexBenchmarks     - 100-doc roundtrip overhead (index + search)'
}

$stratDescriptions = [ordered]@{
    'default'       = 'No overrides, uses BDN defaults'
    'fast'          = '500 docs, --job dry (minimal smoke-test)'
    'quick-compare' = '1000 docs, --job short (quick comparison)'
    'intense'       = '10000 docs, default BDN job'
    'stress'        = '50000 docs, default BDN job'
}

if ($Help) {
    Write-Host ''
    Write-Host '  LeanLucene Benchmark Runner'
    Write-Host '  ============================'
    Write-Host ''
    Write-Host '  Usage:'
    Write-Host '    .\scripts\benchmark.ps1 [options] [BenchmarkDotNet args...]'
    Write-Host ''
    Write-Host '  Options:'
    Write-Host '    --suite <name>     Run a specific benchmark suite (default: all)'
    Write-Host '    --strat <name>     Use a predefined strategy (default: default)'
    Write-Host '    --doccount <n>     Override document count (overrides -Strat doc count)'
    Write-Host '    --list             List available suites and strategies and exit'
    Write-Host '    --dry              Print the command that would run without executing it'
    Write-Host '    --help             Show this help message and exit'
    Write-Host ''
    Write-Host '  Suites:'
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $suiteDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Strategies (-Strat):'
    foreach ($name in $stratDescriptions.Keys) {
        Write-Host ("    {0,-16} {1}" -f $name, $stratDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Output:'
    Write-Host '    bench/data/<timestamp>-<commit>/<suite>/'
    Write-Host '      *.json   Full BenchmarkDotNet results (JsonExporterAttribute.Full)'
    Write-Host '      *.md     GitHub Markdown table (MarkdownExporterAttribute.GitHub)'
    Write-Host '      *.html   BenchmarkDotNet HTML report (HtmlExporter)'
    Write-Host '    bench/data/<timestamp>-<commit>.json   Consolidated run report'
    Write-Host '    bench/data/index.json                  Run index for the web viewer'
    Write-Host ''
    Write-Host '  Viewer:'
    Write-Host '    Start with:  node bench/server.js'
    Write-Host '    Open:        http://localhost:4173'
    Write-Host ''
    Write-Host '  BenchmarkDotNet pass-through examples:'
    Write-Host '    --filter *Lean*            Run only methods whose name contains Lean'
    Write-Host '    --job short                Use the Short job instead of Default'
    Write-Host '    --runtimes net10.0         Override the target runtime'
    Write-Host '    --memory true              Force memory diagnoser (already enabled)'
    Write-Host '    --exporters json           Add extra exporters (json, html, csv, markdown)'
    Write-Host '    --artifacts <path>         Override output directory'
    Write-Host ''
    Write-Host '  Examples:'
    Write-Host '    .\scripts\benchmark.ps1                                    # all suites'
    Write-Host '    .\scripts\benchmark.ps1 --suite query                      # query only'
    Write-Host '    .\scripts\benchmark.ps1 --strat fast --suite boolean       # fast strategy'
    Write-Host '    .\scripts\benchmark.ps1 --strat intense --doccount 20000   # intense + override'
    Write-Host '    .\scripts\benchmark.ps1 --doccount 5000                    # custom doc count'
    Write-Host '    .\scripts\benchmark.ps1 --list                             # list suites'
    Write-Host '    .\scripts\benchmark.ps1 --dry --suite index --strat fast   # dry run'
    Write-Host ''
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
    Write-Host "  Available strategies (-Strat):"
    Write-Host ""
    foreach ($name in $stratDescriptions.Keys) {
        Write-Host ("    {0,-16} {1}" -f $name, $stratDescriptions[$name])
    }
    Write-Host ""
    exit 0
}

# --- Resolve strategy presets ---
$stratDocCount = 0
$stratJobArgs = @()

switch ($Strat) {
    'fast' {
        $stratDocCount = 500
        $stratJobArgs = @('--job', 'dry')
    }
    'quick-compare' {
        $stratDocCount = 1000
        $stratJobArgs = @('--job', 'short')
    }
    'intense' {
        $stratDocCount = 10000
    }
    'stress' {
        $stratDocCount = 50000
    }
}

# DocCount parameter overrides strategy; if neither set, leave unset (BDN [Params] defaults)
$effectiveDocCount = 0
if ($DocCount -gt 0) {
    $effectiveDocCount = $DocCount
} elseif ($stratDocCount -gt 0) {
    $effectiveDocCount = $stratDocCount
}

$projectPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\example\Rowles.LeanLucene.Example.Benchmarks\Rowles.LeanLucene.Example.Benchmarks.csproj")
)

if (-not (Test-Path $projectPath)) {
    Write-Error "Benchmark project not found at: $projectPath"
    exit 1
}

# Build argument list
$runArgs = @('--suite', $Suite)

if ($effectiveDocCount -gt 0) {
    $runArgs += @('--doccount', $effectiveDocCount.ToString())
    $env:BENCH_DOC_COUNT = $effectiveDocCount.ToString()
}

# Prepend strategy job args before user-supplied BDN args
$allExtraArgs = $stratJobArgs
if ($BenchmarkArgs) { $allExtraArgs += $BenchmarkArgs }

Write-Host "Suite:   $Suite"
Write-Host "Strat:   $Strat"
if ($effectiveDocCount -gt 0) {
    Write-Host "Docs:    $effectiveDocCount"
}
if ($allExtraArgs) {
    Write-Host "Extra:   $($allExtraArgs -join ' ')"
}

if ($Dry) {
    $cmdDisplay = "dotnet run -c Release --project `"$projectPath`" -- $($runArgs -join ' ')"
    if ($allExtraArgs) { $cmdDisplay += " $($allExtraArgs -join ' ')" }
    Write-Host ""
    Write-Host 'Dry run - command that would execute:'
    Write-Host "  $cmdDisplay"
    if ($effectiveDocCount -gt 0) {
        Write-Host "  env: BENCH_DOC_COUNT=$effectiveDocCount"
    }
    Write-Host ""
    exit 0
}

Write-Host ""
dotnet run -c Release --project $projectPath -- @runArgs @allExtraArgs

