param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\example\Rowles.LeanLucene.Example.Benchmarks\Rowles.LeanLucene.Example.Benchmarks.csproj"))

dotnet run -c Release --project $projectPath -- --suite smallindex @BenchmarkArgs
