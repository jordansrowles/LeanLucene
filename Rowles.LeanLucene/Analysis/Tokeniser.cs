namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Slices input text into tokens at word boundaries, splitting on
/// whitespace and punctuation whilst tracking character offsets.
/// </summary>
public sealed class Tokeniser : ITokeniser
{
    public List<Token> Tokenise(ReadOnlySpan<char> input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            // Skip non-letter/digit characters (whitespace, punctuation, etc.)
            if (!char.IsLetterOrDigit(input[i]))
            {
                i++;
                continue;
            }

            // Start of a token
            int start = i;
            while (i < input.Length && char.IsLetterOrDigit(input[i]))
            {
                i++;
            }

            tokens.Add(new Token(input[start..i].ToString(), start, i));
        }

        return tokens;
    }
}
