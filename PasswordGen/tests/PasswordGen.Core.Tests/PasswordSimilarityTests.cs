using PasswordGen.Core.Checks;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class PasswordSimilarityTests
    {
        [Theory]
        [InlineData("", "abc", 3)]
        [InlineData("abc", "", 3)]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("uguale", "uguale", 0)]
        public void EditDistance_IsLevenshtein(string a, string b, int expected)
        {
            Assert.Equal(expected, PasswordSimilarity.EditDistance(a, b));
        }

        [Theory]
        [InlineData("abcdef", "xxcdexx", 3)]
        [InlineData("abc", "xyz", 0)]
        [InlineData("Lampo47", "lampo99", 5)]
        public void LongestCommonSubstring_FindsTheLongestSharedRun(string a, string b, int expected)
        {
            Assert.Equal(expected, PasswordSimilarity.LongestCommonSubstring(a.ToLowerInvariant(), b.ToLowerInvariant()));
        }

        [Theory]
        [InlineData("Lampo-Cavallo-Nebbia47!", "Lampo-Cavallo-Nebbia48!")]   // cifra incrementata
        [InlineData("Lampo-Cavallo-Nebbia47!", "Lampo-Cavallo-Nebbia47?")]   // simbolo cambiato
        [InlineData("Estate2026!", "Autunno2026!")]                          // stagione cambiata
        [InlineData("Password1!", "Password2!")]
        public void IsSufficientlyDifferent_RejectsSimpleVariants(string previous, string candidate)
        {
            Assert.False(PasswordSimilarity.IsSufficientlyDifferent(previous, candidate));
        }

        [Fact]
        public void IsSufficientlyDifferent_AcceptsUnrelatedPasswords()
        {
            Assert.True(PasswordSimilarity.IsSufficientlyDifferent("Lampo-Cavallo-Nebbia47!", "Torre.Ruscello.Dado.Pino83="));
        }

        [Fact]
        public void IsSufficientlyDifferent_IsCaseInsensitive()
        {
            Assert.False(PasswordSimilarity.IsSufficientlyDifferent("Lampo-Cavallo47!", "LAMPO-CAVALLO47!"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsSufficientlyDifferent_WithoutPreviousPassword_IsAlwaysTrue(string previous)
        {
            Assert.True(PasswordSimilarity.IsSufficientlyDifferent(previous, "Qualsiasi-Cosa47!"));
        }
    }
}
