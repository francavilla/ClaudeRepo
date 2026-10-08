using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PasswordGen.Core.Checks;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Randomness;

namespace PasswordGen.Core.Generation
{
    /// <summary>
    /// Genera password casuali che rispettano la policy. Ogni scelta usa la sorgente casuale indicata
    /// (crittografica in produzione) e l'entropia riportata è il logaritmo del numero di esiti possibili.
    /// </summary>
    public sealed class PasswordGenerator
    {
        private const string Consonants = "bdflmnprstvz";
        private const string Vowels = "aeiou";
        private const string Separators = "-._+=";
        private const string LowerLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string AllDigits = "0123456789";
        private const string ClearDigits = "23456789";
        private const int SyllablesPerGroup = 3;
        private const int DigitsInTail = 2;
        private const int MaxAttempts = 500;

        private readonly IRandomSource _random;
        private readonly WordList _words;

        public PasswordGenerator(IRandomSource random, WordList words)
        {
            _random = random;
            _words = words;
        }

        public GeneratedPassword Generate(GenerationOptions options)
        {
            var policy = (options.Policy ?? new PasswordPolicy()).Normalized();

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var candidate = Build(options, policy);
                if (policy.IsValid(candidate.Text)
                    && PasswordSimilarity.IsSufficientlyDifferent(options.PreviousPassword, candidate.Text)
                    && PasswordSimilarity.IsSufficientlyDifferentFromAll(options.PreviousPasswords, candidate.Text))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Non è stato possibile generare una password conforme alle regole indicate.");
        }

        /// <summary>Genera <paramref name="count"/> proposte diverse tra loro.</summary>
        public IReadOnlyList<GeneratedPassword> GenerateMany(GenerationOptions options, int count)
        {
            var result = new List<GeneratedPassword>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var guard = 0;
            while (result.Count < count && guard++ < count * 10)
            {
                var item = Generate(options);
                if (seen.Add(item.Text))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private GeneratedPassword Build(GenerationOptions options, PasswordPolicy policy)
        {
            switch (options.Mode)
            {
                case GenerationMode.Syllables:
                    return BuildSyllables(options, policy);
                case GenerationMode.Random:
                    return BuildRandom(options, policy);
                default:
                    return BuildPassphrase(options, policy);
            }
        }

        // ---------------------------------------------------------------- Passphrase

        private GeneratedPassword BuildPassphrase(GenerationOptions options, PasswordPolicy policy)
        {
            var count = Clamp(options.WordCount, GenerationOptions.MinWords, GenerationOptions.MaxWords);
            var separator = PickSeparator(policy, out double bits);
            var tail = BuildTail(policy, ref bits);

            var chosen = new List<string>();
            while (chosen.Count < count || Compose(chosen, separator, tail).Length < policy.MinLength)
            {
                if (chosen.Count >= _words.Count)
                {
                    break;
                }

                string word;
                do
                {
                    word = _random.Pick(_words.Words);
                }
                while (chosen.Contains(word));

                // Parole diverse tra loro: la scelta i-esima avviene tra (N - i) parole.
                bits += Math.Log(_words.Count - chosen.Count, 2);
                chosen.Add(word);
            }

            return new GeneratedPassword(Compose(chosen, separator, tail), bits, GenerationMode.Passphrase);
        }

        private static string Compose(IList<string> words, char separator, string tail)
        {
            return string.Join(separator.ToString(), words.Select(Capitalize)) + tail;
        }

        // ---------------------------------------------------------------- Sillabe pronunciabili

        private GeneratedPassword BuildSyllables(GenerationOptions options, PasswordPolicy policy)
        {
            var count = Clamp(options.SyllableCount, GenerationOptions.MinSyllables, GenerationOptions.MaxSyllables);
            var separator = PickSeparator(policy, out double bits);
            var tail = BuildTail(policy, ref bits);
            var syllableBits = Math.Log(Consonants.Length * Vowels.Length, 2);

            var groups = new List<string>();
            var syllables = 0;
            while (syllables < count || Compose(groups, separator, tail).Length < policy.MinLength)
            {
                if (syllables >= GenerationOptions.MaxRandomLength)
                {
                    break;
                }

                if (syllables % SyllablesPerGroup == 0)
                {
                    groups.Add(string.Empty);
                }

                groups[groups.Count - 1] += _random.Pick(Consonants).ToString() + _random.Pick(Vowels);
                bits += syllableBits;
                syllables++;
            }

            return new GeneratedPassword(Compose(groups, separator, tail), bits, GenerationMode.Syllables);
        }

        // ---------------------------------------------------------------- Casuale

        private GeneratedPassword BuildRandom(GenerationOptions options, PasswordPolicy policy)
        {
            var length = Math.Min(GenerationOptions.MaxRandomLength, Math.Max(options.RandomLength, policy.MinLength));

            var lower = policy.AvoidAmbiguous ? LowerLetters.Replace("l", string.Empty) : LowerLetters;
            var upper = policy.AvoidAmbiguous ? UpperLetters.Replace("I", string.Empty).Replace("O", string.Empty) : UpperLetters;
            var digits = policy.AvoidAmbiguous ? ClearDigits : AllDigits;

            var required = new List<string>();
            if (policy.RequireLower) { required.Add(lower); }
            if (policy.RequireUpper) { required.Add(upper); }
            if (policy.RequireDigit) { required.Add(digits); }
            if (policy.RequireSpecial) { required.Add(policy.Specials); }

            // Senza requisiti si usano comunque lettere e cifre.
            var union = required.Count > 0 ? string.Concat(required) : lower + upper + digits;

            var chars = new List<char>();
            foreach (var pool in required)
            {
                chars.Add(_random.Pick(pool));
            }

            while (chars.Count < length)
            {
                chars.Add(_random.Pick(union));
            }

            _random.Shuffle(chars);
            return new GeneratedPassword(new string(chars.ToArray()), length * Math.Log(union.Length, 2), GenerationMode.Random);
        }

        // ---------------------------------------------------------------- Parti comuni

        private char PickSeparator(PasswordPolicy policy, out double bits)
        {
            var pool = Separators;
            if (policy.RequireSpecial)
            {
                var allowed = new string(Separators.Where(c => policy.Specials.IndexOf(c) >= 0).ToArray());
                pool = allowed.Length > 0 ? allowed : policy.Specials;
            }

            bits = Math.Log(pool.Length, 2);
            return _random.Pick(pool);
        }

        /// <summary>Cifre e carattere speciale in coda, aggiunti solo se la policy li richiede.</summary>
        private string BuildTail(PasswordPolicy policy, ref double bits)
        {
            var tail = new StringBuilder();
            if (policy.RequireDigit)
            {
                var digits = policy.AvoidAmbiguous ? ClearDigits : AllDigits;
                for (var i = 0; i < DigitsInTail; i++)
                {
                    tail.Append(_random.Pick(digits));
                    bits += Math.Log(digits.Length, 2);
                }
            }

            if (policy.RequireSpecial)
            {
                tail.Append(_random.Pick(policy.Specials));
                bits += Math.Log(policy.Specials.Length, 2);
            }

            return tail.ToString();
        }

        private static string Capitalize(string word)
        {
            return word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word.Substring(1);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
