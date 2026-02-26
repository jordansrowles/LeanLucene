using System.Diagnostics;
using System.Runtime;

namespace Rowles.LeanLucene.DevBench;

/// <summary>
/// Lightweight micro-benchmark harness for fast iteration during development.
/// Uses Stopwatch + GC.GetAllocatedBytesForCurrentThread() — no BenchmarkDotNet overhead.
/// </summary>
public sealed class MicroBench
{
    public record struct Result(
        string Name,
        int Iterations,
        double MeanUs,
        double P50Us,
        double P99Us,
        long AllocatedBytes,
        int Gen0,
        int Gen1,
        int Gen2);

    /// <summary>
    /// Measures an action with warmup, then runs <paramref name="iterations"/> timed iterations.
    /// Reports mean, p50, p99 (in microseconds), allocated bytes, and GC counts.
    /// </summary>
    public static Result Measure(string name, int warmup, int iterations, Action action)
    {
        // Warmup
        for (int i = 0; i < warmup; i++)
            action();

        // Force GC to get a clean baseline
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();

        var timings = new double[iterations];
        var sw = new Stopwatch();

        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            action();
            sw.Stop();
            timings[i] = sw.Elapsed.TotalMicroseconds;
        }

        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        int gen0After = GC.CollectionCount(0);
        int gen1After = GC.CollectionCount(1);
        int gen2After = GC.CollectionCount(2);

        Array.Sort(timings);

        double mean = 0;
        for (int i = 0; i < timings.Length; i++)
            mean += timings[i];
        mean /= timings.Length;

        double p50 = timings[timings.Length / 2];
        double p99 = timings[(int)(timings.Length * 0.99)];

        return new Result(
            name,
            iterations,
            Math.Round(mean, 2),
            Math.Round(p50, 2),
            Math.Round(p99, 2),
            allocAfter - allocBefore,
            gen0After - gen0Before,
            gen1After - gen1Before,
            gen2After - gen2Before);
    }

    /// <summary>
    /// Measures an action that returns a value (prevents dead-code elimination).
    /// </summary>
    public static Result Measure<T>(string name, int warmup, int iterations, Func<T> action)
    {
        T? sink = default;
        return Measure(name, warmup, iterations, () => { sink = action(); });
    }

    /// <summary>
    /// Prints a result to the console in a compact table format.
    /// </summary>
    public static void Print(Result r)
    {
        Console.WriteLine($"  {r.Name,-40} | {FormatUs(r.MeanUs),10} mean | {FormatUs(r.P50Us),10} p50 | {FormatUs(r.P99Us),10} p99 | {FormatBytes(r.AllocatedBytes),10} alloc | GC {r.Gen0}/{r.Gen1}/{r.Gen2}");
    }

    /// <summary>
    /// Prints a header for the benchmark output.
    /// </summary>
    public static void PrintHeader(string suiteName)
    {
        Console.WriteLine();
        Console.WriteLine($"═══ {suiteName} ═══");
        Console.WriteLine($"  {"Benchmark",-40} | {"Mean",10} | {"P50",10} | {"P99",10} | {"Alloc",10} | GC 0/1/2");
        Console.WriteLine($"  {new string('─', 40)} | {new string('─', 10)} | {new string('─', 10)} | {new string('─', 10)} | {new string('─', 10)} | {new string('─', 10)}");
    }

    /// <summary>
    /// Runs a suite of benchmarks, printing header and each result.
    /// </summary>
    public static List<Result> RunSuite(string suiteName, params (string name, int warmup, int iterations, Action action)[] benchmarks)
    {
        PrintHeader(suiteName);
        var results = new List<Result>(benchmarks.Length);
        foreach (var (name, warmup, iterations, action) in benchmarks)
        {
            var r = Measure(name, warmup, iterations, action);
            Print(r);
            results.Add(r);
        }
        return results;
    }

    private static string FormatUs(double us)
    {
        if (us < 1_000) return $"{us:F1} μs";
        if (us < 1_000_000) return $"{us / 1_000:F2} ms";
        return $"{us / 1_000_000:F2} s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
