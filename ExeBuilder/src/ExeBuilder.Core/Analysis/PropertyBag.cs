using System;
using System.Collections.Generic;
using System.Text;

namespace ExeBuilder.Core.Analysis
{
    /// <summary>
    /// Proprietà MSBuild durante la valutazione statica. Le proprietà globali
    /// (es. Configuration=Release) non possono essere sovrascritte dal progetto, come in MSBuild.
    /// </summary>
    internal sealed class PropertyBag
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _global = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void SetGlobal(string name, string value)
        {
            _values[name] = value;
            _global.Add(name);
        }

        public void Set(string name, string value)
        {
            if (!_global.Contains(name))
            {
                _values[name] = value;
            }
        }

        public string Get(string name)
        {
            string value;
            return _values.TryGetValue(name, out value) ? value : string.Empty;
        }

        public bool IsDefined(string name)
        {
            return _values.ContainsKey(name);
        }

        /// <summary>
        /// Espande i riferimenti $(Nome). Le property function $([...]) non sono supportate:
        /// restano invariate e <paramref name="unresolved"/> diventa true.
        /// </summary>
        public string Expand(string text, out bool unresolved)
        {
            unresolved = false;
            if (string.IsNullOrEmpty(text) || text.IndexOf("$(", StringComparison.Ordinal) < 0)
            {
                return text ?? string.Empty;
            }

            var result = new StringBuilder(text.Length);
            var i = 0;
            while (i < text.Length)
            {
                if (text[i] == '$' && i + 1 < text.Length && text[i + 1] == '(')
                {
                    var end = FindClosingParen(text, i + 1);
                    if (end < 0)
                    {
                        result.Append(text, i, text.Length - i);
                        unresolved = true;
                        break;
                    }

                    var inner = text.Substring(i + 2, end - i - 2).Trim();
                    if (IsSimpleName(inner))
                    {
                        result.Append(Get(inner));
                    }
                    else
                    {
                        result.Append(text, i, end - i + 1);
                        unresolved = true;
                    }

                    i = end + 1;
                }
                else
                {
                    result.Append(text[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        public string Expand(string text)
        {
            bool ignored;
            return Expand(text, out ignored);
        }

        private static int FindClosingParen(string text, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '(')
                {
                    depth++;
                }
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static bool IsSimpleName(string name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            foreach (var c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
