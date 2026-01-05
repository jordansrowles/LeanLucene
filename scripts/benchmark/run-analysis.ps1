# Delegates to the unified benchmark script. See .\scripts\benchmark.ps1 --help
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$unified = Join-Path $PSScriptRoot "..\benchmark.ps1"
& $unified --suite analysis @BenchmarkArgs
