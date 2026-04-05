# download-gutenberg.ps1
# downloads gutenberg ebooks for benchmark testing into .\bench\data\gutenberg-ebooks
$outputDir = Join-Path $PSScriptRoot "bench\data\gutenberg-ebooks"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# A curated list of 10 public domain books (IDs)
$bookIds = @(84, 1342, 11, 1260, 98, 1661, 2701, 345, 174, 158)

Write-Host "Downloading $($bookIds.Count) books to $outputDir" -ForegroundColor Green

foreach ($id in $bookIds) {
    $url = "https://www.gutenberg.org/files/$id/$id-0.txt"
    $outputPath = Join-Path $outputDir "$id.txt"

    Write-Host "Fetching ID $id ..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing -UserAgent "Mozilla/5.0 (compatible; BenchmarkBot/1.0)"
        Write-Host "  Saved to $outputPath" -ForegroundColor Green
    }
    catch {
        Write-Host "  Failed: $_" -ForegroundColor Red
    }
    Start-Sleep -Seconds 1
}

Write-Host "`nAll done! Combine files with:" -ForegroundColor Yellow
Write-Host "  Get-Content '$outputDir\*.txt' | Out-File '$outputDir\corpus.txt'" -ForegroundColor Yellow