<#
.SYNOPSIS
    Unified LeanLucene benchmark runner.

.DESCRIPTION
    Runs BenchmarkDotNet suites for LeanLucene. Results are written to
    benches/{machine}/{date}/{time}/ with JSON, Markdown, and HTML output.
    A consolidated report.json and per-machine index.json are generated.

.PARAMETER Suite
    Which benchmark suite to run. Default: all.
    Valid values: all, index, query, analysis, boolean, phrase, prefix,
    fuzzy, wildcard, deletion, suggester, schemajson, compound, indexsort,
    blockjoin, gutenberg-analysis, gutenberg-index, gutenberg-search,
    tokenbudget, diagnostics.

.PARAMETER Strat
    Predefined strategy that configures DocCount and BDN job.
    Valid values: default, fast, quick-compare, intense, stress.
      default       - No overrides, uses BDN defaults. Type: full.
      fast          - 500 docs, --job dry (minimal smoke-test). Type: smoke.
      quick-compare - 1000 docs, --job short (quick comparison). Type: partial.
      intense       - 10000 docs, default BDN job. Type: full.
      stress        - 50000 docs, default BDN job. Type: stress.

.PARAMETER DocCount
    Override document count for all suites.

.PARAMETER LeanOnly
    Skip Lucene.NET comparison benchmarks; run LeanLucene methods only.

.PARAMETER Help
    Show usage information and exit.

.PARAMETER List
    List available benchmark suites and exit.

.PARAMETER Dry
    Print the dotnet command that would be run without executing it.

.PARAMETER GcDump
    Collect GC heap dumps after each benchmark run (requires dotnet-gcdump).

.PARAMETER BenchmarkArgs
    Additional arguments passed through to BenchmarkDotNet
    (e.g. --filter "*Lean*", --job short, --memory true).

.EXAMPLE
    .\scripts\benchmark.ps1
    Runs all standard benchmark suites with default settings.

.EXAMPLE
    .\scripts\benchmark.ps1 -Suite query
    Runs only the TermQuery benchmark suite.

.EXAMPLE
    .\scripts\benchmark.ps1 -Suite gutenberg-search -LeanOnly
    Runs Gutenberg search benchmarks, LeanLucene only.

.EXAMPLE
    .\scripts\benchmark.ps1 -Strat fast -Suite boolean
    Runs BooleanQuery benchmarks with 500 docs and --job dry.

.EXAMPLE
    .\scripts\benchmark.ps1 -Strat intense -DocCount 20000
    Runs all suites with 20K docs.

.EXAMPLE
    .\scripts\benchmark.ps1 -List
    Lists all available benchmark suites and strategies.

.EXAMPLE
    .\scripts\benchmark.ps1 -Dry -Suite index -Strat fast
    Prints the command that would run without executing it.
#>
param(
    [ValidateSet('all', 'index', 'query', 'analysis', 'boolean', 'phrase',
                 'prefix', 'fuzzy', 'wildcard', 'deletion',
                 'suggester', 'schemajson', 'compound', 'indexsort', 'blockjoin',
                 'gutenberg-analysis', 'gutenberg-index', 'gutenberg-search',
                 'tokenbudget', 'diagnostics')]
    [string]$Suite = 'all',

    [ValidateSet('default', 'fast', 'quick-compare', 'intense', 'stress')]
    [string]$Strat = 'default',

    [int]$DocCount = 0,

    [switch]$LeanOnly,

    [switch]$Help,

    [switch]$List,

    [switch]$Dry,

    [switch]$GcDump,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$suiteDescriptions = [ordered]@{
    all                  = 'Run all standard benchmark suites (default)'
    index                = 'IndexingBenchmarks        -- bulk indexing throughput (vs Lucene.NET)'
    query                = 'TermQueryBenchmarks        -- single-term search (vs Lucene.NET)'
    analysis             = 'AnalysisBenchmarks         -- tokenisation pipeline'
    boolean              = 'BooleanQueryBenchmarks     -- Must / Should / MustNot'
    phrase               = 'PhraseQueryBenchmarks      -- exact and slop phrase'
    prefix               = 'PrefixQueryBenchmarks      -- prefix matching (vs Lucene.NET)'
    fuzzy                = 'FuzzyQueryBenchmarks       -- fuzzy/edit-distance'
    wildcard             = 'WildcardQueryBenchmarks    -- wildcard patterns'
    deletion             = 'DeletionBenchmarks         -- delete throughput (vs Lucene.NET)'
    suggester            = 'SuggesterBenchmarks        -- DidYouMean spelling (vs Lucene.NET)'
    schemajson           = 'SchemaAndJsonBenchmarks    -- schema validation + JSON mapping'
    compound             = 'CompoundFileIndex/Search   -- compound file read/write (vs Lucene.NET)'
    indexsort            = 'IndexSortIndex/Search      -- index-time sort + early termination'
    blockjoin            = 'BlockJoinBenchmarks        -- block-join queries (vs Lucene.NET)'
    'gutenberg-analysis' = 'GutenbergAnalysis          -- analysis on real ebook text (explicit only)'
    'gutenberg-index'    = 'GutenbergIndex             -- indexing real ebook data (explicit only)'
    'gutenberg-search'   = 'GutenbergSearch            -- search on real ebook data (explicit only)'
    tokenbudget          = 'TokenBudgetBenchmarks      -- token budget enforcement overhead (explicit only)'
    diagnostics          = 'DiagnosticsBenchmarks      -- SlowQueryLog + Analytics overhead (explicit only)'
}

$stratDescriptions = [ordered]@{
    'default'       = 'No overrides, uses BDN defaults.'
    'fast'          = '500 docs, --job dry (minimal smoke-test).'
    'quick-compare' = '1000 docs, --job short (quick comparison).'
    'intense'       = '10000 docs, default BDN job.'
    'stress'        = '50000 docs, default BDN job.'
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
    Write-Host '    -Suite <name>      Benchmark suite to run (default: all)'
    Write-Host '    -Strat <name>      Predefined strategy (default: default)'
    Write-Host '    -DocCount <n>      Override document count (overrides -Strat)'
    Write-Host '    -LeanOnly          Skip Lucene.NET comparison benchmarks'
    Write-Host '    -List              List available suites and strategies and exit'
    Write-Host '    -Dry               Print the command that would run without executing it'
    Write-Host '    -GcDump            Collect GC heap dumps (requires dotnet-gcdump)'
    Write-Host '    -Help              Show this help message and exit'
    Write-Host ''
    Write-Host '  Suites (-Suite):'
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-22} {1}" -f $name, $suiteDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Strategies (-Strat):'
    foreach ($name in $stratDescriptions.Keys) {
        Write-Host ("    {0,-16} {1}" -f $name, $stratDescriptions[$name])
    }
    Write-Host ''
    Write-Host '  Output:'
    Write-Host '    benches/{machine-name}/{yyyy-MM-dd}/{HH-mm}/'
    Write-Host '    benches/{machine-name}/index.json   Per-machine run index'
    Write-Host ''
    Write-Host '  BenchmarkDotNet pass-through examples:'
    Write-Host '    --filter *Lean*            Run only methods whose name contains Lean'
    Write-Host '    --job short                Use the Short job instead of Default'
    Write-Host '    --runtimes net10.0         Override the target runtime'
    Write-Host ''
    Write-Host '  Examples:'
    Write-Host '    .\scripts\benchmark.ps1                                          # all suites'
    Write-Host '    .\scripts\benchmark.ps1 -Suite query                             # query only'
    Write-Host '    .\scripts\benchmark.ps1 -Suite gutenberg-search -LeanOnly        # real data, lean only'
    Write-Host '    .\scripts\benchmark.ps1 -Strat fast -Suite boolean               # smoke: boolean'
    Write-Host '    .\scripts\benchmark.ps1 -Strat intense -DocCount 20000           # full: 20K docs'
    Write-Host '    .\scripts\benchmark.ps1 -List                                    # list suites'
    Write-Host '    .\scripts\benchmark.ps1 -Dry -Suite index -Strat fast            # dry run'
    Write-Host ''
    exit 0
}

if ($List) {
    Write-Host ""
    Write-Host "  Available benchmark suites (-Suite):"
    Write-Host ""
    foreach ($name in $suiteDescriptions.Keys) {
        Write-Host ("    {0,-22} {1}" -f $name, $suiteDescriptions[$name])
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
    (Join-Path $PSScriptRoot "..\src\Rowles.LeanLucene.Benchmarks\Rowles.LeanLucene.Benchmarks.csproj")
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

if ($LeanOnly) {
    $runArgs += '--lean-only'
}

# Prepend strategy job args before user-supplied BDN args
$allExtraArgs = $stratJobArgs
if ($BenchmarkArgs) { $allExtraArgs += $BenchmarkArgs }

Write-Host "Suite:    $Suite"
Write-Host "Strat:    $Strat"
if ($LeanOnly) { Write-Host "LeanOnly: enabled" }
if ($effectiveDocCount -gt 0) {
    Write-Host "Docs:     $effectiveDocCount"
}
if ($allExtraArgs) {
    Write-Host "Extra:    $($allExtraArgs -join ' ')"
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

if ($GcDump) {
    $runArgs += '--gcdump'
    if (-not (Get-Command dotnet-gcdump -ErrorAction SilentlyContinue)) {
        Write-Host 'Installing dotnet-gcdump global tool...'
        dotnet tool install -g dotnet-gcdump
    }
    Write-Host "GcDump:   enabled"
}

Write-Host ""
dotnet run -c Release --project $projectPath -- @runArgs @allExtraArgs
