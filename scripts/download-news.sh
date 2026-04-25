#!/usr/bin/env bash
# Downloads news article datasets for benchmark testing.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

OUTPUT_DIR=""
SKIP_20NEWS=false
SKIP_REUTERS=false

print_help() {
    cat <<'EOF'
Usage:
  ./scripts/download-news.sh [options]

Options:
  --output-dir <dir>  Override the base output directory
  --skip-20news       Skip the 20 Newsgroups dataset
  --skip-reuters      Skip the Reuters-21578 dataset
  --help              Show help and exit
EOF
}

require_command() {
    local command_name="$1"
    local reason="$2"
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "Error: '$command_name' is required. $reason" >&2
        exit 1
    fi
}

download_and_extract() {
    local url="$1"
    local fallback_url="$2"
    local archive_path="$3"
    local extract_dir="$4"
    local dataset_name="$5"

    if [[ ! -f "$archive_path" ]]; then
        echo "Downloading $dataset_name..."
        echo "  Source: $url"
        if ! curl -fL --retry 3 --retry-delay 2 \
                -A "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)" \
                -o "$archive_path" \
                "$url" 2>/dev/null; then
            echo "  Primary source failed, trying fallback: $fallback_url"
            curl -fL --retry 3 --retry-delay 2 \
                -A "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)" \
                -o "$archive_path" \
                "$fallback_url"
        fi
        echo "  Downloaded: $archive_path"
    else
        echo "$dataset_name archive already present."
    fi

    if [[ ! -d "$extract_dir" ]] || [[ -z "$(find "$extract_dir" -type f -print -quit 2>/dev/null)" ]]; then
        echo "Extracting $dataset_name to: $extract_dir"
        mkdir -p "$extract_dir"
        tar -xzf "$archive_path" -C "$extract_dir"
        echo "  Extracted."
    else
        echo "$dataset_name already extracted."
    fi
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --output-dir)
            OUTPUT_DIR="${2:-}"
            shift 2
            ;;
        --skip-20news)
            SKIP_20NEWS=true
            shift
            ;;
        --skip-reuters)
            SKIP_REUTERS=true
            shift
            ;;
        --help|-h)
            print_help
            exit 0
            ;;
        *)
            echo "Error: unknown option '$1'" >&2
            print_help >&2
            exit 1
            ;;
    esac
done

if [[ -z "$OUTPUT_DIR" ]]; then
    OUTPUT_DIR="$REPO_ROOT/bench/data"
fi

mkdir -p "$OUTPUT_DIR"

require_command curl "Install curl to download benchmark data."
require_command tar "Install tar to extract benchmark archives."

if [[ "$SKIP_20NEWS" != true ]]; then
    echo ""
    echo "=== 20 Newsgroups ==="

    news_dir="$OUTPUT_DIR/20newsgroups"
    news_archive="$OUTPUT_DIR/20news-bydate.tar.gz"

    download_and_extract \
        "http://qwone.com/~jason/20Newsgroups/20news-bydate.tar.gz" \
        "https://ndownloader.figshare.com/files/5975967" \
        "$news_archive" \
        "$news_dir" \
        "20 Newsgroups"

    doc_count=$(find "$news_dir" -type f | wc -l)
    echo "  Documents: ~$doc_count"
    echo "  Path: $news_dir"
fi

if [[ "$SKIP_REUTERS" != true ]]; then
    echo ""
    echo "=== Reuters-21578 ==="

    reuters_dir="$OUTPUT_DIR/reuters21578"
    reuters_archive="$OUTPUT_DIR/reuters21578.tar.gz"

    download_and_extract \
        "http://www.daviddlewis.com/resources/testcollections/reuters21578/reuters21578.tar.gz" \
        "https://archive.ics.uci.edu/ml/machine-learning-databases/reuters21578-mld/reuters21578.tar.gz" \
        "$reuters_archive" \
        "$reuters_dir" \
        "Reuters-21578"

    sgm_count=$(find "$reuters_dir" -maxdepth 1 -type f -name "*.sgm" | wc -l)
    echo "  SGM files: $sgm_count (each contains multiple articles)"
    echo "  Path: $reuters_dir"
    echo ""
    echo "  Note: Reuters-21578 uses SGML format. Extract <BODY> content from"
    echo "  .sgm files to use with the benchmarks."
fi

echo ""
echo "Complete. Data in: $OUTPUT_DIR"
