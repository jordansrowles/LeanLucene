using System.Text.RegularExpressions;

namespace Rowles.LeanLucene.Benchmarks;

/// <summary>
/// Provides a unified pool of real-world document bodies sourced from
/// bench/data/ (Gutenberg ebooks, 20 Newsgroups, Reuters-21578).
/// Falls back to synthetic content when no real data is present.
/// </summary>
/// <remarks>
/// Span/ReadOnlyMemory can reduce intermediate copies during the parsing phase,
/// but since IndexWriter requires string values, slices must ultimately be
/// materialised via ToString() -- same heap footprint. The practical limit is
/// capping per-source document counts to avoid loading all 18K newsgroup files.
/// </remarks>
internal static class RealDataPool
{
    private static readonly Lazy<string[]> Pool =
        new(LoadAll, LazyThreadSafetyMode.ExecutionAndPublication);

    private const int MinBodyLength = 40;

    /// <summary>Maximum documents loaded from the 20 Newsgroups source.</summary>
    private const int MaxNewsgroups = 15_000;

    /// <summary>Maximum documents loaded from the Reuters source.</summary>
    private const int MaxReuters = 15_000;

    /// <summary>
    /// Returns <paramref name="count"/> document bodies from the real-data pool,
    /// wrapping round-robin when count exceeds the pool size.
    /// </summary>
    public static string[] GetBodies(int count)
    {
        var pool = Pool.Value;
        if (pool.Length == 0)
            return BuildSynthetic(count);

        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = pool[i % pool.Length];
        return result;
    }

    private static string[] LoadAll()
    {
        var root = GutenbergDataLoader.FindRepositoryRoot();
        var benchDir = Path.Combine(root, "bench", "data");

        if (!Directory.Exists(benchDir))
        {
            Console.Error.WriteLine(
                $"[RealDataPool] bench/data not found at '{benchDir}'. " +
                "Run the download scripts first. Falling back to synthetic data.");
            return [];
        }

        var bodies = new List<string>(60_000);

        LoadGutenberg(benchDir, bodies);
        Load20Newsgroups(benchDir, bodies);
        LoadReuters(benchDir, bodies);

        if (bodies.Count == 0)
        {
            Console.Error.WriteLine(
                "[RealDataPool] No documents loaded from bench/data. " +
                "Run the download scripts first. Falling back to synthetic data.");
            return [];
        }

        Console.Error.WriteLine($"[RealDataPool] Loaded {bodies.Count:N0} real-data documents.");
        return [.. bodies];
    }

    private static void LoadGutenberg(string benchDir, List<string> bodies)
    {
        var dir = Path.Combine(benchDir, "gutenberg-ebooks");
        if (!Directory.Exists(dir))
            return;

        try
        {
            // Reuse existing loader; extract paragraph bodies
            var paragraphs = GutenbergDataLoader.Load(dir);
            foreach (var p in paragraphs)
                bodies.Add(p.Body);
        }
        catch (FileNotFoundException)
        {
            // No .txt files found - skip silently
        }
    }

    private static void Load20Newsgroups(string benchDir, List<string> bodies)
    {
        var dir = Path.Combine(benchDir, "20newsgroups");
        if (!Directory.Exists(dir))
            return;

        var count = 0;
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                     .Where(f => IsNumericFilename(f))
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            if (count >= MaxNewsgroups)
                break;

            var body = ExtractNewsBody(file);
            if (body.Length >= MinBodyLength)
            {
                bodies.Add(body);
                count++;
            }
        }
    }

    private static void LoadReuters(string benchDir, List<string> bodies)
    {
        var dir = Path.Combine(benchDir, "reuters21578");
        if (!Directory.Exists(dir))
            return;

        var count = 0;
        foreach (var file in Directory.GetFiles(dir, "*.sgm", SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            if (count >= MaxReuters)
                break;

            var raw = File.ReadAllText(file, System.Text.Encoding.Latin1);
            foreach (Match m in ReutersBodyPattern.Matches(raw))
            {
                if (count >= MaxReuters)
                    break;
                var body = m.Groups[1].Value.Trim();
                if (body.Length >= MinBodyLength)
                {
                    bodies.Add(body);
                    count++;
                }
            }
        }
    }

    // Non-greedy match of <BODY>...</BODY> in Reuters SGML
    private static readonly Regex ReutersBodyPattern = new(
        @"<BODY>(.*?)</BODY>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(30));

    private static bool IsNumericFilename(string path)
    {
        var name = Path.GetFileName(path);
        return name.Length > 0 && name.All(char.IsDigit);
    }

    private static string ExtractNewsBody(string filePath)
    {
        try
        {
            // Usenet/email format: RFC 2822 headers end at first blank line
            var lines = File.ReadAllLines(filePath, System.Text.Encoding.Latin1);
            var bodyStart = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    bodyStart = i + 1;
                    break;
                }
            }

            if (bodyStart >= lines.Length)
                return string.Empty;

            return string.Join(" ", lines[bodyStart..]).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Synthetic fallback used when bench/data is absent or empty.</summary>
    private static string[] BuildSynthetic(int count)
    {
        var topics  = new[] { "government", "economics", "politics", "science", "technology" };
        var domains = new[] { "national", "international", "regional", "local" };
        var docs = new string[count];
        for (int i = 0; i < count; i++)
        {
            var kw = (i % 3) switch { 0 => "said", 1 => "people", _ => "market" };
            docs[i] = $"doc {i} {kw} {topics[i % topics.Length]} {domains[(i * 7) % domains.Length]} " +
                      "president company reported financial political economic national government";
        }
        return docs;
    }
}
