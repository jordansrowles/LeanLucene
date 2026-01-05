using BenchmarkDotNet.Reports;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Rowles.LeanLucene.Example.Benchmarks;

internal sealed class BenchmarkRunReport
{
    public int SchemaVersion { get; set; } = 1;
    public string RunId { get; set; } = string.Empty;
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string[] CommandLineArgs { get; set; } = [];
    public string HostMachineName { get; set; } = Environment.MachineName;
    public string CommitHash { get; set; } = string.Empty;
    public string DotnetVersion { get; set; } = Environment.Version.ToString();
    public int TotalBenchmarkCount { get; set; }
    public List<BenchmarkSuiteReport> Suites { get; set; } = [];
}

internal sealed class BenchmarkSuiteReport
{
    public string SuiteName { get; set; } = string.Empty;
    public string SummaryTitle { get; set; } = string.Empty;
    public int BenchmarkCount { get; set; }
    public List<BenchmarkCaseReport> Benchmarks { get; set; } = [];
}

internal sealed class BenchmarkCaseReport
{
    public string Key { get; set; } = string.Empty;
    public string DisplayInfo { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.Ordinal);
    public BenchmarkStatisticsReport? Statistics { get; set; }
    public BenchmarkGcReport? Gc { get; set; }
}

internal sealed class BenchmarkStatisticsReport
{
    public int? SampleCount { get; set; }
    public double? MeanNanoseconds { get; set; }
    public double? MedianNanoseconds { get; set; }
    public double? MinNanoseconds { get; set; }
    public double? MaxNanoseconds { get; set; }
    public double? StandardDeviationNanoseconds { get; set; }
    public double? OperationsPerSecond { get; set; }
}

internal sealed class BenchmarkGcReport
{
    public double? BytesAllocatedPerOperation { get; set; }
    public double? Gen0Collections { get; set; }
    public double? Gen1Collections { get; set; }
    public double? Gen2Collections { get; set; }
}

internal sealed class BenchmarkRunIndex
{
    public int SchemaVersion { get; set; } = 1;
    public List<BenchmarkRunIndexEntry> Runs { get; set; } = [];
}

internal sealed class BenchmarkRunIndexEntry
{
    public string RunId { get; set; } = string.Empty;
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int BenchmarkCount { get; set; }
    public string[] Suites { get; set; } = [];
}

internal static class BenchmarkRunReportBuilder
{
    public static BenchmarkRunReport Build(
        string runId,
        DateTimeOffset generatedAtUtc,
        string[] benchmarkArgs,
        IReadOnlyList<(string Suite, Summary Summary)> suiteSummaries)
    {
        var suiteReports = suiteSummaries
            .Select(BuildSuiteReport)
            .OrderBy(r => r.SuiteName, StringComparer.Ordinal)
            .ToList();

        return new BenchmarkRunReport
        {
            RunId = runId,
            GeneratedAtUtc = generatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            CommandLineArgs = benchmarkArgs,
            TotalBenchmarkCount = suiteReports.Sum(r => r.BenchmarkCount),
            Suites = suiteReports
        };
    }

    private static BenchmarkSuiteReport BuildSuiteReport((string Suite, Summary Summary) source)
    {
        var benchmarks = source.Summary.Reports
            .Select(BuildCaseReport)
            .OrderBy(r => r.Key, StringComparer.Ordinal)
            .ToList();

        return new BenchmarkSuiteReport
        {
            SuiteName = source.Suite,
            SummaryTitle = source.Summary.Title,
            BenchmarkCount = benchmarks.Count,
            Benchmarks = benchmarks
        };
    }

    private static BenchmarkCaseReport BuildCaseReport(BenchmarkReport report)
    {
        var benchmarkCase = report.BenchmarkCase;
        var descriptor = benchmarkCase.Descriptor;
        var parameters = benchmarkCase.Parameters.Items
            .ToDictionary(
                item => item.Name,
                item => item.Value?.ToString() ?? string.Empty,
                StringComparer.Ordinal);

        var key = BuildBenchmarkKey(descriptor.Type.Name, descriptor.WorkloadMethod.Name, parameters);

        return new BenchmarkCaseReport
        {
            Key = key,
            DisplayInfo = benchmarkCase.DisplayInfo,
            TypeName = descriptor.Type.Name,
            MethodName = descriptor.WorkloadMethod.Name,
            Parameters = parameters,
            Statistics = BuildStatisticsReport(report.ResultStatistics),
            Gc = BuildGcReport(report.GcStats)
        };
    }

    private static string BuildBenchmarkKey(
        string typeName,
        string methodName,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return $"{typeName}.{methodName}";
        }

        var parameterParts = parameters
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => $"{kvp.Key}={kvp.Value}");

        return $"{typeName}.{methodName}|{string.Join(", ", parameterParts)}";
    }

    private static BenchmarkStatisticsReport? BuildStatisticsReport(object? statistics)
    {
        if (statistics is null)
        {
            return null;
        }

        var mean = ReadDoubleProperty(statistics, "Mean");

        return new BenchmarkStatisticsReport
        {
            SampleCount = ReadIntProperty(statistics, "N"),
            MeanNanoseconds = mean,
            MedianNanoseconds = ReadDoubleProperty(statistics, "Median"),
            MinNanoseconds = ReadDoubleProperty(statistics, "Min"),
            MaxNanoseconds = ReadDoubleProperty(statistics, "Max"),
            StandardDeviationNanoseconds = ReadDoubleProperty(statistics, "StandardDeviation"),
            OperationsPerSecond = mean is > 0 ? 1_000_000_000d / mean.Value : null
        };
    }

    private static BenchmarkGcReport? BuildGcReport(object? gcStats)
    {
        if (gcStats is null)
        {
            return null;
        }

        return new BenchmarkGcReport
        {
            BytesAllocatedPerOperation = ReadDoubleProperty(gcStats, "BytesAllocatedPerOperation"),
            Gen0Collections = ReadDoubleProperty(gcStats, "Gen0Collections"),
            Gen1Collections = ReadDoubleProperty(gcStats, "Gen1Collections"),
            Gen2Collections = ReadDoubleProperty(gcStats, "Gen2Collections")
        };
    }

    private static double? ReadDoubleProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        if (value is null)
        {
            return null;
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static int? ReadIntProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        if (value is null)
        {
            return null;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }
}

internal static class BenchmarkRunReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteReport(string dataDirectory, BenchmarkRunReport report)
    {
        Directory.CreateDirectory(dataDirectory);

        var reportFileName = $"{report.RunId}.json";
        var reportPath = Path.Combine(dataDirectory, reportFileName);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));

        var indexPath = Path.Combine(dataDirectory, "index.json");
        var runIndex = File.Exists(indexPath)
            ? JsonSerializer.Deserialize<BenchmarkRunIndex>(File.ReadAllText(indexPath), JsonOptions) ?? new BenchmarkRunIndex()
            : new BenchmarkRunIndex();

        runIndex.Runs.RemoveAll(entry => string.Equals(entry.RunId, report.RunId, StringComparison.Ordinal));

        runIndex.Runs.Add(new BenchmarkRunIndexEntry
        {
            RunId = report.RunId,
            GeneratedAtUtc = report.GeneratedAtUtc,
            CommitHash = report.CommitHash,
            File = reportFileName,
            BenchmarkCount = report.TotalBenchmarkCount,
            Suites = report.Suites.Select(s => s.SuiteName).ToArray()
        });

        runIndex.Runs = runIndex.Runs
            .OrderByDescending(entry => entry.GeneratedAtUtc, StringComparer.Ordinal)
            .ToList();

        File.WriteAllText(indexPath, JsonSerializer.Serialize(runIndex, JsonOptions));
    }
}
