using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PasswordGen.Core.Generation
{
    /// <summary>Esito della lettura di un file di parole: parole valide e motivi degli scarti.</summary>
    public sealed class WordFileResult
    {
        public WordFileResult(string path, IReadOnlyList<string> words, int tooShort, int tooLong, int invalidChars, int duplicates, string error)
        {
            Path = path;
            Words = words;
            TooShort = tooShort;
            TooLong = tooLong;
            InvalidChars = invalidChars;
            Duplicates = duplicates;
            Error = error;
        }

        public string Path { get; private set; }

        /// <summary>Parole valide, normalizzate e senza duplicati.</summary>
        public IReadOnlyList<string> Words { get; private set; }

        public int TooShort { get; private set; }

        public int TooLong { get; private set; }

        public int InvalidChars { get; private set; }

        public int Duplicates { get; private set; }

        /// <summary>Motivo per cui il file non si è potuto leggere (nullo se la lettura è riuscita).</summary>
        public string Error { get; private set; }

        public string FileName
        {
            get { return string.IsNullOrEmpty(Path) ? string.Empty : System.IO.Path.GetFileName(Path); }
        }

        /// <summary>Riepilogo leggibile: parole valide e scarti.</summary>
        public string Summary
        {
            get
            {
                if (Error != null)
                {
                    return Error;
                }

                var text = new StringBuilder();
                text.Append(Words.Count).Append(Words.Count == 1 ? " parola valida" : " parole valide");

                var rejected = new List<string>();
                if (TooShort > 0) { rejected.Add(TooShort + " troppo corte"); }
                if (TooLong > 0) { rejected.Add(TooLong + " troppo lunghe"); }
                if (InvalidChars > 0) { rejected.Add(InvalidChars + " con caratteri non validi"); }
                if (Duplicates > 0) { rejected.Add(Duplicates + " duplicate"); }
                if (rejected.Count > 0)
                {
                    text.Append(". Scartate: ").Append(string.Join(", ", rejected));
                }

                return text.Append('.').ToString();
            }
        }
    }

    /// <summary>
    /// Legge un file di parole dell'utente (una per riga, # per i commenti) e lo porta nello stesso formato della lista integrata:
    /// minuscole, senza accenti, solo lettere a-z, da <see cref="MinLength"/> a <see cref="MaxLength"/> caratteri, senza duplicati.
    /// </summary>
    public static class CustomWordList
    {
        public const int MinLength = 4;
        public const int MaxLength = 9;

        /// <summary>Parole valide necessarie per usare «Solo il mio file».</summary>
        public const int MinWordsCustomOnly = 300;

        /// <summary>Parole valide necessarie per aggiungere il file alla lista integrata.</summary>
        public const int MinWordsCombined = 1;

        private const long MaxFileBytes = 5 * 1024 * 1024;

        public static WordFileResult Parse(IEnumerable<string> lines, string path = null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var words = new List<string>();
            int tooShort = 0, tooLong = 0, invalid = 0, duplicates = 0;

            foreach (var line in lines)
            {
                var raw = (line ?? string.Empty).Trim();
                if (raw.Length == 0 || raw.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var word = RemoveDiacritics(raw.ToLowerInvariant());
                if (!word.All(c => c >= 'a' && c <= 'z'))
                {
                    invalid++;
                }
                else if (word.Length < MinLength)
                {
                    tooShort++;
                }
                else if (word.Length > MaxLength)
                {
                    tooLong++;
                }
                else if (!seen.Add(word))
                {
                    duplicates++;
                }
                else
                {
                    words.Add(word);
                }
            }

            return new WordFileResult(path, words, tooShort, tooLong, invalid, duplicates, null);
        }

        public static WordFileResult Load(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return Failed(path, "File non trovato: " + path);
                }

                if (info.Length > MaxFileBytes)
                {
                    return Failed(path, "Il file è troppo grande (massimo 5 MB).");
                }

                return Parse(File.ReadAllLines(path, Encoding.UTF8), path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                return Failed(path, "Impossibile leggere il file: " + ex.Message);
            }
        }

        private static WordFileResult Failed(string path, string error)
        {
            return new WordFileResult(path, new string[0], 0, 0, 0, 0, error);
        }

        private static string RemoveDiacritics(string text)
        {
            var builder = new StringBuilder();
            foreach (var c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
