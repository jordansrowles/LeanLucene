<#
.SYNOPSIS
  Generates an HTML comparison report between two benchmark runs.
.DESCRIPTION
  Reads two benchmark JSON files (schema v2), matches benchmarks by key,
  and produces a self-contained dark-themed HTML report.
.PARAMETER RunA
  Path to the baseline benchmark JSON file. If omitted, uses the second-most-recent run from index.json.
.PARAMETER RunB
  Path to the after benchmark JSON file. If omitted, uses the most-recent run from index.json.
.EXAMPLE
  .\scripts\compare-benchmarks.ps1
  .\scripts\compare-benchmarks.ps1 -RunA "bench\data\full\2026-02-20 19-05 (aed60e6).json" -RunB "bench\data\full\2026-02-23 15-22 (efc655f).json"
#>
param(
    [string]$RunA,
    [string]$RunB
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

# Resolve runs from index.json if not provided
if (-not $RunA -or -not $RunB) {
    $indexPath = Join-Path $repoRoot 'bench\data\index.json'
    if (-not (Test-Path $indexPath)) {
        Write-Error "index.json not found at $indexPath. Provide -RunA and -RunB explicitly."
        return
    }
    $index = Get-Content $indexPath -Raw | ConvertFrom-Json
    $sorted = $index.runs | Sort-Object { $_.generatedAtUtc } -Descending
    if ($sorted.Count -lt 2) {
        Write-Error "Need at least 2 runs in index.json to auto-select. Found $($sorted.Count)."
        return
    }
    if (-not $RunB) { $RunB = Join-Path $repoRoot "bench\data\$($sorted[0].file)" }
    if (-not $RunA) { $RunA = Join-Path $repoRoot "bench\data\$($sorted[1].file)" }
}

# Make paths absolute
if (-not [System.IO.Path]::IsPathRooted($RunA)) { $RunA = Join-Path $repoRoot $RunA }
if (-not [System.IO.Path]::IsPathRooted($RunB)) { $RunB = Join-Path $repoRoot $RunB }

if (-not (Test-Path $RunA)) { Write-Error "Run A not found: $RunA"; return }
if (-not (Test-Path $RunB)) { Write-Error "Run B not found: $RunB"; return }

Write-Host "Baseline (A): $RunA"
Write-Host "After    (B): $RunB"

$a = Get-Content $RunA -Raw | ConvertFrom-Json
$b = Get-Content $RunB -Raw | ConvertFrom-Json

# Build benchmark lookup maps keyed by 'key'
function Get-BenchmarkMap($run) {
    $map = @{}
    foreach ($suite in $run.suites) {
        foreach ($bm in $suite.benchmarks) {
            if ($null -ne $bm.statistics) {
                $map[$bm.key] = @{
                    Suite      = $suite.suiteName
                    TypeName   = $bm.typeName
                    MethodName = $bm.methodName
                    Parameters = $bm.parameters
                    Stats      = $bm.statistics
                    GC         = $bm.gc
                    Key        = $bm.key
                    DisplayInfo = $bm.displayInfo
                }
            }
        }
    }
    return $map
}

$mapA = Get-BenchmarkMap $a
$mapB = Get-BenchmarkMap $b

# Classify benchmarks
$matched = @()
$onlyA = @()
$onlyB = @()

foreach ($key in $mapA.Keys) {
    if ($mapB.ContainsKey($key)) {
        $ba = $mapA[$key]; $bb = $mapB[$key]
        $beforeMean = $ba.Stats.meanNanoseconds
        $afterMean  = $bb.Stats.meanNanoseconds
        $changePct  = if ($beforeMean -ne 0) { (($afterMean - $beforeMean) / $beforeMean) * 100 } else { 0 }
        $speedup    = if ($afterMean -ne 0) { $beforeMean / $afterMean } else { 0 }

        # Determine library from method name
        $lib = 'Other'
        if ($ba.MethodName -match '^LeanLucene_') { $lib = 'LeanLucene' }
        elseif ($ba.MethodName -match '^LuceneNet_') { $lib = 'LuceneNet' }
        elseif ($ba.MethodName -match '^Lifti_') { $lib = 'Lifti' }

        $matched += [PSCustomObject]@{
            Key         = $key
            Suite       = $ba.Suite
            TypeName    = $ba.TypeName
            MethodName  = $ba.MethodName
            Library     = $lib
            BeforeMean  = $beforeMean
            AfterMean   = $afterMean
            ChangePct   = $changePct
            Speedup     = $speedup
            BeforeStats = $ba.Stats
            AfterStats  = $bb.Stats
            BeforeGC    = $ba.GC
            AfterGC     = $bb.GC
            Parameters  = $ba.Parameters
        }
    } else {
        $onlyA += $mapA[$key]
    }
}
foreach ($key in $mapB.Keys) {
    if (-not $mapA.ContainsKey($key)) {
        $onlyB += $mapB[$key]
    }
}

$matched = $matched | Sort-Object ChangePct

# Statistics
$improved  = @($matched | Where-Object { $_.ChangePct -lt -5 })
$regressed = @($matched | Where-Object { $_.ChangePct -gt 5 })
$neutral   = @($matched | Where-Object { $_.ChangePct -ge -5 -and $_.ChangePct -le 5 })
$avgChange = if ($matched.Count -gt 0) { ($matched | Measure-Object -Property ChangePct -Average).Average } else { 0 }
$medianChange = if ($matched.Count -gt 0) {
    $sorted = $matched.ChangePct | Sort-Object
    $mid = [math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2 -eq 0) { ($sorted[$mid - 1] + $sorted[$mid]) / 2 } else { $sorted[$mid] }
} else { 0 }

$bestImprovement = $matched | Select-Object -First 1
$worstRegression = $matched | Select-Object -Last 1

# Helper: format nanoseconds to human-readable
function Format-Ns([double]$ns) {
    if ($ns -ge 1e9)      { return "{0:N1} s" -f ($ns / 1e9) }
    if ($ns -ge 1e6)      { return "{0:N1} ms" -f ($ns / 1e6) }
    if ($ns -ge 1e3)      { return "{0:N1} µs" -f ($ns / 1e3) }
    return "{0:N1} ns" -f $ns
}

# Helper: format bytes to human-readable
function Format-Bytes([object]$bytes) {
    if ($null -eq $bytes -or $bytes -eq 0) { return '—' }
    $b = [double]$bytes
    if ($b -ge 1048576) { return "{0:N1} MB" -f ($b / 1048576) }
    if ($b -ge 1024)    { return "{0:N1} KB" -f ($b / 1024) }
    return "{0:N0} B" -f $b
}

# Helper: CSS class for change
function Get-ChangeClass([double]$pct) {
    if ($pct -lt -5) { return 'improved' }
    if ($pct -gt 5)  { return 'regressed' }
    return 'neutral'
}

# Helper: format change percentage with sign
function Format-ChangePct([double]$pct) {
    if ($pct -le 0) { return "&minus;{0:N1}%" -f [math]::Abs($pct) }
    return "+{0:N1}%" -f $pct
}

# Helper: short benchmark name from key
function Get-ShortName([string]$key) {
    # e.g. "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=1000, QueryTerm=search"
    $parts = $key -split '\|', 2
    $method = ($parts[0] -split '\.')[-1]
    # strip library prefix
    $method = $method -replace '^(LeanLucene|LuceneNet|Lifti)_', ''
    if ($parts.Count -gt 1) {
        $params = $parts[1].Trim()
        return "$method ($params)"
    }
    return $method
}

# Methodology notes
$notes = @()
$aSamples = ($matched | ForEach-Object { $_.BeforeStats.sampleCount } | Sort-Object -Unique)
$bSamples = ($matched | ForEach-Object { $_.AfterStats.sampleCount } | Sort-Object -Unique)
if ($aSamples.Count -gt 0 -and $bSamples.Count -gt 0) {
    $aMin = ($aSamples | Measure-Object -Minimum).Minimum
    $aMax = ($aSamples | Measure-Object -Maximum).Maximum
    $bMin = ($bSamples | Measure-Object -Minimum).Minimum
    $bMax = ($bSamples | Measure-Object -Maximum).Maximum
    if ($aMin -ne $bMin -or $aMax -ne $bMax) {
        $notes += "Sample counts differ between runs: Baseline $aMin–$aMax vs After $bMin–$bMax. Higher sample counts generally yield more stable measurements."
    }
}
$aHasAlloc = ($matched | Where-Object { $null -ne $_.BeforeGC -and $_.BeforeGC.bytesAllocatedPerOperation -gt 0 }).Count -gt 0
$bHasAlloc = ($matched | Where-Object { $null -ne $_.AfterGC -and $_.AfterGC.bytesAllocatedPerOperation -gt 0 }).Count -gt 0
if ($aHasAlloc -ne $bHasAlloc) {
    $which = if ($bHasAlloc) { 'After' } else { 'Baseline' }
    $notes += "Only the $which run includes allocation data (MemoryDiagnoser). Allocation comparisons are one-sided."
}
if ($a.runType -ne $b.runType) {
    $notes += "Run types differ: Baseline is '$($a.runType)', After is '$($b.runType)'. Results may not be directly comparable."
}

# ── Build per-suite breakdown ──
$suiteNames = $matched | Select-Object -ExpandProperty Suite -Unique | Sort-Object
$suiteData = @{}
foreach ($s in $suiteNames) {
    $items = @($matched | Where-Object { $_.Suite -eq $s })
    $avg = ($items | Measure-Object -Property ChangePct -Average).Average
    $best = $items | Sort-Object ChangePct | Select-Object -First 1
    $worst = $items | Sort-Object ChangePct -Descending | Select-Object -First 1
    $suiteData[$s] = @{
        Count = $items.Count
        Avg   = $avg
        Best  = $best
        Worst = $worst
        Items = $items
    }
}

# ── LeanLucene-specific ──
$llMatched = @($matched | Where-Object { $_.Library -eq 'LeanLucene' })
$llImproved = @($llMatched | Where-Object { $_.ChangePct -lt -5 } | Sort-Object ChangePct)
$llRegressed = @($llMatched | Where-Object { $_.ChangePct -gt 5 } | Sort-Object ChangePct -Descending)

# ── Build HTML ──
$sb = [System.Text.StringBuilder]::new(65536)

[void]$sb.AppendLine('<!DOCTYPE html>')
[void]$sb.AppendLine('<html lang="en">')
[void]$sb.AppendLine('<head>')
[void]$sb.AppendLine('<meta charset="utf-8"/>')
[void]$sb.AppendLine("<title>LeanLucene Benchmark Comparison — $($a.commitHash) → $($b.commitHash)</title>")
[void]$sb.AppendLine('<style>')
[void]$sb.AppendLine(@'
  :root { --bg: #0d1117; --card: #161b22; --border: #30363d; --text: #c9d1d9; --muted: #8b949e;
          --green: #3fb950; --red: #f85149; --yellow: #d29922; --blue: #58a6ff; --purple: #bc8cff; }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: 'Segoe UI', -apple-system, sans-serif; background: var(--bg); color: var(--text); padding: 2rem; line-height: 1.6; }
  h1 { font-size: 1.8rem; margin-bottom: .5rem; }
  h2 { font-size: 1.35rem; margin: 2rem 0 1rem; border-bottom: 1px solid var(--border); padding-bottom: .4rem; }
  h3 { font-size: 1.1rem; margin: 1.5rem 0 .6rem; color: var(--blue); }
  a { color: var(--blue); text-decoration: none; }
  a:hover { text-decoration: underline; }
  .meta { color: var(--muted); font-size: .85rem; margin-bottom: 1.5rem; }
  .meta code { background: var(--card); padding: 2px 6px; border-radius: 4px; font-size: .82rem; }

  .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin: 1.5rem 0; }
  .card { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 1.2rem; }
  .card .label { font-size: .75rem; text-transform: uppercase; color: var(--muted); letter-spacing: .05em; }
  .card .value { font-size: 2rem; font-weight: 700; margin-top: .2rem; }
  .card .sub { font-size: .8rem; color: var(--muted); margin-top: .15rem; }
  .card .value.green { color: var(--green); }
  .card .value.red { color: var(--red); }
  .card .value.yellow { color: var(--yellow); }

  table { width: 100%; border-collapse: collapse; font-size: .82rem; margin: 1rem 0; }
  thead th { background: var(--card); position: sticky; top: 0; text-align: left; padding: .6rem .5rem;
             border-bottom: 2px solid var(--border); font-weight: 600; white-space: nowrap; }
  td { padding: .45rem .5rem; border-bottom: 1px solid var(--border); }
  tr:hover td { background: rgba(56,139,253,.06); }
  .num { text-align: right; font-variant-numeric: tabular-nums; font-family: 'Cascadia Code', 'Fira Code', monospace; }
  .improved { color: var(--green); }
  .regressed { color: var(--red); }
  .neutral { color: var(--muted); }
  .bar-cell { width: 120px; }
  .bar-outer { background: var(--card); border-radius: 3px; height: 16px; position: relative; overflow: hidden; }
  .bar-inner { height: 100%; border-radius: 3px; position: absolute; }
  .bar-inner.g { background: var(--green); right: 50%; }
  .bar-inner.r { background: var(--red); left: 50%; }
  .bar-mid { position: absolute; left: 50%; top: 0; bottom: 0; width: 1px; background: var(--muted); opacity: .4; }

  .tag { display: inline-block; padding: 1px 8px; border-radius: 12px; font-size: .72rem; font-weight: 600; }
  .tag-faster { background: rgba(63,185,80,.15); color: var(--green); }
  .tag-slower { background: rgba(248,81,73,.15); color: var(--red); }
  .tag-neutral { background: rgba(139,148,158,.15); color: var(--muted); }
  .tag-suite { background: rgba(188,140,255,.15); color: var(--purple); }

  .note { background: var(--card); border-left: 3px solid var(--blue); padding: .8rem 1rem; margin: 1rem 0; border-radius: 0 6px 6px 0; font-size: .85rem; }
  .note.warn { border-left-color: var(--yellow); }
  .note.good { border-left-color: var(--green); }

  .chart-container { display: flex; flex-wrap: wrap; gap: 1rem; margin: 1rem 0; }
  .suite-chart { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 1rem; min-width: 280px; flex: 1; }
  .suite-chart h4 { font-size: .9rem; margin-bottom: .6rem; }
  .hbar { display: flex; align-items: center; margin: .3rem 0; font-size: .78rem; }
  .hbar .hbar-label { width: 120px; text-align: right; padding-right: 8px; color: var(--muted); flex-shrink: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .hbar .hbar-track { flex: 1; height: 18px; background: rgba(255,255,255,.03); border-radius: 3px; position: relative; }
  .hbar .hbar-fill { height: 100%; border-radius: 3px; position: absolute; top: 0; }
  .hbar .hbar-fill.g { background: var(--green); right: 50%; }
  .hbar .hbar-fill.r { background: var(--red); left: 50%; }
  .hbar .hbar-center { position: absolute; left: 50%; top: 0; bottom: 0; width: 1px; background: var(--muted); opacity: .3; }
  .hbar .hbar-val { width: 70px; text-align: right; padding-left: 6px; flex-shrink: 0; }

  footer { margin-top: 3rem; padding-top: 1rem; border-top: 1px solid var(--border); color: var(--muted); font-size: .78rem; }
  .toc { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 1rem 1.5rem; margin: 1.5rem 0; }
  .toc ol { padding-left: 1.2rem; }
  .toc li { margin: .3rem 0; }
'@)
[void]$sb.AppendLine('</style>')
[void]$sb.AppendLine('</head>')
[void]$sb.AppendLine('<body>')

# ── Title & Meta ──
[void]$sb.AppendLine("<h1>&#x1F3CE;&#xFE0F; Benchmark Comparison Report</h1>")

$dateA = if ($a.generatedAtUtc) { try { ([DateTime]$a.generatedAtUtc).ToString('yyyy-MM-dd HH:mm') } catch { "$($a.generatedAtUtc)" } } else { 'N/A' }
$dateB = if ($b.generatedAtUtc) { try { ([DateTime]$b.generatedAtUtc).ToString('yyyy-MM-dd HH:mm') } catch { "$($b.generatedAtUtc)" } } else { 'N/A' }

[void]$sb.AppendLine("<div class=`"meta`">")
[void]$sb.AppendLine("  Baseline: <code>$($a.commitHash)</code> ($dateA) vs After: <code>$($b.commitHash)</code> ($dateB)")
[void]$sb.AppendLine("  &middot; Host: $($b.hostMachineName) &middot; .NET $($b.dotnetVersion)")
[void]$sb.AppendLine("  &middot; Matched: $($matched.Count) benchmarks")
[void]$sb.AppendLine("</div>")

# Methodology notes
if ($notes.Count -gt 0) {
    [void]$sb.AppendLine('<div class="note warn">')
    [void]$sb.AppendLine('  <strong>Methodology notes:</strong><br/>')
    foreach ($n in $notes) {
        [void]$sb.AppendLine("  $n<br/>")
    }
    [void]$sb.AppendLine('</div>')
}

# TOC
[void]$sb.AppendLine('<nav class="toc">')
[void]$sb.AppendLine('  <strong>Contents</strong>')
[void]$sb.AppendLine('  <ol>')
[void]$sb.AppendLine('    <li><a href="#summary">Executive Summary</a></li>')
[void]$sb.AppendLine('    <li><a href="#metadata">Run Metadata</a></li>')
[void]$sb.AppendLine('    <li><a href="#suites">Per-Suite Breakdown</a></li>')
[void]$sb.AppendLine('    <li><a href="#leanlucene">LeanLucene-Specific Analysis</a></li>')
[void]$sb.AppendLine('    <li><a href="#allocations">Allocation Comparison</a></li>')
[void]$sb.AppendLine('    <li><a href="#full-table">Full Comparison Table</a></li>')
[void]$sb.AppendLine('    <li><a href="#unmatched">Unmatched Benchmarks</a></li>')
[void]$sb.AppendLine('  </ol>')
[void]$sb.AppendLine('</nav>')

# ═══════════════════ 1. EXECUTIVE SUMMARY ═══════════════════
[void]$sb.AppendLine('<h2 id="summary">1. Executive Summary</h2>')
[void]$sb.AppendLine('<div class="cards">')

$avgClass = if ($avgChange -lt -5) { 'green' } elseif ($avgChange -gt 5) { 'red' } else { 'yellow' }
$medClass = if ($medianChange -lt -5) { 'green' } elseif ($medianChange -gt 5) { 'red' } else { 'yellow' }

[void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Overall Average</div><div class=`"value $avgClass`">$(Format-ChangePct $avgChange)</div><div class=`"sub`">Mean time change across all $($matched.Count) matched benchmarks</div></div>")
[void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Median Change</div><div class=`"value $medClass`">$(Format-ChangePct $medianChange)</div><div class=`"sub`">Middle benchmark change</div></div>")
[void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Improved (&gt;5%)</div><div class=`"value green`">$($improved.Count)</div><div class=`"sub`">of $($matched.Count) benchmarks got faster</div></div>")
[void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Regressed (&gt;5%)</div><div class=`"value red`">$($regressed.Count)</div><div class=`"sub`">of $($matched.Count) benchmarks got slower</div></div>")
[void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Neutral</div><div class=`"value yellow`">$($neutral.Count)</div><div class=`"sub`">within &plusmn;5% threshold</div></div>")

if ($bestImprovement) {
    [void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Best Improvement</div><div class=`"value green`">$(Format-ChangePct $bestImprovement.ChangePct)</div><div class=`"sub`">$(Get-ShortName $bestImprovement.Key)</div></div>")
}
if ($worstRegression -and $worstRegression.ChangePct -gt 5) {
    [void]$sb.AppendLine("  <div class=`"card`"><div class=`"label`">Worst Regression</div><div class=`"value red`">$(Format-ChangePct $worstRegression.ChangePct)</div><div class=`"sub`">$(Get-ShortName $worstRegression.Key)</div></div>")
}
[void]$sb.AppendLine('</div>')

# Key takeaway
$takeawayClass = if ($improved.Count -gt $regressed.Count) { 'good' } elseif ($regressed.Count -gt $improved.Count) { 'warn' } else { '' }
$pctImproved = if ($matched.Count -gt 0) { [math]::Round(($improved.Count / $matched.Count) * 100) } else { 0 }
[void]$sb.AppendLine("<div class=`"note $takeawayClass`">")
[void]$sb.AppendLine("  <strong>Key takeaway:</strong> $pctImproved% of matched benchmarks improved ($($improved.Count)/$($matched.Count)), with a median change of $(Format-ChangePct $medianChange).")
[void]$sb.AppendLine('</div>')

# ═══════════════════ 2. RUN METADATA ═══════════════════
[void]$sb.AppendLine('<h2 id="metadata">2. Run Metadata</h2>')
[void]$sb.AppendLine('<table>')
[void]$sb.AppendLine('  <thead><tr><th>Property</th><th>Baseline (Run A)</th><th>After (Run B)</th></tr></thead>')
[void]$sb.AppendLine('  <tbody>')
[void]$sb.AppendLine("    <tr><td>Run ID</td><td>$($a.runId)</td><td>$($b.runId)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Commit</td><td><code>$($a.commitHash)</code></td><td><code>$($b.commitHash)</code></td></tr>")
[void]$sb.AppendLine("    <tr><td>Date (UTC)</td><td>$dateA</td><td>$dateB</td></tr>")
[void]$sb.AppendLine("    <tr><td>Run Type</td><td>$($a.runType)</td><td>$($b.runType)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Host</td><td>$($a.hostMachineName)</td><td>$($b.hostMachineName)</td></tr>")
[void]$sb.AppendLine("    <tr><td>.NET Version</td><td>$($a.dotnetVersion)</td><td>$($b.dotnetVersion)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Total Benchmarks</td><td>$($a.totalBenchmarkCount)</td><td>$($b.totalBenchmarkCount)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Matched</td><td colspan=`"2`">$($matched.Count)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Only in Baseline</td><td colspan=`"2`">$($onlyA.Count)</td></tr>")
[void]$sb.AppendLine("    <tr><td>Only in After</td><td colspan=`"2`">$($onlyB.Count)</td></tr>")
[void]$sb.AppendLine('  </tbody>')
[void]$sb.AppendLine('</table>')

# ═══════════════════ 3. PER-SUITE BREAKDOWN ═══════════════════
[void]$sb.AppendLine('<h2 id="suites">3. Per-Suite Breakdown</h2>')
[void]$sb.AppendLine('<div class="chart-container">')

foreach ($s in $suiteNames) {
    $sd = $suiteData[$s]
    $avgClass2 = Get-ChangeClass $sd.Avg
    [void]$sb.AppendLine("<div class=`"suite-chart`">")
    [void]$sb.AppendLine("  <h4><span class=`"tag tag-suite`">$s</span> $($sd.Count) benchmarks &middot; Avg: <span class=`"$avgClass2`">$(Format-ChangePct $sd.Avg)</span></h4>")

    # Show individual bars for each benchmark in this suite (up to 8, then summarise)
    $items = $sd.Items | Sort-Object ChangePct
    $showCount = [math]::Min($items.Count, 8)
    for ($i = 0; $i -lt $showCount; $i++) {
        $item = $items[$i]
        $pct = $item.ChangePct
        $cls = Get-ChangeClass $pct
        $barDir = if ($pct -lt 0) { 'g' } else { 'r' }
        $barWidth = [math]::Min([math]::Abs($pct) / 2, 50)
        $shortName = (Get-ShortName $item.Key)
        if ($shortName.Length -gt 20) { $shortName = $shortName.Substring(0, 17) + '...' }
        [void]$sb.AppendLine("  <div class=`"hbar`"><span class=`"hbar-label`" title=`"$(Get-ShortName $item.Key)`">$shortName</span><div class=`"hbar-track`"><div class=`"hbar-center`"></div><div class=`"hbar-fill $barDir`" style=`"width:$("{0:N1}" -f $barWidth)%`"></div></div><span class=`"hbar-val $cls`">$(Format-ChangePct $pct)</span></div>")
    }
    if ($items.Count -gt 8) {
        [void]$sb.AppendLine("  <div style=`"font-size:.75rem;color:var(--muted);margin-top:.3rem`">...and $($items.Count - 8) more</div>")
    }

    [void]$sb.AppendLine("</div>")
}
[void]$sb.AppendLine('</div>')

# ═══════════════════ 4. LEANLUCENE-SPECIFIC ═══════════════════
[void]$sb.AppendLine('<h2 id="leanlucene">4. LeanLucene-Specific Analysis</h2>')

if ($llMatched.Count -eq 0) {
    [void]$sb.AppendLine('<div class="note">No LeanLucene-specific benchmarks found in the matched set.</div>')
} else {
    $llAvg = ($llMatched | Measure-Object -Property ChangePct -Average).Average
    $llAvgClass = Get-ChangeClass $llAvg
    [void]$sb.AppendLine("<div class=`"note`"><strong>LeanLucene overall:</strong> $($llMatched.Count) benchmarks, average change <span class=`"$llAvgClass`">$(Format-ChangePct $llAvg)</span> ($($llImproved.Count) improved, $($llRegressed.Count) regressed)</div>")

    # Top improvements
    if ($llImproved.Count -gt 0) {
        [void]$sb.AppendLine('<h3>4.1 Top Improvements</h3>')
        [void]$sb.AppendLine('<table>')
        [void]$sb.AppendLine('  <thead><tr><th>Benchmark</th><th>Suite</th><th class="num">Before</th><th class="num">After</th><th class="num">Change</th><th class="num">Speedup</th></tr></thead>')
        [void]$sb.AppendLine('  <tbody>')
        $showLimit = [math]::Min($llImproved.Count, 15)
        for ($i = 0; $i -lt $showLimit; $i++) {
            $item = $llImproved[$i]
            [void]$sb.AppendLine("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td class=`"num`">$(Format-Ns $item.BeforeMean)</td><td class=`"num`">$(Format-Ns $item.AfterMean)</td><td class=`"num improved`">$(Format-ChangePct $item.ChangePct)</td><td class=`"num`">$("{0:N2}" -f $item.Speedup)&times;</td></tr>")
        }
        [void]$sb.AppendLine('  </tbody>')
        [void]$sb.AppendLine('</table>')
    }

    # Top regressions
    if ($llRegressed.Count -gt 0) {
        [void]$sb.AppendLine('<h3>4.2 Top Regressions</h3>')
        [void]$sb.AppendLine('<table>')
        [void]$sb.AppendLine('  <thead><tr><th>Benchmark</th><th>Suite</th><th class="num">Before</th><th class="num">After</th><th class="num">Change</th><th class="num">&Delta; Absolute</th></tr></thead>')
        [void]$sb.AppendLine('  <tbody>')
        $showLimit = [math]::Min($llRegressed.Count, 10)
        for ($i = 0; $i -lt $showLimit; $i++) {
            $item = $llRegressed[$i]
            $absDelta = $item.AfterMean - $item.BeforeMean
            [void]$sb.AppendLine("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td class=`"num`">$(Format-Ns $item.BeforeMean)</td><td class=`"num`">$(Format-Ns $item.AfterMean)</td><td class=`"num regressed`">$(Format-ChangePct $item.ChangePct)</td><td class=`"num`">+$(Format-Ns $absDelta)</td></tr>")
        }
        [void]$sb.AppendLine('  </tbody>')
        [void]$sb.AppendLine('</table>')
    }
}

# ═══════════════════ 5. ALLOCATION COMPARISON ═══════════════════
[void]$sb.AppendLine('<h2 id="allocations">5. Allocation Comparison</h2>')

$hasAnyAlloc = $aHasAlloc -or $bHasAlloc
if (-not $hasAnyAlloc) {
    [void]$sb.AppendLine('<div class="note">Neither run includes allocation data (MemoryDiagnoser was not enabled).</div>')
} else {
    if (-not $aHasAlloc) {
        [void]$sb.AppendLine('<div class="note warn">Baseline run did not include <code>MemoryDiagnoser</code>. Allocation data shown is from the After run only.</div>')
    } elseif (-not $bHasAlloc) {
        [void]$sb.AppendLine('<div class="note warn">After run did not include <code>MemoryDiagnoser</code>. Allocation data shown is from the Baseline run only.</div>')
    }

    # Allocation table: show per-benchmark allocations
    $allocItems = @($matched | Where-Object {
        ($null -ne $_.BeforeGC -and $_.BeforeGC.bytesAllocatedPerOperation -gt 0) -or
        ($null -ne $_.AfterGC -and $_.AfterGC.bytesAllocatedPerOperation -gt 0)
    } | Sort-Object { $_.Library }, { $_.Suite })

    if ($allocItems.Count -gt 0) {
        # LeanLucene vs competitors side-by-side where both exist
        $llAllocBenchmarks = @($allocItems | Where-Object { $_.Library -eq 'LeanLucene' })
        $otherAllocBenchmarks = @($allocItems | Where-Object { $_.Library -ne 'LeanLucene' })

        if ($llAllocBenchmarks.Count -gt 0 -and $otherAllocBenchmarks.Count -gt 0) {
            [void]$sb.AppendLine('<h3>5.1 LeanLucene vs Competitors (Allocations)</h3>')
            [void]$sb.AppendLine('<table>')
            [void]$sb.AppendLine('  <thead><tr><th>Suite</th><th>Benchmark Type</th><th class="num">LeanLucene</th>')

            # Discover which other libraries we have
            $otherLibs = @($otherAllocBenchmarks | Select-Object -ExpandProperty Library -Unique | Sort-Object)
            foreach ($ol in $otherLibs) {
                [void]$sb.AppendLine("    <th class=`"num`">$ol</th>")
            }
            [void]$sb.AppendLine('  </tr></thead><tbody>')

            # Group by TypeName to find matching benchmarks
            foreach ($llItem in $llAllocBenchmarks) {
                $afterAllocLL = if ($null -ne $llItem.AfterGC) { $llItem.AfterGC.bytesAllocatedPerOperation } else { $null }
                $beforeAllocLL = if ($null -ne $llItem.BeforeGC) { $llItem.BeforeGC.bytesAllocatedPerOperation } else { $null }
                $displayAlloc = if ($null -ne $afterAllocLL -and $afterAllocLL -gt 0) { $afterAllocLL } else { $beforeAllocLL }

                [void]$sb.Append("    <tr><td>$($llItem.Suite)</td><td>$(Get-ShortName $llItem.Key)</td><td class=`"num`">$(Format-Bytes $displayAlloc)</td>")

                foreach ($ol in $otherLibs) {
                    # Find matching benchmark by TypeName + parameters but different library
                    $matchingOther = $otherAllocBenchmarks | Where-Object {
                        $_.Library -eq $ol -and $_.TypeName -eq $llItem.TypeName -and
                        ($_.Parameters | ConvertTo-Json -Compress) -eq ($llItem.Parameters | ConvertTo-Json -Compress)
                    } | Select-Object -First 1

                    if ($matchingOther) {
                        $otherAlloc = if ($null -ne $matchingOther.AfterGC) { $matchingOther.AfterGC.bytesAllocatedPerOperation } else { $null }
                        if ($null -eq $otherAlloc -or $otherAlloc -eq 0) {
                            $otherAlloc = if ($null -ne $matchingOther.BeforeGC) { $matchingOther.BeforeGC.bytesAllocatedPerOperation } else { $null }
                        }
                        [void]$sb.Append("<td class=`"num`">$(Format-Bytes $otherAlloc)</td>")
                    } else {
                        [void]$sb.Append('<td class="num">&mdash;</td>')
                    }
                }
                [void]$sb.AppendLine('</tr>')
            }
            [void]$sb.AppendLine('  </tbody>')
            [void]$sb.AppendLine('</table>')
        }

        # Full allocation table
        [void]$sb.AppendLine('<h3>5.2 Per-Benchmark Allocations</h3>')
        [void]$sb.AppendLine('<table>')
        $headerRow = '  <thead><tr><th>Benchmark</th><th>Suite</th><th>Library</th>'
        if ($aHasAlloc) { $headerRow += '<th class="num">Baseline Alloc</th>' }
        if ($bHasAlloc) { $headerRow += '<th class="num">After Alloc</th>' }
        if ($aHasAlloc -and $bHasAlloc) { $headerRow += '<th class="num">Alloc Change</th>' }
        $headerRow += '</tr></thead>'
        [void]$sb.AppendLine($headerRow)
        [void]$sb.AppendLine('  <tbody>')

        foreach ($item in $allocItems) {
            $beforeAlloc = if ($null -ne $item.BeforeGC) { $item.BeforeGC.bytesAllocatedPerOperation } else { $null }
            $afterAlloc = if ($null -ne $item.AfterGC) { $item.AfterGC.bytesAllocatedPerOperation } else { $null }

            [void]$sb.Append("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td>$($item.Library)</td>")
            if ($aHasAlloc) { [void]$sb.Append("<td class=`"num`">$(Format-Bytes $beforeAlloc)</td>") }
            if ($bHasAlloc) { [void]$sb.Append("<td class=`"num`">$(Format-Bytes $afterAlloc)</td>") }
            if ($aHasAlloc -and $bHasAlloc -and $null -ne $beforeAlloc -and $beforeAlloc -gt 0 -and $null -ne $afterAlloc) {
                $allocChange = (($afterAlloc - $beforeAlloc) / $beforeAlloc) * 100
                $allocCls = Get-ChangeClass $allocChange
                [void]$sb.Append("<td class=`"num $allocCls`">$(Format-ChangePct $allocChange)</td>")
            } elseif ($aHasAlloc -and $bHasAlloc) {
                [void]$sb.Append('<td class="num">&mdash;</td>')
            }
            [void]$sb.AppendLine('</tr>')
        }

        [void]$sb.AppendLine('  </tbody>')
        [void]$sb.AppendLine('</table>')

        # Flag benchmarks with null/zero allocation
        $zeroAlloc = @($matched | Where-Object {
            ($null -eq $_.AfterGC -or $_.AfterGC.bytesAllocatedPerOperation -eq 0) -and
            ($null -eq $_.BeforeGC -or $_.BeforeGC.bytesAllocatedPerOperation -eq 0)
        })
        if ($zeroAlloc.Count -gt 0) {
            [void]$sb.AppendLine("<div class=`"note`"><strong>Note:</strong> $($zeroAlloc.Count) benchmark(s) report zero/null allocation data in both runs.</div>")
        }
    }
}

# ═══════════════════ 6. FULL COMPARISON TABLE ═══════════════════
[void]$sb.AppendLine('<h2 id="full-table">6. Full Comparison Table</h2>')
[void]$sb.AppendLine('<table>')
[void]$sb.AppendLine('  <thead><tr><th>Benchmark</th><th>Suite</th><th>Library</th><th class="num">Before (mean)</th><th class="num">After (mean)</th><th class="num">Change</th><th class="bar-cell">Visual</th><th class="num">After Alloc</th></tr></thead>')
[void]$sb.AppendLine('  <tbody>')

foreach ($item in $matched) {
    $cls = Get-ChangeClass $item.ChangePct
    $barDir = if ($item.ChangePct -lt 0) { 'g' } else { 'r' }
    $barWidth = [math]::Min([math]::Abs($item.ChangePct) / 2, 50)
    $afterAlloc = if ($null -ne $item.AfterGC) { Format-Bytes $item.AfterGC.bytesAllocatedPerOperation } else { '&mdash;' }

    [void]$sb.AppendLine("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td>$($item.Library)</td><td class=`"num`">$(Format-Ns $item.BeforeMean)</td><td class=`"num`">$(Format-Ns $item.AfterMean)</td><td class=`"num $cls`">$(Format-ChangePct $item.ChangePct)</td><td class=`"bar-cell`"><div class=`"bar-outer`"><div class=`"bar-mid`"></div><div class=`"bar-inner $barDir`" style=`"width:$("{0:N1}" -f $barWidth)%`"></div></div></td><td class=`"num`">$afterAlloc</td></tr>")
}

[void]$sb.AppendLine('  </tbody>')
[void]$sb.AppendLine('</table>')

# ═══════════════════ 7. UNMATCHED BENCHMARKS ═══════════════════
[void]$sb.AppendLine('<h2 id="unmatched">7. Unmatched Benchmarks</h2>')

if ($onlyA.Count -eq 0 -and $onlyB.Count -eq 0) {
    [void]$sb.AppendLine('<div class="note good">All benchmarks matched between runs. No unmatched benchmarks.</div>')
} else {
    if ($onlyA.Count -gt 0) {
        [void]$sb.AppendLine("<h3>7.1 Only in Baseline ($($onlyA.Count))</h3>")
        [void]$sb.AppendLine('<table>')
        [void]$sb.AppendLine('  <thead><tr><th>Benchmark</th><th>Suite</th><th class="num">Mean</th><th class="num">Median</th><th class="num">Std Dev</th><th class="num">Alloc</th></tr></thead>')
        [void]$sb.AppendLine('  <tbody>')
        foreach ($item in ($onlyA | Sort-Object { $_.Suite }, { $_.Key })) {
            $allocVal = if ($null -ne $item.GC) { Format-Bytes $item.GC.bytesAllocatedPerOperation } else { '&mdash;' }
            [void]$sb.AppendLine("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td class=`"num`">$(Format-Ns $item.Stats.meanNanoseconds)</td><td class=`"num`">$(Format-Ns $item.Stats.medianNanoseconds)</td><td class=`"num`">$(Format-Ns $item.Stats.standardDeviationNanoseconds)</td><td class=`"num`">$allocVal</td></tr>")
        }
        [void]$sb.AppendLine('  </tbody></table>')
    }

    if ($onlyB.Count -gt 0) {
        [void]$sb.AppendLine("<h3>7.2 Only in After ($($onlyB.Count))</h3>")
        [void]$sb.AppendLine('<table>')
        [void]$sb.AppendLine('  <thead><tr><th>Benchmark</th><th>Suite</th><th class="num">Mean</th><th class="num">Median</th><th class="num">Std Dev</th><th class="num">Alloc</th></tr></thead>')
        [void]$sb.AppendLine('  <tbody>')
        foreach ($item in ($onlyB | Sort-Object { $_.Suite }, { $_.Key })) {
            $allocVal = if ($null -ne $item.GC) { Format-Bytes $item.GC.bytesAllocatedPerOperation } else { '&mdash;' }
            [void]$sb.AppendLine("    <tr><td>$(Get-ShortName $item.Key)</td><td><span class=`"tag tag-suite`">$($item.Suite)</span></td><td class=`"num`">$(Format-Ns $item.Stats.meanNanoseconds)</td><td class=`"num`">$(Format-Ns $item.Stats.medianNanoseconds)</td><td class=`"num`">$(Format-Ns $item.Stats.standardDeviationNanoseconds)</td><td class=`"num`">$allocVal</td></tr>")
        }
        [void]$sb.AppendLine('  </tbody></table>')
    }
}

# Footer
[void]$sb.AppendLine('<footer>')
[void]$sb.AppendLine("  LeanLucene Benchmark Comparison &middot; Generated $(Get-Date -Format 'yyyy-MM-dd HH:mm') &middot;")
[void]$sb.AppendLine("  Baseline: <code>$($a.commitHash)</code> ($($a.runType), $($a.totalBenchmarkCount) benchmarks) &rarr;")
[void]$sb.AppendLine("  After: <code>$($b.commitHash)</code> ($($b.runType), $($b.totalBenchmarkCount) benchmarks)")
if ($b.dotnetVersion) { [void]$sb.AppendLine("  &middot; .NET $($b.dotnetVersion)") }
if ($b.hostMachineName) { [void]$sb.AppendLine("  &middot; Host: $($b.hostMachineName)") }
[void]$sb.AppendLine('</footer>')
[void]$sb.AppendLine('</body>')
[void]$sb.AppendLine('</html>')

# Write output
$outDir = Join-Path $repoRoot 'bench\comparisons'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$outFile = Join-Path $outDir "$($a.runId) vs $($b.runId).html"
$sb.ToString() | Set-Content -Path $outFile -Encoding UTF8
Write-Host ""
Write-Host "Report written to: $outFile"
Write-Host "  Matched: $($matched.Count)  Improved: $($improved.Count)  Regressed: $($regressed.Count)  Neutral: $($neutral.Count)"
Write-Host "  Only in A: $($onlyA.Count)  Only in B: $($onlyB.Count)"
