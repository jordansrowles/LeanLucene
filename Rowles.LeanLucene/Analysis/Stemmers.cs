namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// English stemmer wrapping the existing Porter stemmer implementation.
/// </summary>
public sealed class EnglishStemmer : IStemmer
{
    public string Stem(string word) => PorterStemmerFilter.Stem(word);
}

/// <summary>
/// French Snowball-inspired stemmer. Handles common French suffixes.
/// </summary>
public sealed class FrenchStemmer : IStemmer
{
    public string Stem(string word)
    {
        if (word.Length <= 3) return word;

        Span<char> buf = word.Length <= 64
            ? stackalloc char[word.Length]
            : new char[word.Length];
        word.AsSpan().CopyTo(buf);
        int len = buf.Length;

        // Step 1: Remove common suffixes
        len = RemoveSuffix(buf, len, "issements", "")
           ?? RemoveSuffix(buf, len, "issement", "")
           ?? RemoveSuffix(buf, len, "ements", "")
           ?? RemoveSuffix(buf, len, "ement", "")
           ?? RemoveSuffix(buf, len, "ations", "")
           ?? RemoveSuffix(buf, len, "ation", "")
           ?? RemoveSuffix(buf, len, "euses", "")
           ?? RemoveSuffix(buf, len, "euse", "")
           ?? RemoveSuffix(buf, len, "eurs", "")
           ?? RemoveSuffix(buf, len, "eur", "")
           ?? RemoveSuffix(buf, len, "ités", "")
           ?? RemoveSuffix(buf, len, "ité", "")
           ?? RemoveSuffix(buf, len, "ives", "")
           ?? RemoveSuffix(buf, len, "ive", "")
           ?? RemoveSuffix(buf, len, "ifs", "")
           ?? RemoveSuffix(buf, len, "if", "")
           ?? RemoveSuffix(buf, len, "aux", "al")
           ?? buf.Length;

        // Step 2: Verb endings
        len = RemoveSuffix(buf, len, "issent", "")
           ?? RemoveSuffix(buf, len, "issons", "")
           ?? RemoveSuffix(buf, len, "issez", "")
           ?? RemoveSuffix(buf, len, "irent", "")
           ?? RemoveSuffix(buf, len, "eront", "")
           ?? RemoveSuffix(buf, len, "erons", "")
           ?? RemoveSuffix(buf, len, "erez", "")
           ?? RemoveSuffix(buf, len, "ent", "")
           ?? RemoveSuffix(buf, len, "ons", "")
           ?? RemoveSuffix(buf, len, "ez", "")
           ?? RemoveSuffix(buf, len, "er", "")
           ?? RemoveSuffix(buf, len, "es", "")
           ?? len;

        // Remove trailing 'e' if stem > 2 chars
        if (len > 2 && buf[len - 1] == 'e')
            len--;

        var result = buf[..len];
        return result.SequenceEqual(word.AsSpan()) ? word : new string(result);
    }

    private static int? RemoveSuffix(Span<char> buf, int len, ReadOnlySpan<char> suffix, ReadOnlySpan<char> replacement)
    {
        if (len < suffix.Length + 2) return null;
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

/// <summary>
/// Russian stemmer. Handles common Russian suffixes (Cyrillic).
/// </summary>
public sealed class RussianStemmer : IStemmer
{
    public string Stem(string word)
    {
        if (word.Length <= 3) return word;

        Span<char> buf = word.Length <= 64
            ? stackalloc char[word.Length]
            : new char[word.Length];
        word.AsSpan().CopyTo(buf);
        int len = buf.Length;

        // Remove perfective gerund endings
        len = RemoveSuffix(buf, len, "ившись")
           ?? RemoveSuffix(buf, len, "ывшись")
           ?? RemoveSuffix(buf, len, "вшись")
           ?? RemoveSuffix(buf, len, "ивши")
           ?? RemoveSuffix(buf, len, "ывши")
           ?? RemoveSuffix(buf, len, "вши")
           ?? len;

        // Remove adjective/participle endings
        len = RemoveSuffix(buf, len, "ейшим")
           ?? RemoveSuffix(buf, len, "ейшая")
           ?? RemoveSuffix(buf, len, "ейшей")
           ?? RemoveSuffix(buf, len, "ейшие")
           ?? RemoveSuffix(buf, len, "ейший")
           ?? len;

        // Remove noun endings
        len = RemoveSuffix(buf, len, "ость")
           ?? RemoveSuffix(buf, len, "ями")
           ?? RemoveSuffix(buf, len, "ами")
           ?? RemoveSuffix(buf, len, "ей")
           ?? RemoveSuffix(buf, len, "ов")
           ?? RemoveSuffix(buf, len, "ом")
           ?? RemoveSuffix(buf, len, "ой")
           ?? RemoveSuffix(buf, len, "ии")
           ?? RemoveSuffix(buf, len, "ия")
           ?? RemoveSuffix(buf, len, "ий")
           ?? RemoveSuffix(buf, len, "ие")
           ?? len;

        // Remove verb endings
        len = RemoveSuffix(buf, len, "ать")
           ?? RemoveSuffix(buf, len, "ять")
           ?? RemoveSuffix(buf, len, "еть")
           ?? RemoveSuffix(buf, len, "ить")
           ?? RemoveSuffix(buf, len, "ут")
           ?? RemoveSuffix(buf, len, "ют")
           ?? RemoveSuffix(buf, len, "ет")
           ?? RemoveSuffix(buf, len, "ит")
           ?? len;

        var result = buf[..len];
        return result.SequenceEqual(word.AsSpan()) ? word : new string(result);
    }

    private static int? RemoveSuffix(Span<char> buf, int len, ReadOnlySpan<char> suffix)
    {
        if (len < suffix.Length + 2) return null;
        if (!buf.Slice(len - suffix.Length, suffix.Length).SequenceEqual(suffix)) return null;
        return len - suffix.Length;
    }
}
