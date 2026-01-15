<#
.SYNOPSIS
    Unified LeanLucene benchmark runner.

.DESCRIPTION
    Runs BenchmarkDotNet suites for LeanLucene. Results are written to
    bench/data/<type>/<runId>/<suite>/ with JSON, Markdown, and HTML
    output. A consolidated report and index.json are generated at bench/data/.

.PARAMETER Suite
    Which benchmark suite to run. Default: all.
    Valid values: all, index, query, analysis, boolean, phrase, prefix,
    fuzzy, wildcard, deletion, smallindex.

.PARAMETER Type
    Run type that determines the output folder. Overrides auto-detection from -Strat.
    Valid values: full, smoke, stress, partial.
    Default mapping from -Strat: fast → smoke, quick-compare → partial,
    intense → full, stress → stress, default → full.

.PARAMETER Strat
    Predefined strategy that configures DocCount and BDN job.
    Valid values: default, fast, quick-compare, intense, stress.
      default       - No overrides, uses BDN defaults. Type: full.
      fast          - 500 docs, --job dry (minimal smoke-test). Type: smoke.
      quick-compare - 1000 docs, --job short (quick comparison). Type: partial.
      intense       - 10000 docs, default BDN job. Type: full.
      stress        - 50000 docs, default BDN job. Type: stress.

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
    Runs all benchmark suites with default settings (type: full).

.EXAMPLE
    .\scripts\benchmark.ps1 -Suite query
    Runs only the TermQuery benchmark suite.

.EXAMPLE
    .\scripts\benchmark.ps1 -Strat fast -Suite boolean
    Runs BooleanQuery benchmarks with 500 docs and --job dry (type: smoke).

.EXAMPLE
    .\scripts\benchmark.ps1 -Type partial -Suite analysis
    Runs analysis benchmarks and writes output to bench/data/partial/analysis/.

.EXAMPLE
    .\scripts\benchmark.ps1 -Strat intense -DocCount 20000
    Runs all suites with 20K docs (overrides intense default of 10K).

.EXAMPLE
    .\scripts\benchmark.ps1 -List
    Lists all available benchmark suites and strategies.

.EXAMPLE
    .\scripts\benchmark.ps1 -Dry -Suite index -Strat fast
    Prints the command that would run without executing it.
#>
param(
    [ValidateSet('all', 'index', 'query', 'analysis', 'boolean', 'phrase',
                 'prefix', 'fuzzy', 'wildcard', 'deletion', 'smallindex')]
    [string]$Suite = 'all',

    [ValidateSet('full', 'smoke', 'stress', 'partial')]
    [string]$Type = '',

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
    index      = 'IndexingBenchmarks       - bulk indexing throughput (vs Lucene.NET)'
    query      = 'TermQueryBenchmarks      - single-term search (vs Lucene.NET)'
    analysis   = 'AnalysisBenchmarks       - tokenisation pipeline (vs Lucene.NET)'
    boolean    = 'BooleanQueryBenchmarks   - Must / Should / MustNot (vs Lucene.NET)'
    phrase     = 'PhraseQueryBenchmarks    - exact and slop phrase (vs Lucene.NET)'
    prefix     = 'PrefixQueryBenchmarks    - prefix matching (vs Lucene.NET)'
    fuzzy      = 'FuzzyQueryBenchmarks     - fuzzy/edit-distance (vs Lucene.NET)'
    wildcard   = 'WildcardQueryBenchmarks  - wildcard patterns (vs Lucene.NET)'
    deletion   = 'DeletionBenchmarks       - delete throughput (vs Lucene.NET)'
    smallindex = 'SmallIndexBenchmarks     - 100-doc roundtrip overhead (index + search)'
}

$stratDescriptions = [ordered]@{
    'default'       = 'No overrides, uses BDN defaults. Type: full'
    'fast'          = '500 docs, --job dry (minimal smoke-test). Type: smoke'
    'quick-compare' = '1000 docs, --job short (quick comparison). Type: partial'
    'intense'       = '10000 docs, default BDN job. Type: full'
    'stress'        = '50000 docs, default BDN job. Type: stress'
}

$typeDescriptions = [ordered]@{
    'full'    = 'Standardised full run with maximum information output'
    'smoke'   = 'Quick smoke test (fast validation)'
    'stress'  = 'Stress testing with large document counts'
    'partial' = 'Benchmarking specific suites (auto-set for single-suite runs)'
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
    Write-Host '    -Suite <name>      Run a specific benchmark suite (default: all)'
    Write-Host '    -Type <name>       Run type: full, smoke, stress, partial (overrides auto-detection)'
    Write-Host '    -Strat <name>      Use a predefined strategy (default: default)'
    Write-Host '    -DocCount <n>      Override document count (overrides -Strat doc count)'
    Write-Host '    -List              List available suites, types, and strategies and exit'
    Write-Host '    -Dry               Print the command that would run without executing it'
    Write-Host '    -Help              Show this help message and exit'
    Write-Host ''
    Write-Host '  Run Types (-Type):'
    foreach ($name in $typeDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $typeDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Suites (-Suite):'
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $suiteDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Strategies (-Strat):'
    foreach ($name in $stratDescriptions.Keys) {
        Write-Host ("    {0,-16} {1}" -f $name, $stratDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Type auto-detection from -Strat (overridden by -Type):'
    Write-Host '    fast          -> smoke'
    Write-Host '    quick-compare -> partial'
    Write-Host '    intense       -> full'
    Write-Host '    stress        -> stress'
    Write-Host '    default       -> full'
    Write-Host ''
    Write-Host '  Output:'
    Write-Host '    bench/data/<type>/<runId>/<suite>/'
    Write-Host '    Run ID format: "yyyy-MM-dd HH-mm (shortcommit)"'
    Write-Host '    bench/data/index.json           Run index for all runs'
    Write-Host ''
    Write-Host '  BenchmarkDotNet pass-through examples:'
    Write-Host '    --filter *Lean*            Run only methods whose name contains Lean'
    Write-Host '    --job short                Use the Short job instead of Default'
    Write-Host '    --runtimes net10.0         Override the target runtime'
    Write-Host '    --memory true              Force memory diagnoser (already enabled)'
    Write-Host ''
    Write-Host '  Examples:'
    Write-Host '    .\scripts\benchmark.ps1                                    # all suites, type: full'
    Write-Host '    .\scripts\benchmark.ps1 -Suite query                       # query only'
    Write-Host '    .\scripts\benchmark.ps1 -Strat fast -Suite boolean         # smoke: boolean'
    Write-Host '    .\scripts\benchmark.ps1 -Type partial -Suite analysis      # partial: analysis'
    Write-Host '    .\scripts\benchmark.ps1 -Strat intense -DocCount 20000     # full: 20K docs'
    Write-Host '    .\scripts\benchmark.ps1 -List                              # list suites'
    Write-Host '    .\scripts\benchmark.ps1 -Dry -Suite index -Strat fast      # dry run'
    Write-Host ''
    exit 0
}

if ($List) {
    Write-Host ""
    Write-Host "  Available run types (-Type):"
    Write-Host ""
    foreach ($name in $typeDescriptions.Keys) {
        Write-Host ("    {0,-12} {1}" -f $name, $typeDescriptions[$name])
    }
    Write-Host ""
    Write-Host "  Available benchmark suites (-Suite):"
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
$stratType = 'full'

switch ($Strat) {
    'fast' {
        $stratDocCount = 500
        $stratJobArgs = @('--job', 'dry')
        $stratType = 'smoke'
    }
    'quick-compare' {
        $stratDocCount = 1000
        $stratJobArgs = @('--job', 'short')
        $stratType = 'partial'
    }
    'intense' {
        $stratDocCount = 10000
        $stratType = 'full'
    }
    'stress' {
        $stratDocCount = 50000
        $stratType = 'stress'
    }
}

# Resolve effective type: -Type overrides strategy-derived type
$effectiveType = if ($Type -ne '') { $Type } else { $stratType }

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
$runArgs = @('--suite', $Suite, '--type', $effectiveType)

if ($effectiveDocCount -gt 0) {
    $runArgs += @('--doccount', $effectiveDocCount.ToString())
    $env:BENCH_DOC_COUNT = $effectiveDocCount.ToString()
}

# Prepend strategy job args before user-supplied BDN args
$allExtraArgs = $stratJobArgs
if ($BenchmarkArgs) { $allExtraArgs += $BenchmarkArgs }

Write-Host "Suite:   $Suite"
Write-Host "Type:    $effectiveType"
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

