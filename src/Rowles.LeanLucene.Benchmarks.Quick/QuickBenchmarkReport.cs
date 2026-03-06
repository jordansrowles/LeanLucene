namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Full report for a quick benchmark run, including environment metadata
/// and all individual benchmark results.
/// </summary>
internal sealed class QuickBenchmarkReport
{
    public int SchemaVersion { get; init; } = 1;
    public required string RunId { get; init; }
    public required string GeneratedAtUtc { get; init; }
    public required EnvironmentInfo Environment { get; init; }
    public required List<QuickBenchmarkResult> Results { get; init; }
}

/// <summary>
/// Captures the environment in which the benchmark run executed.
/// </summary>
internal sealed class EnvironmentInfo
{
    public required string MachineName { get; init; }
    public required string OsDescription { get; init; }
    public required int ProcessorCount { get; init; }
    public required string DotNetVersion { get; init; }
    public required string RuntimeIdentifier { get; init; }
    public required string Architecture { get; init; }
    public required bool IsServerGc { get; init; }
    public required bool IsConcurrentGc { get; init; }
    public required string CommitHash { get; init; }
    public required bool IsReleaseBuild { get; init; }
    public required long TimestampFrequency { get; init; }
}

internal static class QuickBenchmarkReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Writes the report as JSON to the specified directory, returning the file path.
    /// </summary>
    public static string WriteReport(string outputDirectory, QuickBenchmarkReport report)
    {
        Directory.CreateDirectory(outputDirectory);

        var fileName = $"{report.RunId}.json";
        var filePath = Path.Combine(outputDirectory, fileName);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(filePath, json);

        return filePath;
    }

    /// <summary>
    /// Captures current environment metadata.
    /// </summary>
    public static EnvironmentInfo CaptureEnvironment(string commitHash)
    {
        return new EnvironmentInfo
        {
            MachineName = System.Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            ProcessorCount = System.Environment.ProcessorCount,
            DotNetVersion = System.Environment.Version.ToString(),
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            IsServerGc = GCSettings.IsServerGC,
            IsConcurrentGc = GCSettings.LatencyMode != GCLatencyMode.Batch,
            CommitHash = commitHash,
            IsReleaseBuild = IsRelease(),
            TimestampFrequency = Stopwatch.Frequency
        };
    }

    private static bool IsRelease()
    {
#if DEBUG
        return false;
#else
        return true;
#endif
    }
}
