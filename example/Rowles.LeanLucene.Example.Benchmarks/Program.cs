using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Globalization;

namespace Rowles.LeanLucene.Example.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        var (suite, benchmarkArgs) = ParseArguments(args);
        var repoRoot = FindRepositoryRoot();
        var runRoot = Path.Combine(
            repoRoot,
            "bench",
            DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture));

        Directory.CreateDirectory(runRoot);

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Query)
        {
            var queryArtifactsPath = Path.Combine(runRoot, "query");
            BenchmarkRunner.Run<TermQueryBenchmarks>(BuildConfig(queryArtifactsPath), benchmarkArgs);
        }

        if (suite is BenchmarkSuite.All or BenchmarkSuite.Index)
        {
            var indexArtifactsPath = Path.Combine(runRoot, "index");
            BenchmarkRunner.Run<IndexingBenchmarks>(BuildConfig(indexArtifactsPath), benchmarkArgs);
        }

        Console.WriteLine($"Benchmark artifacts written to: {runRoot}");
    }

    private static IConfig BuildConfig(string artifactsPath)
    {
        Directory.CreateDirectory(artifactsPath);
        return DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
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
