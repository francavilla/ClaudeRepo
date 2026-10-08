using System;

namespace PasswordGen.Core.Checks
{
    /// <summary>
    /// Verifica che una nuova password non sia una semplice variante della precedente
    /// (stesse parole, cifra incrementata, simbolo cambiato...), come richiedono molte policy aziendali.
    /// </summary>
    public static class PasswordSimilarity
    {
        /// <summary>Lunghezza massima di una sequenza di caratteri che la nuova password può avere in comune con la precedente.</summary>
        public const int MaxSharedRun = 4;

        public static bool IsSufficientlyDifferent(string previous, string candidate)
        {
            if (string.IsNullOrEmpty(previous) || string.IsNullOrEmpty(candidate))
            {
                return true;
            }

            var a = previous.ToLowerInvariant();
            var b = candidate.ToLowerInvariant();
            var minDistance = Math.Max(5, b.Length / 2);

            return EditDistance(a, b) >= minDistance && LongestCommonSubstring(a, b) <= MaxSharedRun;
        }

        /// <summary>Vero se la nuova password è sufficientemente diversa da ciascuna delle precedenti.</summary>
        public static bool IsSufficientlyDifferentFromAll(System.Collections.Generic.IEnumerable<string> previous, string candidate)
        {
            if (previous == null)
            {
                return true;
            }

            foreach (var old in previous)
            {
                if (!IsSufficientlyDifferent(old, candidate))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Distanza di Levenshtein: numero minimo di inserimenti, cancellazioni e sostituzioni.</summary>
        public static int EditDistance(string a, string b)
        {
            if (a.Length == 0)
            {
                return b.Length;
            }

            if (b.Length == 0)
            {
                return a.Length;
            }

            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[b.Length];
        }

        /// <summary>Lunghezza della più lunga sequenza contigua di caratteri presente in entrambe le stringhe.</summary>
        public static int LongestCommonSubstring(string a, string b)
        {
            var best = 0;
            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];

            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    current[j] = a[i - 1] == b[j - 1] ? previous[j - 1] + 1 : 0;
                    if (current[j] > best)
                    {
                        best = current[j];
                    }
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return best;
        }
    }
}
