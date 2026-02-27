#!/usr/bin/env bash
# Unified LeanLucene benchmark runner.
#
# USAGE:
#   ./scripts/benchmark.sh [options] [BenchmarkDotNet args...]
#
# OPTIONS:
#   --suite <name>      Benchmark suite (default: all)
#   --type <name>       Run type: full, smoke, stress, partial
#   --strat <name>      Predefined strategy: default, fast, quick-compare, intense, stress
#   --doccount <n>      Override document count
#   --list              List available suites, types, and strategies
#   --dry               Print the command without executing it
#   --help              Show help and exit
#
# Extra arguments after -- are passed through to BenchmarkDotNet.
#
# EXAMPLES:
#   ./scripts/benchmark.sh
#   ./scripts/benchmark.sh --suite query
#   ./scripts/benchmark.sh --strat fast --suite boolean
#   ./scripts/benchmark.sh --type partial --suite analysis
#   ./scripts/benchmark.sh --strat intense --doccount 20000
#   ./scripts/benchmark.sh --list
#   ./scripts/benchmark.sh --dry --suite index --strat fast

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$REPO_ROOT/src/Rowles.LeanLucene.Example.Benchmarks/Rowles.LeanLucene.Example.Benchmarks.csproj"

# ── Defaults ──────────────────────────────────────────────────────────────────
SUITE="all"
TYPE=""
STRAT="default"
DOC_COUNT=0
DRY=false
LIST=false
HELP=false
GCDUMP=false
EXTRA_ARGS=()

# ── Suite / strategy / type descriptions ──────────────────────────────────────
declare -A SUITE_DESC=(
    [all]="Run all benchmark suites (default)"
    [index]="IndexingBenchmarks       - bulk indexing throughput (vs Lucene.NET)"
    [query]="TermQueryBenchmarks      - single-term search (vs Lucene.NET)"
    [analysis]="AnalysisBenchmarks       - tokenisation pipeline (vs Lucene.NET)"
    [boolean]="BooleanQueryBenchmarks   - Must / Should / MustNot (vs Lucene.NET)"
    [phrase]="PhraseQueryBenchmarks    - exact and slop phrase (vs Lucene.NET)"
    [prefix]="PrefixQueryBenchmarks    - prefix matching (vs Lucene.NET)"
    [fuzzy]="FuzzyQueryBenchmarks     - fuzzy/edit-distance (vs Lucene.NET)"
    [wildcard]="WildcardQueryBenchmarks  - wildcard patterns (vs Lucene.NET)"
    [deletion]="DeletionBenchmarks       - delete throughput (vs Lucene.NET)"
    [smallindex]="SmallIndexBenchmarks     - 100-doc roundtrip overhead (index + search)"
    [tokenbudget]="TokenBudgetBenchmarks   - token budget enforcement overhead"
    [diagnostics]="DiagnosticsBenchmarks   - SlowQueryLog + Analytics hook overhead"
    [suggester]="SuggesterBenchmarks      - DidYouMean spelling (vs Lucene.NET SpellChecker)"
    [schemajson]="SchemaAndJsonBenchmarks  - schema validation + JSON mapping"
    [compound]="CompoundFileIndex/SearchBenchmarks - compound file read/write (vs Lucene.NET)"
    [indexsort]="IndexSortIndex/SearchBenchmarks   - index-time sort + early termination"
    [blockjoin]="BlockJoinBenchmarks      - block-join queries (vs Lucene.NET Join)"
)
SUITE_ORDER=(all index query analysis boolean phrase prefix fuzzy wildcard deletion smallindex tokenbudget diagnostics suggester schemajson compound indexsort blockjoin)

declare -A STRAT_DESC=(
    [default]="No overrides, uses BDN defaults. Type: full"
    [fast]="500 docs, --job dry (minimal smoke-test). Type: smoke"
    [quick-compare]="1000 docs, --job short (quick comparison). Type: partial"
    [intense]="10000 docs, default BDN job. Type: full"
    [stress]="50000 docs, default BDN job. Type: stress"
)
STRAT_ORDER=(default fast quick-compare intense stress)

declare -A TYPE_DESC=(
    [full]="Standardised full run with maximum information output"
    [smoke]="Quick smoke test (fast validation)"
    [stress]="Stress testing with large document counts"
    [partial]="Benchmarking specific suites (auto-set for single-suite runs)"
)
TYPE_ORDER=(full smoke stress partial)

VALID_SUITES=(all index query analysis boolean phrase prefix fuzzy wildcard deletion smallindex tokenbudget diagnostics suggester schemajson compound indexsort blockjoin)
VALID_TYPES=(full smoke stress partial)
VALID_STRATS=(default fast quick-compare intense stress)

# ── Helper: check membership ───────────────────────────────────────────────────
contains() {
    local val="$1"; shift
    for item in "$@"; do [[ "$item" == "$val" ]] && return 0; done
    return 1
}

# ── Parse arguments ────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --suite)   SUITE="$2";     shift 2 ;;
        --type)    TYPE="$2";      shift 2 ;;
        --strat)   STRAT="$2";     shift 2 ;;
        --doccount) DOC_COUNT="$2"; shift 2 ;;
        --dry)     DRY=true;       shift ;;
        --list)    LIST=true;      shift ;;
        --gcdump)  GCDUMP=true;    shift ;;
        --help|-h) HELP=true;      shift ;;
        --)        shift; EXTRA_ARGS+=("$@"); break ;;
        *)         EXTRA_ARGS+=("$1"); shift ;;
    esac
done

# ── Validate ───────────────────────────────────────────────────────────────────
if ! contains "$SUITE" "${VALID_SUITES[@]}"; then
    echo "Error: invalid suite '$SUITE'. Valid: ${VALID_SUITES[*]}" >&2; exit 1
fi
if [[ -n "$TYPE" ]] && ! contains "$TYPE" "${VALID_TYPES[@]}"; then
    echo "Error: invalid type '$TYPE'. Valid: ${VALID_TYPES[*]}" >&2; exit 1
fi
if ! contains "$STRAT" "${VALID_STRATS[@]}"; then
    echo "Error: invalid strat '$STRAT'. Valid: ${VALID_STRATS[*]}" >&2; exit 1
fi

# ── Help ───────────────────────────────────────────────────────────────────────
if $HELP; then
    echo ""
    echo "  LeanLucene Benchmark Runner"
    echo "  ============================"
    echo ""
    echo "  Usage:"
    echo "    ./scripts/benchmark.sh [options] [-- BenchmarkDotNet args...]"
    echo ""
    echo "  Options:"
    echo "    --suite <name>      Run a specific benchmark suite (default: all)"
    echo "    --type <name>       Run type: full, smoke, stress, partial (overrides auto-detection)"
    echo "    --strat <name>      Use a predefined strategy (default: default)"
    echo "    --doccount <n>      Override document count (overrides --strat doc count)"
    echo "    --list              List available suites, types, and strategies and exit"
    echo "    --dry               Print the command that would run without executing it"
    echo "    --help              Show this help message and exit"
    echo ""
    echo "  Run Types (--type):"
    for name in "${TYPE_ORDER[@]}"; do
        printf "    %-12s %s\n" "$name" "${TYPE_DESC[$name]}"
    done
    echo ""
    echo "  Suites (--suite):"
    for name in "${SUITE_ORDER[@]}"; do
        printf "    %-12s %s\n" "$name" "${SUITE_DESC[$name]}"
    done
    echo ""
    echo "  Strategies (--strat):"
    for name in "${STRAT_ORDER[@]}"; do
        printf "    %-16s %s\n" "$name" "${STRAT_DESC[$name]}"
    done
    echo ""
    echo "  Type auto-detection from --strat (overridden by --type):"
    echo "    fast          -> smoke"
    echo "    quick-compare -> partial"
    echo "    intense       -> full"
    echo "    stress        -> stress"
    echo "    default       -> full"
    echo ""
    echo "  Output:"
    echo "    bench/data/<type>/<runId>/<suite>/"
    echo "    Run ID format: \"yyyy-MM-dd HH-mm (shortcommit)\""
    echo "    bench/data/index.json           Run index for all runs"
    echo ""
    echo "  BenchmarkDotNet pass-through examples (after --):"
    echo "    -- --filter '*Lean*'            Run only methods whose name contains Lean"
    echo "    -- --job short                  Use the Short job instead of Default"
    echo "    -- --runtimes net10.0           Override the target runtime"
    echo "    -- --memory true                Force memory diagnoser (already enabled)"
    echo ""
    echo "  Examples:"
    echo "    ./scripts/benchmark.sh                                    # all suites, type: full"
    echo "    ./scripts/benchmark.sh --suite query                      # query only"
    echo "    ./scripts/benchmark.sh --strat fast --suite boolean       # smoke: boolean"
    echo "    ./scripts/benchmark.sh --type partial --suite analysis    # partial: analysis"
    echo "    ./scripts/benchmark.sh --strat intense --doccount 20000   # full: 20K docs"
    echo "    ./scripts/benchmark.sh --list                             # list suites"
    echo "    ./scripts/benchmark.sh --dry --suite index --strat fast   # dry run"
    echo ""
    exit 0
fi

# ── List ───────────────────────────────────────────────────────────────────────
if $LIST; then
    echo ""
    echo "  Available run types (--type):"
    echo ""
    for name in "${TYPE_ORDER[@]}"; do
        printf "    %-12s %s\n" "$name" "${TYPE_DESC[$name]}"
    done
    echo ""
    echo "  Available benchmark suites (--suite):"
    echo ""
    for name in "${SUITE_ORDER[@]}"; do
        printf "    %-12s %s\n" "$name" "${SUITE_DESC[$name]}"
    done
    echo ""
    echo "  Available strategies (--strat):"
    echo ""
    for name in "${STRAT_ORDER[@]}"; do
        printf "    %-16s %s\n" "$name" "${STRAT_DESC[$name]}"
    done
    echo ""
    exit 0
fi

# ── Resolve strategy presets ───────────────────────────────────────────────────
STRAT_DOC_COUNT=0
STRAT_JOB_ARGS=()
STRAT_TYPE="full"

case "$STRAT" in
    fast)
        STRAT_DOC_COUNT=500
        STRAT_JOB_ARGS=(--job dry)
        STRAT_TYPE="smoke"
        ;;
    quick-compare)
        STRAT_DOC_COUNT=1000
        STRAT_JOB_ARGS=(--job short)
        STRAT_TYPE="partial"
        ;;
    intense)
        STRAT_DOC_COUNT=10000
        STRAT_TYPE="full"
        ;;
    stress)
        STRAT_DOC_COUNT=50000
        STRAT_TYPE="stress"
        ;;
esac

# Resolve effective type: --type overrides strategy-derived type
EFFECTIVE_TYPE="${TYPE:-$STRAT_TYPE}"

# Resolve effective doc count: --doccount overrides strategy
EFFECTIVE_DOC_COUNT=0
if [[ "$DOC_COUNT" -gt 0 ]]; then
    EFFECTIVE_DOC_COUNT="$DOC_COUNT"
elif [[ "$STRAT_DOC_COUNT" -gt 0 ]]; then
    EFFECTIVE_DOC_COUNT="$STRAT_DOC_COUNT"
fi

# ── Validate project exists ────────────────────────────────────────────────────
if [[ ! -f "$PROJECT_PATH" ]]; then
    echo "Error: benchmark project not found at: $PROJECT_PATH" >&2
    exit 1
fi

# ── Build run args ─────────────────────────────────────────────────────────────
RUN_ARGS=(--suite "$SUITE" --type "$EFFECTIVE_TYPE")

if [[ "$EFFECTIVE_DOC_COUNT" -gt 0 ]]; then
    RUN_ARGS+=(--doccount "$EFFECTIVE_DOC_COUNT")
    export BENCH_DOC_COUNT="$EFFECTIVE_DOC_COUNT"
fi

ALL_EXTRA_ARGS=("${STRAT_JOB_ARGS[@]}" "${EXTRA_ARGS[@]}")

# ── Print summary ──────────────────────────────────────────────────────────────
echo "Suite:   $SUITE"
echo "Type:    $EFFECTIVE_TYPE"
echo "Strat:   $STRAT"
if [[ "$EFFECTIVE_DOC_COUNT" -gt 0 ]]; then
    echo "Docs:    $EFFECTIVE_DOC_COUNT"
fi
if [[ "${#ALL_EXTRA_ARGS[@]}" -gt 0 ]]; then
    echo "Extra:   ${ALL_EXTRA_ARGS[*]}"
fi

# ── Dry run ────────────────────────────────────────────────────────────────────
if $DRY; then
    echo ""
    echo "Dry run - command that would execute:"
    printf "  dotnet run -c Release --project \"%s\" -- %s" \
        "$PROJECT_PATH" "${RUN_ARGS[*]}"
    if [[ "${#ALL_EXTRA_ARGS[@]}" -gt 0 ]]; then
        printf " %s" "${ALL_EXTRA_ARGS[*]}"
    fi
    echo ""
    if [[ "$EFFECTIVE_DOC_COUNT" -gt 0 ]]; then
        echo "  env: BENCH_DOC_COUNT=$EFFECTIVE_DOC_COUNT"
    fi
    echo ""
    exit 0
fi

# ── GC dump tool check ─────────────────────────────────────────────────────────
if $GCDUMP; then
    RUN_ARGS+=(--gcdump)
    if ! command -v dotnet-gcdump &>/dev/null; then
        echo "Installing dotnet-gcdump global tool..."
        dotnet tool install -g dotnet-gcdump
    fi
    echo "GcDump: enabled"
fi

# ── Execute ────────────────────────────────────────────────────────────────────
echo ""
dotnet run -c Release --project "$PROJECT_PATH" -- "${RUN_ARGS[@]}" "${ALL_EXTRA_ARGS[@]}"
