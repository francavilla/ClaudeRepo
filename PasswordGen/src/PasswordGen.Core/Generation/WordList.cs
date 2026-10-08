using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PasswordGen.Core.Generation
{
    /// <summary>Elenco di parole per le passphrase (minuscole, senza duplicati).</summary>
    public sealed class WordList
    {
        private readonly List<string> _words;

        public WordList(IEnumerable<string> words)
        {
            _words = words
                .Select(w => (w ?? string.Empty).Trim().ToLowerInvariant())
                .Where(w => w.Length > 0 && !w.StartsWith("#", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (_words.Count < 2)
            {
                throw new ArgumentException("La lista deve contenere almeno due parole.", nameof(words));
            }
        }

        public IReadOnlyList<string> Words
        {
            get { return _words; }
        }

        public int Count
        {
            get { return _words.Count; }
        }

        /// <summary>Lista italiana incorporata nell'eseguibile (Words/it.txt).</summary>
        public static WordList LoadItalian()
        {
            var assembly = typeof(WordList).Assembly;
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("Words.it.txt", StringComparison.OrdinalIgnoreCase));
            if (name == null)
            {
                throw new InvalidOperationException("La lista delle parole italiane non è incorporata nell'assembly.");
            }

            using (var stream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                return new WordList(reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }
    }
}
