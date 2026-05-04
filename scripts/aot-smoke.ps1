param(
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\examples\Rowles.LeanLucene.Example.NativeAot\Rowles.LeanLucene.Example.NativeAot.csproj"

dotnet publish $project -c Release -r $RuntimeIdentifier --self-contained true -p:PublishAot=true

$publishDirectory = Join-Path $repoRoot "src\examples\Rowles.LeanLucene.Example.NativeAot\bin\Release\net10.0\$RuntimeIdentifier\publish"
$executable = if ($RuntimeIdentifier.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)) {
    Join-Path $publishDirectory "Rowles.LeanLucene.Example.NativeAot.exe"
} else {
    Join-Path $publishDirectory "Rowles.LeanLucene.Example.NativeAot"
}

& $executable
if ($LASTEXITCODE -ne 0) {
    throw "Native AOT smoke executable failed with exit code $LASTEXITCODE."
}
