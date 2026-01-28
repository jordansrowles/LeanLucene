namespace Rowles.LeanLucene.Search;

/// <summary>
/// Computes Levenshtein edit distance between two strings.
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
}
