namespace Rowles.LeanLucene.Search;

/// <summary>
/// Computes Levenshtein edit distance between two strings or UTF-8 byte spans.
/// Uses a single-row DP approach with stackalloc for short strings.
/// </summary>
public static class LevenshteinDistance
{
    public static int Compute(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.IsEmpty) return b.Length;
        if (b.IsEmpty) return a.Length;

        // Ensure a is the shorter one to minimise buffer size
        if (a.Length > b.Length)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        int aLen = a.Length;
        int bLen = b.Length;

        Span<int> row = aLen + 1 <= 256
            ? stackalloc int[aLen + 1]
            : new int[aLen + 1];

        for (int i = 0; i <= aLen; i++)
            row[i] = i;

        for (int j = 1; j <= bLen; j++)
        {
            int prev = row[0];
            row[0] = j;

            for (int i = 1; i <= aLen; i++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                int current = Math.Min(
                    Math.Min(row[i] + 1, row[i - 1] + 1),
                    prev + cost);
                prev = row[i];
                row[i] = current;
            }
        }

        return row[aLen];
    }

    /// <summary>
    /// Computes Levenshtein edit distance on raw byte spans. Only valid for ASCII text
    /// where each byte is one character. Returns -1 if either span contains multi-byte
    /// UTF-8 sequences (high bit set).
    /// </summary>
    public static int ComputeAscii(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.IsEmpty) return b.Length;
        if (b.IsEmpty) return a.Length;

        if (a.Length > b.Length)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        int aLen = a.Length;
        int bLen = b.Length;

        Span<int> row = aLen + 1 <= 256
            ? stackalloc int[aLen + 1]
            : new int[aLen + 1];

        for (int i = 0; i <= aLen; i++)
            row[i] = i;

        for (int j = 1; j <= bLen; j++)
        {
            int prev = row[0];
            row[0] = j;
            byte bj = b[j - 1];
            if (bj >= 0x80) return -1;

            for (int i = 1; i <= aLen; i++)
            {
                byte ai = a[i - 1];
                if (ai >= 0x80) return -1;
                int cost = ai == bj ? 0 : 1;
                int current = Math.Min(
                    Math.Min(row[i] + 1, row[i - 1] + 1),
                    prev + cost);
                prev = row[i];
                row[i] = current;
            }
        }

        return row[aLen];
    }
}
