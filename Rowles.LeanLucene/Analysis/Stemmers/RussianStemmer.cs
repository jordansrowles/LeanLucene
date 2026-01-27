namespace Rowles.LeanLucene.Analysis.Stemmers;

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
