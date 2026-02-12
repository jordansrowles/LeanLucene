#!/usr/bin/env bash
# Full LeanLucene benchmark run — intense strategy, all suites.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$SCRIPT_DIR/scripts/benchmark.sh" --suite all --type full --strat intense "$@"
