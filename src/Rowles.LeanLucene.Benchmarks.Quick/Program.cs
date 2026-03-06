using Rowles.LeanLucene.Benchmarks.Quick;
using Rowles.LeanLucene.Benchmarks.Quick.Benchmarks;

var benchmarks = new IQuickBenchmark[]
{
    new AnalysisSanityBenchmark(),
    new IndexingSanityBenchmark(),
    new SearchSanityBenchmark(),
    new DidYouMeanSanityBenchmark(),
    new SpellIndexSanityBenchmark()
};

var commitHash = GetGitShortHash();
var timestamp = DateTimeOffset.UtcNow;
var runId = timestamp.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

Console.WriteLine("LeanLucene Quick Benchmark Harness");
Console.WriteLine(new string('=', 50));
Console.WriteLine($"  .NET:       {Environment.Version}");
Console.WriteLine($"  OS:         {RuntimeInformation.OSDescription}");
Console.WriteLine($"  CPUs:       {Environment.ProcessorCount}");
Console.WriteLine($"  Server GC:  {GCSettings.IsServerGC}");
Console.WriteLine($"  Commit:     {(string.IsNullOrEmpty(commitHash) ? "(unknown)" : commitHash)}");
Console.WriteLine($"  Timestamp:  {timestamp:O}");
#if DEBUG
Console.WriteLine();
Console.WriteLine("  WARNING: Running in DEBUG mode. Results are not representative.");
#endif
Console.WriteLine();

var results = new List<QuickBenchmarkResult>(benchmarks.Length);

foreach (var benchmark in benchmarks)
{
    Console.Write($"  Running {benchmark.Name}...");
    var result = QuickBenchmarkRunner.Run(benchmark);
    results.Add(result);
    Console.WriteLine($" {result.TimeMs.Mean:F2} ms (mean), {result.BytesAllocated.Mean:N0} bytes allocated (mean)");
}

// Build and write report.
var environment = QuickBenchmarkReportWriter.CaptureEnvironment(commitHash);
var report = new QuickBenchmarkReport
{
    RunId = runId,
    GeneratedAtUtc = timestamp.ToString("O", CultureInfo.InvariantCulture),
    Environment = environment,
    Results = results
};

var repoRoot = FindRepositoryRoot();
var outputDir = Path.Combine(repoRoot, "bench", "data", "quick");
var reportPath = QuickBenchmarkReportWriter.WriteReport(outputDir, report);

// Console summary table.
Console.WriteLine();
Console.WriteLine(new string('-', 105));
Console.WriteLine($"{"Benchmark",-35} {"Mean ms",10} {"Median ms",10} {"Min ms",10} {"Max ms",10} {"Alloc (bytes)",14}");
Console.WriteLine(new string('-', 105));

foreach (var r in results)
{
    Console.WriteLine(
        $"{r.BenchmarkName,-35} {r.TimeMs.Mean,10:F3} {r.TimeMs.Median,10:F3} {r.TimeMs.Min,10:F3} {r.TimeMs.Max,10:F3} {r.BytesAllocated.Mean,14:N0}");
}

Console.WriteLine(new string('-', 105));
Console.WriteLine();
Console.WriteLine($"Report written to: {reportPath}");

return 0;

// -- Helper methods --

static string GetGitShortHash()
{
    try
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "rev-parse --short HEAD",
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

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Rowles.LeanLucene.slnx")))
            return current.FullName;
        current = current.Parent;
    }
    return Directory.GetCurrentDirectory();
}
