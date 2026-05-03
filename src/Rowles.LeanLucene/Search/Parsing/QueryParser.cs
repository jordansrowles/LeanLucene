using Rowles.LeanLucene.Analysis.Analysers;
namespace Rowles.LeanLucene.Search.Parsing;

/// <summary>
/// Parses a query string into a Query object tree.
/// Supports: term, field:term, "phrase", +required, -excluded, (grouping),
/// prefix*, wild?card, fuzzy~N, "phrase"~N, field:term^boost.
/// </summary>
public sealed class QueryParser
{
    private readonly string _defaultField;
    private readonly IAnalyser _analyser;

    /// <summary>Initialises a new <see cref="QueryParser"/> with the given default field and analyser.</summary>
    /// <param name="defaultField">The field used when no explicit <c>field:</c> prefix is present in the query string.</param>
    /// <param name="analyser">The analyser used to tokenise terms and phrases at query time.</param>
    public QueryParser(string defaultField, IAnalyser analyser)
    {
        _defaultField = defaultField;
        _analyser = analyser;
    }

    /// <summary>Parses the query string into a <see cref="Query"/> object tree.</summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>
    /// A <see cref="Query"/> representing the parsed expression, or an empty
    /// <see cref="BooleanQuery"/> when <paramref name="queryString"/> is null or whitespace.
    /// </returns>
    public Query Parse(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return new BooleanQuery();

        var tokens = Tokenize(queryString);
        int pos = 0;
        var query = ParseExpression(tokens, ref pos);
        if (pos < tokens.Count)
            throw new QueryParseException($"Unexpected token '{tokens[pos].Value}' at position {pos}.");
        return query;
    }

    private Query ParseExpression(List<QToken> tokens, ref int pos)
    {
        var clauses = new List<BooleanClause>();

        while (pos < tokens.Count)
        {
            if (tokens[pos].Type == QTokenType.RParen)
                break;

            var occur = Occur.Should;
            if (tokens[pos].Type == QTokenType.Plus)
            {
                occur = Occur.Must;
                pos++;
            }
            else if (tokens[pos].Type == QTokenType.Minus)
            {
                occur = Occur.MustNot;
                pos++;
            }

            if (pos >= tokens.Count)
                throw new QueryParseException("A required or prohibited operator must be followed by a query clause.");

            var subQuery = ParseClause(tokens, ref pos);
            if (subQuery is not null)
                clauses.Add(new BooleanClause(subQuery, occur));
        }

        if (clauses.Count == 1 && clauses[0].Occur == Occur.Should)
            return clauses[0].Query;

        var boolQuery = new BooleanQuery();
        foreach (var c in clauses)
            boolQuery.Add(c.Query, c.Occur);
        return boolQuery;
    }

    private Query? ParseClause(List<QToken> tokens, ref int pos)
    {
        if (pos >= tokens.Count) return null;

        // Parenthetical grouping
        if (tokens[pos].Type == QTokenType.LParen)
        {
            pos++; // consume '('
            var inner = ParseExpression(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos].Type == QTokenType.RParen)
                pos++; // consume ')'
            else
                throw new QueryParseException("Unmatched opening parenthesis.");
            return ApplyBoost(inner, tokens, ref pos);
        }

        // Quoted phrase
        if (tokens[pos].Type == QTokenType.Phrase)
        {
            var phrase = tokens[pos].Value;
            pos++;
            string field = _defaultField;

            var query = BuildPhraseQuery(field, phrase);
            query = ApplySlop(query, tokens, ref pos);
            return ApplyBoost(query, tokens, ref pos);
        }

        // Term (possibly with field: prefix)
        if (tokens[pos].Type == QTokenType.Term)
        {
            string field = _defaultField;
            string term = tokens[pos].Value;
            pos++;

            // Check for field:value
            if (pos < tokens.Count && tokens[pos].Type == QTokenType.Colon)
            {
                pos++; // consume ':'
                field = term;

                if (pos < tokens.Count)
                {
                    if (tokens[pos].Type == QTokenType.Phrase)
                    {
                        var phrase = tokens[pos].Value;
                        pos++;
                        var pq = BuildPhraseQuery(field, phrase);
                        pq = ApplySlop(pq, tokens, ref pos);
                        return ApplyBoost(pq, tokens, ref pos);
                    }
                    else if (tokens[pos].Type == QTokenType.Term)
                    {
                        term = tokens[pos].Value;
                        pos++;
                    }
                    else
                    {
                        throw new QueryParseException($"Field '{field}' must be followed by a term or phrase.");
                    }
                }
                else
                {
                    throw new QueryParseException($"Field '{field}' must be followed by a term or phrase.");
                }
            }

            // Check for wildcard/prefix/fuzzy suffixes
            if (term.Contains('*') || term.Contains('?'))
            {
                if (term.EndsWith('*') && !term.AsSpan()[..^1].Contains('*') && !term.AsSpan()[..^1].Contains('?'))
                {
                    var q = new PrefixQuery(field, term[..^1]);
                    return ApplyBoost(q, tokens, ref pos);
                }
                var wq = new WildcardQuery(field, term);
                return ApplyBoost(wq, tokens, ref pos);
            }

            // Check for fuzzy ~ suffix
            if (pos < tokens.Count && tokens[pos].Type == QTokenType.Tilde)
            {
                pos++;
                int maxEdits = 2;
                if (pos < tokens.Count && tokens[pos].Type == QTokenType.Term &&
                    int.TryParse(tokens[pos].Value, out int edits))
                {
                    maxEdits = edits;
                    pos++;
                }
                var analysed = AnalyseTerm(term);
                var fq = new FuzzyQuery(field, analysed, maxEdits);
                return ApplyBoost(fq, tokens, ref pos);
            }

            // Regular term — analyse it
            var analysedTerm = AnalyseTerm(term);
            if (string.IsNullOrEmpty(analysedTerm))
                return null; // stop word removed

            var tq = new TermQuery(field, analysedTerm);
            return ApplyBoost(tq, tokens, ref pos);
        }

        throw new QueryParseException($"Unexpected token '{tokens[pos].Value}' at position {pos}.");
    }

    private PhraseQuery BuildPhraseQuery(string field, string phraseText)
    {
        var analysedTokens = _analyser.Analyse(phraseText.AsSpan());
        var terms = analysedTokens.Select(t => t.Text).ToArray();
        return terms.Length > 0 ? new PhraseQuery(field, terms) : new PhraseQuery(field, phraseText.Split(' '));
    }

    private static PhraseQuery ApplySlop(PhraseQuery query, List<QToken> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == QTokenType.Tilde)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == QTokenType.Term &&
                int.TryParse(tokens[pos].Value, out int slop))
            {
                query.Slop = slop;
                pos++;
            }
        }
        return query;
    }

    private static Query ApplyBoost(Query query, List<QToken> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == QTokenType.Caret)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == QTokenType.Term &&
                float.TryParse(tokens[pos].Value, System.Globalization.CultureInfo.InvariantCulture, out float boost))
            {
                query.Boost = boost;
                pos++;
            }
        }
        return query;
    }

    private string AnalyseTerm(string term)
    {
        var tokens = _analyser.Analyse(term.AsSpan());
        return tokens.Count > 0 ? tokens[0].Text : string.Empty;
    }

    private static List<QToken> Tokenize(string input)
    {
        var tokens = new List<QToken>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            switch (c)
            {
                case '+': tokens.Add(new QToken(QTokenType.Plus, "+")); i++; continue;
                case '-': tokens.Add(new QToken(QTokenType.Minus, "-")); i++; continue;
                case '(': tokens.Add(new QToken(QTokenType.LParen, "(")); i++; continue;
                case ')': tokens.Add(new QToken(QTokenType.RParen, ")")); i++; continue;
                case ':': tokens.Add(new QToken(QTokenType.Colon, ":")); i++; continue;
                case '~': tokens.Add(new QToken(QTokenType.Tilde, "~")); i++; continue;
                case '^': tokens.Add(new QToken(QTokenType.Caret, "^")); i++; continue;
            }

            if (c == '"')
            {
                i++; // skip opening quote
                int start = i;
                while (i < input.Length && input[i] != '"')
                    i++;
                if (i >= input.Length)
                    throw new QueryParseException("Unmatched quote in query string.");
                tokens.Add(new QToken(QTokenType.Phrase, input[start..i]));
                i++; // skip closing quote
                continue;
            }

            // Regular term
            {
                int start = i;
                while (i < input.Length && !char.IsWhiteSpace(input[i]) &&
                       input[i] != '(' && input[i] != ')' && input[i] != ':' &&
                       input[i] != '"' && input[i] != '~' && input[i] != '^')
                {
                    i++;
                }
                tokens.Add(new QToken(QTokenType.Term, input[start..i]));
            }
        }

        return tokens;
    }

    private enum QTokenType { Term, Phrase, Plus, Minus, LParen, RParen, Colon, Tilde, Caret }

    private readonly record struct QToken(QTokenType Type, string Value);
}

/// <summary>Exception thrown when a query string cannot be parsed.</summary>
public sealed class QueryParseException : FormatException
{
    /// <summary>Initialises a new <see cref="QueryParseException"/> with the supplied message.</summary>
    /// <param name="message">Description of the parse error.</param>
    public QueryParseException(string message) : base(message)
    {
    }
}
