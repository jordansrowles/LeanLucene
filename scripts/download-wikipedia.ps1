<#
.SYNOPSIS
    Downloads the Simple English Wikipedia abstracts dump for benchmark testing.

.DESCRIPTION
    Downloads the Simple English Wikipedia 'abstract' dump (~30-50 MB compressed)
    from dumps.wikimedia.org, extracts it, and converts it to plain-text .txt files
    in bench/data/wikipedia/ for use in text analysis and indexing benchmarks.

    The dump contains ~200,000 article abstracts in XML format. This script extracts
    each article's title and abstract body into individual or batched plain-text files.

    Source: https://dumps.wikimedia.org/simplewiki/latest/
    Licence: Creative Commons Attribution-ShareAlike (CC BY-SA)

.PARAMETER OutputDir
    Override the output directory. Defaults to bench/data/wikipedia relative
    to the repository root.

.PARAMETER MaxArticles
    Maximum number of articles to extract. Default: 0 (all articles).

.PARAMETER BatchSize
    Number of articles per output file. Default: 5000.

.EXAMPLE
    .\scripts\download-wikipedia.ps1
    Downloads and extracts all Simple Wikipedia abstracts.

.EXAMPLE
    .\scripts\download-wikipedia.ps1 -MaxArticles 50000 -BatchSize 10000
    Downloads and extracts the first 50,000 articles in batches of 10,000.
#>
param(
    [string]$OutputDir = '',
    [int]$MaxArticles = 0,
    [int]$BatchSize = 5000
)

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrEmpty($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "bench\data\wikipedia"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$dumpUrl  = "https://dumps.wikimedia.org/simplewiki/latest/simplewiki-latest-abstract.xml.gz"
$gzPath   = Join-Path $OutputDir "simplewiki-latest-abstract.xml.gz"
$xmlPath  = Join-Path $OutputDir "simplewiki-latest-abstract.xml"

# Download if not already present
if (-not (Test-Path $gzPath)) {
    Write-Host "Downloading Simple Wikipedia abstracts dump..." -ForegroundColor Cyan
    Write-Host "  Source: $dumpUrl"
    Write-Host "  Destination: $gzPath"
    Write-Host ""
    Invoke-WebRequest -Uri $dumpUrl -OutFile $gzPath -UseBasicParsing `
        -UserAgent "Mozilla/5.0 (compatible; BenchmarkDataBot/1.0)"
    Write-Host "Download complete." -ForegroundColor Green
} else {
    Write-Host "Archive already present at: $gzPath" -ForegroundColor DarkGray
}

# Decompress if not already done
if (-not (Test-Path $xmlPath)) {
    Write-Host "Decompressing archive..." -ForegroundColor Cyan
    $inStream  = [System.IO.File]::OpenRead($gzPath)
    $outStream = [System.IO.File]::Create($xmlPath)
    $gzip      = [System.IO.Compression.GZipStream]::new($inStream, [System.IO.Compression.CompressionMode]::Decompress)
    $gzip.CopyTo($outStream)
    $gzip.Dispose()
    $outStream.Dispose()
    $inStream.Dispose()
    Write-Host "Decompressed to: $xmlPath" -ForegroundColor Green
} else {
    Write-Host "XML already decompressed at: $xmlPath" -ForegroundColor DarkGray
}

# Parse XML and write plain-text batches
Write-Host "Parsing XML and writing plain-text files..." -ForegroundColor Cyan

Add-Type -AssemblyName System.Xml.Linq

$settings = [System.Xml.XmlReaderSettings]::new()
$settings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore
$reader    = [System.Xml.XmlReader]::Create($xmlPath, $settings)

$articleCount = 0
$batchIndex   = 0
$batchLines   = [System.Collections.Generic.List[string]]::new($BatchSize * 3)

function FlushBatch {
    param([System.Collections.Generic.List[string]]$Lines, [int]$Index, [string]$Dir)
    $batchFile = Join-Path $Dir ("batch-{0:D4}.txt" -f $Index)
    [System.IO.File]::WriteAllLines($batchFile, $Lines)
    Write-Host ("  Written batch {0:D4}: {1} lines -> {2}" -f $Index, $Lines.Count, $batchFile) -ForegroundColor Green
    $Lines.Clear()
}

$inDoc     = $false
$title     = ''
$abstract  = ''

while ($reader.Read()) {
    if ($reader.NodeType -eq [System.Xml.XmlNodeType]::Element) {
        switch ($reader.LocalName) {
            'doc'      { $inDoc = $true; $title = ''; $abstract = '' }
            'title'    { if ($inDoc) { $title    = $reader.ReadElementContentAsString() } }
            'abstract' { if ($inDoc) { $abstract = $reader.ReadElementContentAsString() } }
        }
    }
    elseif ($reader.NodeType -eq [System.Xml.XmlNodeType]::EndElement -and $reader.LocalName -eq 'doc') {
        $inDoc = $false
        $cleanTitle    = $title    -replace '^Wikipedia: ', ''
        $cleanAbstract = $abstract.Trim()

        if ($cleanAbstract.Length -gt 20) {
            $batchLines.Add($cleanTitle)
            $batchLines.Add($cleanAbstract)
            $batchLines.Add('')
            $articleCount++

            if ($batchLines.Count -ge ($BatchSize * 3)) {
                FlushBatch -Lines $batchLines -Index $batchIndex -Dir $OutputDir
                $batchIndex++
            }

            if ($MaxArticles -gt 0 -and $articleCount -ge $MaxArticles) {
                break
            }
        }
    }
}

$reader.Dispose()

if ($batchLines.Count -gt 0) {
    FlushBatch -Lines $batchLines -Index $batchIndex -Dir $OutputDir
}

Write-Host ""
Write-Host "Complete: $articleCount articles extracted into $($batchIndex + 1) file(s)." -ForegroundColor Yellow
Write-Host "Data in: $OutputDir"
