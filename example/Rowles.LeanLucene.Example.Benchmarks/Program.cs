using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Globalization;

namespace Rowles.LeanLucene.Example.Benchmarks;

internal static class Program
{
    public static int Main(string[] args)
    {
        var (suite, benchmarkArgs) = ParseArguments(args);
        var repoRoot = FindRepositoryRoot();
        var benchRoot = Path.Combine(repoRoot, "bench");
        var dataDirectory = Path.Combine(benchRoot, "data");
        Directory.CreateDirectory(dataDirectory);

        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var suiteArtifacts = new List<(string Suite, string ArtifactsPath)>();

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Query)
        {
            var queryArtifactsPath = BuildArtifactsPath(dataDirectory, runId, "query");
            BenchmarkRunner.Run<TermQueryBenchmarks>(BuildConfig(queryArtifactsPath), benchmarkArgs);
            suiteArtifacts.Add(("query", queryArtifactsPath));
        }

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Index)
        {
            var indexArtifactsPath = BuildArtifactsPath(dataDirectory, runId, "index");
            BenchmarkRunner.Run<IndexingBenchmarks>(BuildConfig(indexArtifactsPath), benchmarkArgs);
            suiteArtifacts.Add(("index", indexArtifactsPath));
        }

        if (suiteArtifacts.Count == 0)
        {
            Console.Error.WriteLine("No benchmark suite selected.");
            return 1;
        }

        foreach (var (suiteName, artifactsPath) in suiteArtifacts)
        {
            Console.WriteLine($"[{suiteName}] Artifacts: {artifactsPath}");
        }

        Console.WriteLine("Use BenchmarkDotNet HtmlExporter and JsonExporterAttribute.Full outputs from these folders.");
        return 0;
    }

    private static IConfig BuildConfig(string artifactsPath)
    {
        Directory.CreateDirectory(artifactsPath);
        return DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
    }

    private static string BuildArtifactsPath(string dataDirectory, string runId, string suiteName)
    {
        return Path.Combine(dataDirectory, runId, suiteName);
    }

    private static (BenchmarkSuite Suite, string[] BenchmarkArgs) ParseArguments(string[] args)
    {
        var suite = BenchmarkSuite.All;
        var benchmarkArgs = new List<string>(args.Length);

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--suite", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                suite = ParseSuite(args[++i]);
                continue;
            }

            benchmarkArgs.Add(args[i]);
        }

        return (suite, [.. benchmarkArgs]);
    }

    private static BenchmarkSuite ParseSuite(string value)
    {
        if (value.Equals("index", StringComparison.OrdinalIgnoreCase))
        {
            return BenchmarkSuite.Index;
        }

        if (value.Equals("query", StringComparison.OrdinalIgnoreCase))
        {
            return BenchmarkSuite.Query;
        }

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
        Query
    }
}
