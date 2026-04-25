# Benchmarking

The benchmark project lives at `src/Rowles.LeanLucene.Benchmarks` and uses
BenchmarkDotNet.

## Run

```bash
dotnet run -c Release --project src/Rowles.LeanLucene.Benchmarks -- --filter '*'
```

Filter to a specific class:

```bash
dotnet run -c Release --project src/Rowles.LeanLucene.Benchmarks -- --filter '*SearchBenchmarks*'
```

## Output layout

Results land under `./bench/{machine}/...`, where `{machine}` is the local
hostname. This keeps results from different machines from overwriting each other
when the directory is shared via source control.

The corpus and any generated indices used by the benchmarks also live under
`./bench`.

## Comparing runs

BenchmarkDotNet writes `Markdown`, `CSV`, and `JSON` reports per run. Diff JSON
files between runs to surface regressions.
