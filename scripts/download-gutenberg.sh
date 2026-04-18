#!/usr/bin/env bash
# Downloads Project Gutenberg plain-text ebooks for benchmark testing.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

BOOK_COUNT=200
OUTPUT_DIR=""

print_help() {
    cat <<'EOF'
Usage:
  ./scripts/download-gutenberg.sh [options]

Options:
  --book-count <n>   Number of books to download (default: 200)
  --output-dir <dir> Override the output directory
  --help             Show help and exit
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

while [[ $# -gt 0 ]]; do
    case "$1" in
        --book-count)
            BOOK_COUNT="${2:-}"
            shift 2
            ;;
        --output-dir)
            OUTPUT_DIR="${2:-}"
            shift 2
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

if ! [[ "$BOOK_COUNT" =~ ^[0-9]+$ ]] || (( BOOK_COUNT < 1 )); then
    echo "Error: --book-count must be a positive integer." >&2
    exit 1
fi

if [[ -z "$OUTPUT_DIR" ]]; then
    OUTPUT_DIR="$REPO_ROOT/bench/data/gutenberg-ebooks"
fi

CATALOGUE_CACHE_DIR="$REPO_ROOT/bench/data"
CATALOGUE_CACHE_PATH="$CATALOGUE_CACHE_DIR/.gutenberg-catalog.csv"

mkdir -p "$OUTPUT_DIR" "$CATALOGUE_CACHE_DIR"

require_command curl "Install curl to download benchmark data."

SEED_BOOKS=$(cat <<'EOF'
9|Hamlet and works (Shakespeare)
11|Alice's Adventures in Wonderland (Carroll)
12|Through the Looking-Glass (Carroll)
16|Peter Pan (Barrie)
23|Narrative of the Life of Frederick Douglass (Douglass)
26|The Jungle (Sinclair)
35|The Time Machine (Wells)
43|The Strange Case of Dr Jekyll and Mr Hyde (Stevenson)
45|Anne of Green Gables (Montgomery)
46|A Christmas Carol (Dickens)
47|Treasure Island (Stevenson)
55|The Wonderful Wizard of Oz (Baum)
61|The Secret Garden (Burnett)
74|Adventures of Huckleberry Finn (Twain)
84|Frankenstein (Shelley)
98|A Tale of Two Cities (Dickens)
113|The Secret Sharer (Conrad)
158|Emma (Austen)
174|The Picture of Dorian Gray (Wilde)
209|The Turn of the Screw (James)
215|The Call of the Wild (London)
244|A Study in Scarlet (Doyle)
345|Dracula (Stoker)
514|Little Women (Alcott)
768|Wuthering Heights (Bronte)
776|What Is Man? (Twain)
829|Gulliver's Travels (Swift)
844|The Importance of Being Earnest (Wilde) - variant
863|Hamlet (Shakespeare)
1080|A Modest Proposal (Swift)
1184|The Count of Monte Cristo (Dumas)
1232|The Prince (Machiavelli)
1260|Jane Eyre (Bronte)
1342|Pride and Prejudice (Austen)
1399|Vanity Fair (Thackeray)
1400|Great Expectations (Dickens)
1497|The Republic (Plato)
1512|Romeo and Juliet (Shakespeare)
1513|Macbeth (Shakespeare)
1661|The Adventures of Sherlock Holmes (Doyle)
1952|The Yellow Wallpaper (Gilman)
2097|The War of the Worlds (Wells)
2148|Uncle Tom's Cabin (Stowe)
2542|A Doll's House (Ibsen)
2554|Crime and Punishment (Dostoevsky)
2591|Grimms' Fairy Tales
2701|Moby Dick (Melville)
2814|Dubliners (Joyce)
3207|Leviathan (Hobbes)
3268|The Mysterious Affair at Styles (Christie)
3825|The Cherry Orchard (Chekhov)
4280|The Life and Adventures of Robinson Crusoe (Defoe)
4300|Ulysses (Joyce)
4517|My Antonia (Cather)
5200|Metamorphosis (Kafka)
5348|Narrative of the Life of Frederick Douglass (variant)
5740|Siddhartha (Hesse)
5765|Flatland (Abbott)
6130|The Iliad (Homer)
6593|The Wind in the Willows (Grahame)
6761|The Adventures of Tom Sawyer (Twain)
7370|Walden (Thoreau)
7849|The Return of Sherlock Holmes (Doyle)
8800|Don Quixote (Cervantes)
10148|Sons and Lovers (Lawrence)
10676|The Scarlet Letter (Hawthorne)
11231|The Jungle Book (Kipling)
15399|The Awakening (Chopin)
16328|Beowulf
16389|My Man Jeeves (Wodehouse)
18328|The Island of Doctor Moreau (Wells)
19337|The Red Badge of Courage (Crane)
19942|The Scarlet Pimpernel (Orczy)
20203|Anna Karenina (Tolstoy)
22381|The Thirty-Nine Steps (Buchan)
25344|The Enchanted April (von Arnim)
27827|The Brothers Karamazov (Dostoevsky)
28054|The Brothers Karamazov v2 (Dostoevsky)
30254|The Portrait of a Lady (James)
36|The War of the Worlds (Wells) - alternate
42671|The Importance of Being Earnest (Wilde)
EOF
)

mapfile -t SEED_LINES < <(printf '%s\n' "$SEED_BOOKS")

declare -a BOOK_IDS=()
declare -A BOOK_TITLES=()

SEED_COUNT=${#SEED_LINES[@]}
INITIAL_COUNT=$BOOK_COUNT
if (( INITIAL_COUNT > SEED_COUNT )); then
    INITIAL_COUNT=$SEED_COUNT
fi

for ((i = 0; i < INITIAL_COUNT; i++)); do
    line="${SEED_LINES[$i]}"
    id="${line%%|*}"
    title="${line#*|}"
    BOOK_IDS+=("$id")
    BOOK_TITLES["$id"]="$title"
done

if (( BOOK_COUNT > SEED_COUNT )); then
    require_command python3 "Book counts above $SEED_COUNT require python3 for Gutenberg catalogue parsing."

    if [[ ! -f "$CATALOGUE_CACHE_PATH" ]]; then
        GZ_PATH="${CATALOGUE_CACHE_PATH}.gz"
        echo "Downloading Gutenberg catalogue..."
        curl -fL --retry 3 --retry-delay 2 \
            -A "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)" \
            -o "$GZ_PATH" \
            "https://www.gutenberg.org/cache/epub/feeds/pg_catalog.csv.gz"

        python3 - "$GZ_PATH" "$CATALOGUE_CACHE_PATH" <<'PY'
import gzip
import shutil
import sys

source_path = sys.argv[1]
target_path = sys.argv[2]

with gzip.open(source_path, "rb") as source, open(target_path, "wb") as target:
    shutil.copyfileobj(source, target)
PY

        rm -f "$GZ_PATH"
    fi

    needed=$((BOOK_COUNT - SEED_COUNT))
    mapfile -t EXTRA_IDS < <(python3 - "$CATALOGUE_CACHE_PATH" "$needed" "${BOOK_IDS[@]}" <<'PY'
import csv
import sys

catalogue_path = sys.argv[1]
needed = int(sys.argv[2])
excluded = set(sys.argv[3:])
results = []

with open(catalogue_path, newline="", encoding="utf-8", errors="replace") as handle:
    reader = csv.DictReader(handle)
    for row in reader:
        text_id = (row.get("Text#") or "").strip()
        language = (row.get("Language") or "").strip()
        if language != "en" or not text_id.isdigit() or text_id in excluded:
            continue

        results.append(text_id)
        excluded.add(text_id)
        if len(results) >= needed:
            break

for text_id in results:
    print(text_id)
PY
)

    for id in "${EXTRA_IDS[@]}"; do
        BOOK_IDS+=("$id")
        BOOK_TITLES["$id"]="Gutenberg Book #$id"
    done
fi

TOTAL=${#BOOK_IDS[@]}
SUCCEEDED=0
FAILED=0

echo "Downloading $TOTAL books to: $OUTPUT_DIR"
echo ""

INDEX=0
for id in "${BOOK_IDS[@]}"; do
    INDEX=$((INDEX + 1))
    title="${BOOK_TITLES[$id]}"
    output_path="$OUTPUT_DIR/$id.txt"

    if [[ -f "$output_path" ]]; then
        echo "[$INDEX/$TOTAL] Skipping $id ($title) - already exists"
        SUCCEEDED=$((SUCCEEDED + 1))
        continue
    fi

    downloaded=false
    urls=(
        "https://www.gutenberg.org/files/$id/$id-0.txt"
        "https://www.gutenberg.org/files/$id/$id.txt"
        "https://www.gutenberg.org/cache/epub/$id/pg$id.txt"
    )

    for url in "${urls[@]}"; do
        echo "[$INDEX/$TOTAL] Fetching $id: $title"
        if curl -fsSL --retry 3 --retry-delay 2 \
            -A "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)" \
            -o "$output_path" \
            "$url"; then
            echo "  Saved $output_path"
            downloaded=true
            SUCCEEDED=$((SUCCEEDED + 1))
            break
        fi

        rm -f "$output_path"
    done

    if [[ "$downloaded" != true ]]; then
        echo "  Failed to download $id ($title)" >&2
        FAILED=$((FAILED + 1))
    fi

    sleep 1
done

echo ""
echo "Complete: $SUCCEEDED downloaded, $FAILED failed."
echo "Data in: $OUTPUT_DIR"
