using System.Text.RegularExpressions;

namespace PythonToCSharp
{
    public class Lexer
    {
        private static readonly List<(TokenType, string)> Tokens = new List<(TokenType, string)>()
        {
            // Identifiers
            (TokenType.Identifier, @"[a-zA-Z_][a-zA-Z0-9_]*"),
            // Integers
            (TokenType.Integer, @"\d+"),
            // Fixed-point numbers
            (TokenType.FloatFixed, @"\d+\.\d*|\.\d+"),
            // Float-point numbers
            (TokenType.FloatExp, @"\d+(\.\d*)?[eE][+-]?\d+"),
            // Strings
            (TokenType.String, @"""[^""]*""|'[^']*'"),
            // Indexed variables
            (TokenType.IndexedVar, @"[a-zA-Z_][a-zA-Z0-9_]*\[\s*[a-zA-Z0-9_+\-*/]+\s*\]"),
            // Comments
            (TokenType.Comment, @"#.*"),
            // Function calls
            (TokenType.FunctionCall, @"[a-zA-Z_][a-zA-Z0-9_]*\s*\([^()]*\)"),
            // Operators
            (TokenType.Operator, @"\+|\-|\*|/|\*\*|==|!=|<=|>=|<|>"),
            // Assignment operator (=)
            (TokenType.Assignment, @"="),
            // Punctuation marks
            (TokenType.Punctuation, @"[(),:;{}\[\]\.]"),
            // New line (\n)
            (TokenType.NewLine, @"\n"),
            // Whitespaces and tabs
            (TokenType.Whitespace, @"\s+"),
            // Mismatches (any other symbols)
            (TokenType.Mismatch, @".")
        };

        private static readonly Regex TokenRegex = new(
            string.Join("|", Tokens.ConvertAll(t => $"(?<{t.Item1}>{t.Item2})")),
            RegexOptions.Compiled
        );

        public static IEnumerable<Token> Tokenize(string code)
        {
            foreach (Match match in TokenRegex.Matches(code))
            {
                foreach (var tokenType in Tokens)
                {
                    if (match.Groups[tokenType.Item1.ToString()].Success)
                    {
                        if (tokenType.Item1 == TokenType.Whitespace || tokenType.Item1 == TokenType.NewLine)
                            continue;
                        if (tokenType.Item1 == TokenType.Mismatch)
                            throw new Exception($"Mismatch: {match.Value}");
                        yield return new Token(tokenType.Item1, match.Value);
                        break;
                    }
                }
            }
        }
    }
}
