<#
.SYNOPSIS
    Downloads Project Gutenberg plain-text ebooks for benchmark testing.

.DESCRIPTION
    Downloads a curated set of public-domain books from Project Gutenberg
    into bench/data/gutenberg-ebooks/. Files are named by Gutenberg ID (e.g. 84.txt).

    Please be courteous to Gutenberg's servers. This script inserts a 1-second
    delay between requests. Do not run repeatedly in a short period of time.

.PARAMETER OutputDir
    Override the output directory. Defaults to bench/data/gutenberg-ebooks relative
    to the repository root.

.EXAMPLE
    .\scripts\download-gutenberg.ps1
    Downloads all books to the default location.

.EXAMPLE
    .\scripts\download-gutenberg.ps1 -OutputDir C:\data\books
    Downloads all books to a custom directory.
#>
param(
    [string]$OutputDir = ''
)

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrEmpty($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "bench\data\gutenberg-ebooks"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Curated list of public-domain books by Gutenberg ID.
# Expanded set covers a range of genres, authors, and writing styles
# for more representative text analysis benchmarks.
$books = [ordered]@{
    11    = "Alice's Adventures in Wonderland (Carroll)"
    84    = "Frankenstein (Shelley)"
    98    = "A Tale of Two Cities (Dickens)"
    158   = "Emma (Austen)"
    174   = "The Picture of Dorian Gray (Wilde)"
    345   = "Dracula (Stoker)"
    514   = "Little Women (Alcott)"
    1260  = "Jane Eyre (Bronte)"
    1342  = "Pride and Prejudice (Austen)"
    1400  = "Great Expectations (Dickens)"
    1661  = "The Adventures of Sherlock Holmes (Doyle)"
    1952  = "The Yellow Wallpaper (Gilman)"
    2097  = "The War of the Worlds (Wells)"
    2542  = "A Doll's House (Ibsen)"
    2591  = "Grimms' Fairy Tales"
    2701  = "Moby Dick (Melville)"
    3207  = "Leviathan (Hobbes)"
    5200  = "Metamorphosis (Kafka)"
    6130  = "The Iliad (Homer)"
    6761  = "The Adventures of Tom Sawyer (Twain)"
    7370  = "Walden (Thoreau)"
    10676 = "The Scarlet Letter (Hawthorne)"
    11231 = "The Jungle Book (Kipling)"
    16328 = "Beowulf"
    20203 = "Anna Karenina (Tolstoy)"
    23 = "Narrative of the Life of Frederick Douglass (Douglass)"
    25344 = "The Enchanted April (von Arnim)"
    27827 = "The Brothers Karamazov (Dostoevsky)"
    28054 = "The Brothers Karamazov v2 (Dostoevsky)"
    35 = "The Time Machine (Wells)"
    36 = "The War of the Worlds (Wells) - alternate"
    42671 = "The Importance of Being Earnest (Wilde)"
    43 = "The Strange Case of Dr Jekyll and Mr Hyde (Stevenson)"
    45 = "Anne of Green Gables (Montgomery)"
    46 = "A Christmas Carol (Dickens)"
    47 = "Treasure Island (Stevenson)"
    4280  = "The Life and Adventures of Robinson Crusoe (Defoe)"
    5348  = "Narrative of the Life of Frederick Douglass (variant)"
    55 = "The Wonderful Wizard of Oz (Baum)"
    61 = "The Secret Garden (Burnett)"
    74 = "Adventures of Huckleberry Finn (Twain)"
    768 = "Wuthering Heights (Bronte)"
    776 = "What Is Man? (Twain)"
    829 = "Gulliver's Travels (Swift)"
    844 = "The Importance of Being Earnest (Wilde) - variant"
    863 = "Hamlet (Shakespeare)"
    9 = "Hamlet and works (Shakespeare)"
}

$total = $books.Count
$succeeded = 0
$failed = 0

Write-Host "Downloading $total books to: $OutputDir" -ForegroundColor Green
Write-Host ""

$index = 0
foreach ($id in $books.Keys) {
    $index++
    $title = $books[$id]
    $outputPath = Join-Path $OutputDir "$id.txt"

    if (Test-Path $outputPath) {
        Write-Host "[$index/$total] Skipping $id ($title) - already exists" -ForegroundColor DarkGray
        $succeeded++
        continue
    }

    # Try both UTF-8 variant (-0.txt) and plain (.txt)
    $urls = @(
        "https://www.gutenberg.org/files/$id/$id-0.txt",
        "https://www.gutenberg.org/files/$id/$id.txt"
    )

    $downloaded = $false
    foreach ($url in $urls) {
        try {
            Write-Host "[$index/$total] Fetching $id: $title" -ForegroundColor Cyan
            Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing `
                -UserAgent "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)"
            Write-Host "  Saved $outputPath" -ForegroundColor Green
            $downloaded = $true
            $succeeded++
            break
        }
        catch {
            # Try next URL
        }
    }

    if (-not $downloaded) {
        Write-Host "  Failed to download $id ($title)" -ForegroundColor Red
        $failed++
    }

    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "Complete: $succeeded downloaded, $failed failed." -ForegroundColor Yellow
Write-Host "Data in: $OutputDir"
