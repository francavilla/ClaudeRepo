using System.Linq;
using System.Text.RegularExpressions;
using PasswordGen.Core.Generation;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class WordListTests
    {
        [Fact]
        public void LoadItalian_HasEnoughWords()
        {
            var list = WordList.LoadItalian();

            Assert.True(list.Count >= 1000, "Parole: " + list.Count);
        }

        [Fact]
        public void LoadItalian_WordsAreLowercaseAsciiAndReasonablyShort()
        {
            var list = WordList.LoadItalian();
            var pattern = new Regex("^[a-z]{4,9}$");

            var invalid = list.Words.Where(w => !pattern.IsMatch(w)).ToList();

            Assert.True(invalid.Count == 0, "Parole non valide: " + string.Join(", ", invalid));
        }

        [Fact]
        public void LoadItalian_HasNoDuplicates()
        {
            var list = WordList.LoadItalian();

            Assert.Equal(list.Count, list.Words.Distinct().Count());
        }

        [Fact]
        public void Constructor_NormalizesAndDeduplicates()
        {
            var list = new WordList(new[] { " Gatto ", "gatto", "# commento", "", "Luna" });

            Assert.Equal(new[] { "gatto", "luna" }, list.Words);
        }

        [Fact]
        public void Constructor_RejectsTinyLists()
        {
            Assert.Throws<System.ArgumentException>(() => new WordList(new[] { "uno" }));
        }
    }
}
