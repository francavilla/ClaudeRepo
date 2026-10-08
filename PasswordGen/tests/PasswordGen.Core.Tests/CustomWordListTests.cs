using System;
using System.IO;
using System.Linq;
using PasswordGen.Core.Generation;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class CustomWordListTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenWords_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private static WordList Builtin
        {
            get { return WordList.LoadItalian(); }
        }

        private static WordFileResult ManyWords(int count)
        {
            // Parole sintetiche tutte diverse: "aaaa", "aaab", ... (4 lettere)
            var words = Enumerable.Range(0, count).Select(i =>
                new string(new[] { 'a', (char)('a' + (i / 676) % 26), (char)('a' + (i / 26) % 26), (char)('a' + i % 26) }));
            return CustomWordList.Parse(words);
        }

        // ------------------------------------------------------------ Parse

        [Fact]
        public void Parse_NormalizesCaseAndAccents()
        {
            var result = CustomWordList.Parse(new[] { "Perché", "CITTÀ", "  Lampo  ", "più" });

            Assert.Equal(new[] { "perche", "citta", "lampo" }, result.Words);
            Assert.Equal(1, result.TooShort);   // «più» -> «piu», 3 lettere
        }

        [Fact]
        public void Parse_IgnoresBlankLinesAndComments()
        {
            var result = CustomWordList.Parse(new[] { "# commento", "", "   ", "cavallo", "#altro" });

            Assert.Equal(new[] { "cavallo" }, result.Words);
            Assert.Equal(0, result.TooShort + result.TooLong + result.InvalidChars + result.Duplicates);
        }

        [Fact]
        public void Parse_CountsEachKindOfRejection()
        {
            var result = CustomWordList.Parse(new[]
            {
                "sole",                  // valida
                "SOLE",                  // duplicata (dopo la normalizzazione)
                "ape",                   // troppo corta
                "parallelepipedo",       // troppo lunga
                "casa-mia",              // trattino
                "ab12cd",                // cifre
                "due parole",            // spazio
                "straße"                 // ß non si scompone in lettere a-z
            });

            Assert.Equal(new[] { "sole" }, result.Words);
            Assert.Equal(1, result.Duplicates);
            Assert.Equal(1, result.TooShort);
            Assert.Equal(1, result.TooLong);
            Assert.Equal(4, result.InvalidChars);
        }

        [Theory]
        [InlineData("abc", false)]
        [InlineData("abcd", true)]
        [InlineData("abcdefghi", true)]
        [InlineData("abcdefghij", false)]
        public void Parse_AcceptsOnlyFourToNineLetters(string word, bool accepted)
        {
            Assert.Equal(accepted, CustomWordList.Parse(new[] { word }).Words.Count == 1);
        }

        [Fact]
        public void Summary_DescribesValidAndRejectedWords()
        {
            var result = CustomWordList.Parse(new[] { "sole", "sole", "ape" });

            Assert.Contains("1 parola valida", result.Summary);
            Assert.Contains("1 troppo corte", result.Summary);
            Assert.Contains("1 duplicate", result.Summary);
        }

        // ------------------------------------------------------------ Load

        [Fact]
        public void Load_ReadsAUtf8FileWithBom()
        {
            Directory.CreateDirectory(_directory);
            var path = Path.Combine(_directory, "parole.txt");
            File.WriteAllText(path, "# le mie parole\nPerché\ncavallo\n", new System.Text.UTF8Encoding(true));

            var result = CustomWordList.Load(path);

            Assert.Null(result.Error);
            Assert.Equal(new[] { "perche", "cavallo" }, result.Words);
            Assert.Equal("parole.txt", result.FileName);
        }

        [Fact]
        public void Load_MissingFile_ReportsAnError()
        {
            var result = CustomWordList.Load(Path.Combine(_directory, "non-esiste.txt"));

            Assert.NotNull(result.Error);
            Assert.Contains("non trovato", result.Error);
            Assert.Empty(result.Words);
        }

        // ------------------------------------------------------------ WordSelection

        [Fact]
        public void Select_Builtin_UsesTheBuiltinList()
        {
            var builtin = Builtin;

            var selection = WordSelection.Select(builtin, ManyWords(500), WordSourceMode.Builtin);

            Assert.Same(builtin, selection.List);
            Assert.Equal(WordSourceMode.Builtin, selection.Effective);
            Assert.Equal(string.Empty, selection.Warning);
        }

        [Fact]
        public void Select_Combined_AddsTheCustomWords()
        {
            var builtin = Builtin;
            var custom = CustomWordList.Parse(new[] { "gnocchi", "tortello", "sole" });   // «sole» è già nella lista integrata

            var selection = WordSelection.Select(builtin, custom, WordSourceMode.Combined);

            Assert.Equal(WordSourceMode.Combined, selection.Effective);
            Assert.Equal(builtin.Count + 2, selection.List.Count);
            Assert.Contains("gnocchi", selection.List.Words);
        }

        [Fact]
        public void Select_Combined_AcceptsEvenASingleWord()
        {
            var selection = WordSelection.Select(Builtin, CustomWordList.Parse(new[] { "gnocchi" }), WordSourceMode.Combined);

            Assert.Equal(WordSourceMode.Combined, selection.Effective);
        }

        [Fact]
        public void Select_CustomOnly_RequiresAtLeast300Words()
        {
            var builtin = Builtin;

            var tooFew = WordSelection.Select(builtin, ManyWords(CustomWordList.MinWordsCustomOnly - 1), WordSourceMode.CustomOnly);
            var enough = WordSelection.Select(builtin, ManyWords(CustomWordList.MinWordsCustomOnly), WordSourceMode.CustomOnly);

            Assert.Equal(WordSourceMode.Builtin, tooFew.Effective);
            Assert.Same(builtin, tooFew.List);
            Assert.Contains("299", tooFew.Warning);
            Assert.Contains("300", tooFew.Warning);

            Assert.Equal(WordSourceMode.CustomOnly, enough.Effective);
            Assert.Equal(300, enough.List.Count);
        }

        [Theory]
        [InlineData(WordSourceMode.Combined)]
        [InlineData(WordSourceMode.CustomOnly)]
        public void Select_WithoutAFile_FallsBackToTheBuiltinList(WordSourceMode requested)
        {
            var builtin = Builtin;

            var selection = WordSelection.Select(builtin, null, requested);

            Assert.Same(builtin, selection.List);
            Assert.Equal(WordSourceMode.Builtin, selection.Effective);
            Assert.NotEqual(string.Empty, selection.Warning);
        }

        [Fact]
        public void Select_WithAnUnreadableFile_ReportsTheError()
        {
            var broken = CustomWordList.Load(Path.Combine(_directory, "manca.txt"));

            var selection = WordSelection.Select(Builtin, broken, WordSourceMode.CustomOnly);

            Assert.Equal(WordSourceMode.Builtin, selection.Effective);
            Assert.Contains("non trovato", selection.Warning);
        }

        [Fact]
        public void Select_WarnsWhenTheListIsShorterThanRecommended()
        {
            var selection = WordSelection.Select(Builtin, ManyWords(400), WordSourceMode.CustomOnly);

            Assert.Equal(400, selection.List.Count);
            Assert.Contains("400 parole", selection.Warning);
            Assert.InRange(selection.BitsPerWord, 8.6, 8.7);
        }

        [Fact]
        public void Select_NoSizeWarningForTheBuiltinList()
        {
            Assert.Equal(string.Empty, WordSelection.Select(Builtin, null, WordSourceMode.Builtin).Warning);
        }

        // ------------------------------------------------------------ Generatore

        [Fact]
        public void Generator_UsesTheNewListAfterSetWords()
        {
            var generator = new PasswordGenerator(new PasswordGen.Core.Randomness.SecureRandom(), Builtin);
            var custom = new WordList(new[] { "gnocchi", "tortello", "ravioli", "agnolotti", "cappelletti", "tortelli" });
            generator.SetWords(custom);

            for (var i = 0; i < 50; i++)
            {
                var text = generator.Generate(new GenerationOptions { WordCount = 3 }).Text;
                var body = text.Substring(0, text.Length - 3);
                foreach (var part in body.Split(new[] { '-', '.', '_', '+', '=' }))
                {
                    Assert.Contains(part.ToLowerInvariant(), custom.Words);
                }
            }
        }
    }
}
