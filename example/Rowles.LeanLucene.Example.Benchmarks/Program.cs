using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Globalization;

namespace Rowles.LeanLucene.Example.Benchmarks;

internal static class Program
{
    public static int Main(string[] args)
    {
        var (suite, benchmarkArgs, showHelp, docCount) = ParseArguments(args);

        if (showHelp)
        {
            PrintHelp();
            return 0;
        }

        // Expose doc count override as env var for [GlobalSetup] to read
        if (docCount is not null)
            Environment.SetEnvironmentVariable("BENCH_DOC_COUNT", docCount.Value.ToString(CultureInfo.InvariantCulture));

        var repoRoot = FindRepositoryRoot();
        var benchRoot = Path.Combine(repoRoot, "bench");
        var dataDirectory = Path.Combine(benchRoot, "data");
        Directory.CreateDirectory(dataDirectory);

        var commitHash = GetGitShortHash(repoRoot);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var runId = string.IsNullOrEmpty(commitHash)
            ? timestamp
            : $"{timestamp}-{commitHash}";

        var suiteSummaries = new List<(string Suite, Summary Summary)>();

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Query)
            RunSuite<TermQueryBenchmarks>("query", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Index)
            RunSuite<IndexingBenchmarks>("index", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Analysis)
            RunSuite<AnalysisBenchmarks>("analysis", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Boolean)
            RunSuite<BooleanQueryBenchmarks>("boolean", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Phrase)
            RunSuite<PhraseQueryBenchmarks>("phrase", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.SmallIndex)
            RunSuite<SmallIndexBenchmarks>("smallindex", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Prefix)
            RunSuite<PrefixQueryBenchmarks>("prefix", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Fuzzy)
            RunSuite<FuzzyQueryBenchmarks>("fuzzy", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Wildcard)
            RunSuite<WildcardQueryBenchmarks>("wildcard", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Deletion)
            RunSuite<DeletionBenchmarks>("deletion", dataDirectory, runId, benchmarkArgs, suiteSummaries);

        if (suiteSummaries.Count == 0)
        {
            Console.Error.WriteLine("No benchmark suite selected.");
            return 1;
        }

        // Build and write consolidated report + index.json
        var report = BenchmarkRunReportBuilder.Build(
            runId,
            DateTimeOffset.UtcNow,
            benchmarkArgs,
            suiteSummaries);
        report.CommitHash = commitHash;

        BenchmarkRunReportWriter.WriteReport(dataDirectory, report);

        Console.WriteLine();
        Console.WriteLine($"Run:    {runId}");
        Console.WriteLine($"Commit: {(string.IsNullOrEmpty(commitHash) ? "(unknown)" : commitHash)}");
        Console.WriteLine($"Output: {Path.Combine(dataDirectory, runId)}");
        Console.WriteLine($"Report: {Path.Combine(dataDirectory, $"{runId}.json")}");
        Console.WriteLine($"Suites: {string.Join(", ", suiteSummaries.Select(s => s.Suite))}");
        return 0;
    }

    private static void RunSuite<T>(
        string suiteName,
        string dataDirectory,
        string runId,
        string[] benchmarkArgs,
        List<(string Suite, Summary Summary)> suiteSummaries) where T : class
    {
        var artifactsPath = Path.Combine(dataDirectory, runId, suiteName);
        Directory.CreateDirectory(artifactsPath);
        var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
        var summary = BenchmarkRunner.Run<T>(config, benchmarkArgs);
        suiteSummaries.Add((suiteName, summary));
    }

    private static string GetGitShortHash(string repoRoot)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            LeanLucene Benchmark Runner

            Usage:
              dotnet run -c Release --project <path> -- [options] [-- BenchmarkDotNet args]

            Options:
              --suite <name>   Run a specific benchmark suite (default: all)
              --doccount <n>   Override document count for all suites (env: BENCH_DOC_COUNT)
              --help, -h       Show this help message

            Suites:
              all              Run all benchmark suites (default)
              index            IndexingBenchmarks — bulk indexing throughput (3K docs)
              query            TermQueryBenchmarks — single-term search with competitors
              analysis         AnalysisBenchmarks — tokenisation pipeline throughput
              boolean          BooleanQueryBenchmarks — Must/Should/MustNot queries
              phrase           PhraseQueryBenchmarks — exact and slop phrase matching
              prefix           PrefixQueryBenchmarks — prefix matching (vs Lucene.NET + Lifti)
              fuzzy            FuzzyQueryBenchmarks — fuzzy/edit-distance matching
              wildcard         WildcardQueryBenchmarks — wildcard pattern matching
              deletion         DeletionBenchmarks — delete throughput (vs Lucene.NET + Lifti)
              smallindex       SmallIndexBenchmarks — 100-doc roundtrip overhead

            Output:
              Results are written to bench/data/<timestamp>-<commit>/<suite>/
              Each suite produces JSON, Markdown, and HTML reports via BenchmarkDotNet.
              A consolidated JSON report and index.json are written to bench/data/
              for the web-based benchmark viewer (bench/index.html).

            Examples:
              # Run all suites
              dotnet run -c Release -- --suite all

              # Run only the query suite
              dotnet run -c Release -- --suite query

              # Run with BenchmarkDotNet filter
              dotnet run -c Release -- --suite query --filter "*Lean*"

            Script wrapper:
              .\scripts\benchmark.ps1 --suite all
              .\scripts\benchmark.ps1 --suite query
              .\scripts\benchmark.ps1 --help
            """);
    }

    private static (BenchmarkSuite Suite, string[] BenchmarkArgs, bool ShowHelp, int? DocCount) ParseArguments(string[] args)
    {
        var suite = BenchmarkSuite.All;
        var benchmarkArgs = new List<string>(args.Length);
        var showHelp = false;
        int? docCount = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[i], "-h", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                continue;
            }

            if (string.Equals(args[i], "--suite", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                suite = ParseSuite(args[++i]);
                continue;
            }

            if (string.Equals(args[i], "--doccount", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dc))
                    docCount = dc;
                continue;
            }

            benchmarkArgs.Add(args[i]);
        }

        return (suite, [.. benchmarkArgs], showHelp, docCount);
    }

    private static BenchmarkSuite ParseSuite(string value)
    {
        if (value.Equals("index", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Index;
        if (value.Equals("query", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Query;
        if (value.Equals("analysis", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Analysis;
        if (value.Equals("boolean", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Boolean;
        if (value.Equals("phrase", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Phrase;
        if (value.Equals("smallindex", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.SmallIndex;
        if (value.Equals("prefix", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Prefix;
        if (value.Equals("fuzzy", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Fuzzy;
        if (value.Equals("wildcard", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Wildcard;
        if (value.Equals("deletion", StringComparison.OrdinalIgnoreCase))
            return BenchmarkSuite.Deletion;

        return BenchmarkSuite.All;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "Rowles.LeanLucene.slnx");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private enum BenchmarkSuite
    {
        All,
        Index,
        Query,
        Analysis,
        Boolean,
        Phrase,
        SmallIndex,
        Prefix,
        Fuzzy,
        Wildcard,
        Deletion
    }
}
