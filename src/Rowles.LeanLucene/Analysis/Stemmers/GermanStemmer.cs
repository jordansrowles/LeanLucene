namespace Rowles.LeanLucene.Analysis.Stemmers;

/// <summary>
/// German Snowball-inspired stemmer. Handles common German suffixes.
/// </summary>
public sealed class GermanStemmer : IStemmer
{
    public string Stem(string word)
    {
        if (word.Length <= 3) return word;

        Span<char> buf = word.Length <= 64
            ? stackalloc char[word.Length]
            : new char[word.Length];
        word.AsSpan().CopyTo(buf);
        int len = buf.Length;

        // Replace umlauts
        for (int i = 0; i < len; i++)
        {
            buf[i] = buf[i] switch
            {
                'ä' => 'a',
                'ö' => 'o',
                'ü' => 'u',
                _ => buf[i]
            };
        }

        // Remove common suffixes
        len = RemoveSuffix(buf, len, "ungen", "")
           ?? RemoveSuffix(buf, len, "ung", "")
           ?? RemoveSuffix(buf, len, "heit", "")
           ?? RemoveSuffix(buf, len, "keit", "")
           ?? RemoveSuffix(buf, len, "lich", "")
           ?? RemoveSuffix(buf, len, "isch", "")
           ?? RemoveSuffix(buf, len, "ern", "")
           ?? RemoveSuffix(buf, len, "em", "")
           ?? RemoveSuffix(buf, len, "en", "")
           ?? RemoveSuffix(buf, len, "er", "")
           ?? RemoveSuffix(buf, len, "es", "")
           ?? RemoveSuffix(buf, len, "ig", "")
           ?? len;

        // Remove trailing 'e' if stem > 3 chars
        if (len > 3 && buf[len - 1] == 'e')
            len--;

        var result = buf[..len];
        return result.SequenceEqual(word.AsSpan()) ? word : new string(result);
    }

    private static int? RemoveSuffix(Span<char> buf, int len, ReadOnlySpan<char> suffix, ReadOnlySpan<char> replacement)
    {
        if (len < suffix.Length + 3) return null;
        if (!buf.Slice(len - suffix.Length, suffix.Length).SequenceEqual(suffix)) return null;
        int stemLen = len - suffix.Length;
        if (replacement.Length > 0)
        {
            replacement.CopyTo(buf[stemLen..]);
            return stemLen + replacement.Length;
        }
        return stemLen;
    }
}
