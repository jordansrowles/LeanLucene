using System.Buffers;

namespace Rowles.LeanLucene.Analysis.Analysers;

/// <summary>
/// Analyser that splits text into letter-only tokens and lowercases them without stop-word removal.
/// </summary>
public sealed class SimpleAnalyser : IAnalyser
{
    private readonly LetterTokeniser _tokeniser = new();

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var tokens = _tokeniser.Tokenise(input);

        char[]? rented = null;
        try
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var span = token.Text.AsSpan();
                int length = span.Length;

                Span<char> buffer = GetRentedBuffer(ref rented, Math.Max(length, 64)).AsSpan(0, length);

                bool changed = false;
                for (int j = 0; j < length; j++)
                {
                    char lower = char.ToLowerInvariant(span[j]);
                    buffer[j] = lower;
                    changed |= lower != span[j];
                }

                if (changed)
                    tokens[i] = new Token(new string(buffer), token.StartOffset, token.EndOffset);
            }
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }

        return tokens;
    }

    private static char[] GetRentedBuffer(ref char[]? rented, int length)
    {
        if (rented is not null && rented.Length >= length)
            return rented;

        if (rented is not null)
            ArrayPool<char>.Shared.Return(rented);

        rented = ArrayPool<char>.Shared.Rent(length);
        return rented;
    }
}
