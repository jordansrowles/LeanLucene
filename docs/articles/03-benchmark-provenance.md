# Benchmark provenance

Benchmark runs now write a consolidated `report.json` and a per-machine
`index.json` under:

```text
bench/{machine}/{yyyy-MM-dd}/{HH-mm}/
```

The report records the commit, runtime, BenchmarkDotNet version, effective
document count and data source fingerprints. This makes copied runs and shared
benchmark folders easier to compare.

When running outside a Git checkout, pass the source metadata explicitly:

```powershell
.\scripts\benchmark.ps1 -SourceCommit abc123 -SourceRef main -SourceManifest manifest.json
```

See [Benchmarking](../tutorials/tips/03-benchmarking.md).
