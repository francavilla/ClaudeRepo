using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BuildExe.Core.Analysis
{
    /// <summary>
    /// Valutatore delle condizioni MSBuild per l'analisi statica. Supporta:
    /// 'a' == 'b', !=, &lt;, &gt;, &lt;=, &gt;= (numeri/versioni), and, or, !, parentesi,
    /// Exists('...'), HasTrailingSlash('...'), true/false.
    /// Una condizione non supportata solleva <see cref="ConditionException"/>.
    /// </summary>
    internal sealed class ConditionEvaluator
    {
        private readonly PropertyBag _properties;
        private readonly string _baseDirectory;
        private List<Token> _tokens;
        private int _position;

        public ConditionEvaluator(PropertyBag properties, string baseDirectory)
        {
            _properties = properties;
            _baseDirectory = baseDirectory;
        }

        public bool Evaluate(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            _tokens = Tokenize(condition);
            _position = 0;
            var result = ParseOr();
            if (Peek().Kind != TokenKind.End)
            {
                throw new ConditionException("token inatteso '" + Peek().Text + "'");
            }

            return ToBool(result);
        }

        // ---- Parser (discesa ricorsiva)

        private Value ParseOr()
        {
            var left = ParseAnd();
            while (IsKeyword(Peek(), "or"))
            {
                _position++;
                var right = ParseAnd();
                left = Value.FromBool(ToBool(left) || ToBool(right));
            }

            return left;
        }

        private Value ParseAnd()
        {
            var left = ParseUnary();
            while (IsKeyword(Peek(), "and"))
            {
                _position++;
                var right = ParseUnary();
                left = Value.FromBool(ToBool(left) && ToBool(right));
            }

            return left;
        }

        private Value ParseUnary()
        {
            if (Peek().Kind == TokenKind.Not)
            {
                _position++;
                return Value.FromBool(!ToBool(ParseUnary()));
            }

            return ParseComparison();
        }

        private Value ParseComparison()
        {
            var left = ParsePrimary();
            var op = Peek();
            if (op.Kind != TokenKind.Operator)
            {
                return left;
            }

            _position++;
            var right = ParsePrimary();
            return Value.FromBool(Compare(left.Text, op.Text, right.Text));
        }

        private Value ParsePrimary()
        {
            var token = Next();
            switch (token.Kind)
            {
                case TokenKind.OpenParen:
                    var inner = ParseOr();
                    Expect(TokenKind.CloseParen);
                    return inner;
                case TokenKind.String:
                    return Value.FromText(ExpandOrThrow(token.Text));
                case TokenKind.Word:
                    if (Peek().Kind == TokenKind.OpenParen)
                    {
                        return CallFunction(token.Text);
                    }

                    return Value.FromText(ExpandOrThrow(token.Text));
                default:
                    throw new ConditionException("token inatteso '" + token.Text + "'");
            }
        }

        private Value CallFunction(string name)
        {
            Expect(TokenKind.OpenParen);
            var argument = Next();
            if (argument.Kind != TokenKind.String && argument.Kind != TokenKind.Word)
            {
                throw new ConditionException("argomento non valido per " + name);
            }

            Expect(TokenKind.CloseParen);
            var value = ExpandOrThrow(argument.Text).Trim();

            if (string.Equals(name, "Exists", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Length == 0)
                {
                    return Value.FromBool(false);
                }

                value = ProjectAnalyzer.NormalizeSeparators(value);
                var path = Path.IsPathRooted(value) ? value : Path.Combine(_baseDirectory, value);
                return Value.FromBool(File.Exists(path) || Directory.Exists(path));
            }

            if (string.Equals(name, "HasTrailingSlash", StringComparison.OrdinalIgnoreCase))
            {
                return Value.FromBool(value.EndsWith("\\", StringComparison.Ordinal) || value.EndsWith("/", StringComparison.Ordinal));
            }

            throw new ConditionException("funzione non supportata: " + name);
        }

        private string ExpandOrThrow(string text)
        {
            bool unresolved;
            var expanded = _properties.Expand(text, out unresolved);
            if (unresolved)
            {
                throw new ConditionException("espressione non valutabile staticamente: " + text);
            }

            return expanded;
        }

        private static bool Compare(string left, string op, string right)
        {
            switch (op)
            {
                case "==":
                    return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                case "!=":
                    return !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }

            var l = ParseNumber(left);
            var r = ParseNumber(right);
            var cmp = l.CompareTo(r);
            switch (op)
            {
                case "<":
                    return cmp < 0;
                case ">":
                    return cmp > 0;
                case "<=":
                    return cmp <= 0;
                case ">=":
                    return cmp >= 0;
                default:
                    throw new ConditionException("operatore sconosciuto " + op);
            }
        }

        private static Version ParseNumber(string text)
        {
            text = text.Trim().TrimStart('v', 'V');
            Version version;
            if (Version.TryParse(text, out version))
            {
                return version;
            }

            int number;
            if (int.TryParse(text, out number))
            {
                return new Version(number, 0);
            }

            throw new ConditionException("'" + text + "' non è un numero");
        }

        private static bool ToBool(Value value)
        {
            if (value.IsBool)
            {
                return value.Bool;
            }

            switch (value.Text.Trim().ToLowerInvariant())
            {
                case "true":
                case "on":
                case "yes":
                case "!false":
                    return true;
                case "false":
                case "off":
                case "no":
                case "!true":
                    return false;
                default:
                    throw new ConditionException("'" + value.Text + "' non è un valore booleano");
            }
        }

        // ---- Tokenizer

        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '(')
                {
                    tokens.Add(new Token(TokenKind.OpenParen, "("));
                    i++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenKind.CloseParen, ")"));
                    i++;
                }
                else if (c == '\'')
                {
                    var end = text.IndexOf('\'', i + 1);
                    if (end < 0)
                    {
                        throw new ConditionException("apice non chiuso");
                    }

                    tokens.Add(new Token(TokenKind.String, text.Substring(i + 1, end - i - 1)));
                    i = end + 1;
                }
                else if (c == '=' || c == '!' || c == '<' || c == '>')
                {
                    var twoChars = i + 1 < text.Length && text[i + 1] == '=';
                    if (c == '!' && !twoChars)
                    {
                        tokens.Add(new Token(TokenKind.Not, "!"));
                        i++;
                    }
                    else if (c == '=' && !twoChars)
                    {
                        throw new ConditionException("'=' non valido, usare '=='");
                    }
                    else
                    {
                        tokens.Add(new Token(TokenKind.Operator, twoChars ? text.Substring(i, 2) : c.ToString()));
                        i += twoChars ? 2 : 1;
                    }
                }
                else
                {
                    // Parola: keyword, nome di funzione, numero o $(Proprietà) non quotata.
                    var sb = new StringBuilder();
                    var depth = 0;
                    while (i < text.Length)
                    {
                        var ch = text[i];
                        if (depth == 0 && (char.IsWhiteSpace(ch) || ch == '\'' || ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == ')' || (ch == '(' && (sb.Length == 0 || sb[sb.Length - 1] != '$'))))
                        {
                            break;
                        }

                        if (ch == '(')
                        {
                            depth++;
                        }
                        else if (ch == ')')
                        {
                            depth--;
                        }

                        sb.Append(ch);
                        i++;
                    }

                    tokens.Add(new Token(TokenKind.Word, sb.ToString()));
                }
            }

            tokens.Add(new Token(TokenKind.End, "<fine>"));
            return tokens;
        }

        private Token Peek()
        {
            return _tokens[_position];
        }

        private Token Next()
        {
            var token = _tokens[_position];
            if (token.Kind != TokenKind.End)
            {
                _position++;
            }

            return token;
        }

        private void Expect(TokenKind kind)
        {
            if (Next().Kind != kind)
            {
                throw new ConditionException("atteso " + kind);
            }
        }

        private static bool IsKeyword(Token token, string keyword)
        {
            return token.Kind == TokenKind.Word && string.Equals(token.Text, keyword, StringComparison.OrdinalIgnoreCase);
        }

        private enum TokenKind
        {
            OpenParen,
            CloseParen,
            String,
            Word,
            Operator,
            Not,
            End
        }

        private struct Token
        {
            public Token(TokenKind kind, string text)
            {
                Kind = kind;
                Text = text;
            }

            public TokenKind Kind { get; }

            public string Text { get; }
        }

        private struct Value
        {
            public bool IsBool { get; private set; }

            public bool Bool { get; private set; }

            public string Text { get; private set; }

            public static Value FromBool(bool value)
            {
                return new Value { IsBool = true, Bool = value, Text = value ? "true" : "false" };
            }

            public static Value FromText(string text)
            {
                return new Value { Text = text };
            }
        }
    }

    internal sealed class ConditionException : Exception
    {
        public ConditionException(string message)
            : base(message)
        {
        }
    }
}
