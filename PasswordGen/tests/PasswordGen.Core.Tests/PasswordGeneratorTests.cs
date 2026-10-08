using System;
using System.Linq;
using PasswordGen.Core.Checks;
using PasswordGen.Core.Generation;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Randomness;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class PasswordGeneratorTests
    {
        private static readonly WordList Words = WordList.LoadItalian();

        private static PasswordGenerator Create(IRandomSource random = null)
        {
            return new PasswordGenerator(random ?? new SecureRandom(), Words);
        }

        [Theory]
        [InlineData(GenerationMode.Passphrase)]
        [InlineData(GenerationMode.Syllables)]
        [InlineData(GenerationMode.Random)]
        public void Generate_AlwaysSatisfiesTheDefaultPolicy(GenerationMode mode)
        {
            var generator = Create();
            var policy = new PasswordPolicy();

            for (var i = 0; i < 300; i++)
            {
                var password = generator.Generate(new GenerationOptions { Mode = mode, Policy = policy });

                var problems = policy.Validate(password.Text);
                Assert.True(problems.Count == 0, password.Text + " -> " + string.Join(", ", problems));
                Assert.True(password.EntropyBits > 30, password.Text + " entropia " + password.EntropyBits);
                Assert.Equal(mode, password.Mode);
            }
        }

        [Theory]
        [InlineData(GenerationMode.Passphrase)]
        [InlineData(GenerationMode.Syllables)]
        [InlineData(GenerationMode.Random)]
        public void Generate_RespectsALongerMinimumLength(GenerationMode mode)
        {
            var generator = Create();
            var policy = new PasswordPolicy { MinLength = 24 };

            for (var i = 0; i < 100; i++)
            {
                var password = generator.Generate(new GenerationOptions { Mode = mode, Policy = policy, RandomLength = 8 });

                Assert.True(password.Text.Length >= 24, password.Text);
            }
        }

        [Fact]
        public void Passphrase_UsesCapitalizedItalianWordsAndExactWordCount()
        {
            var generator = Create();

            for (var count = GenerationOptions.MinWords; count <= GenerationOptions.MaxWords; count++)
            {
                var password = generator.Generate(new GenerationOptions { WordCount = count });

                // Parole + (cifre e simbolo finali): le parole sono separate da un solo carattere speciale.
                var tailLength = 3;
                var body = password.Text.Substring(0, password.Text.Length - tailLength);
                var parts = body.Split(new[] { '-', '.', '_', '+', '=' });

                Assert.Equal(count, parts.Length);
                foreach (var part in parts)
                {
                    Assert.True(char.IsUpper(part[0]), password.Text);
                    Assert.Contains(part.ToLowerInvariant(), Words.Words);
                }
            }
        }

        [Fact]
        public void Passphrase_UsesDistinctWords()
        {
            var generator = Create();

            for (var i = 0; i < 200; i++)
            {
                var password = generator.Generate(new GenerationOptions { WordCount = 8 });
                var body = password.Text.Substring(0, password.Text.Length - 3);
                var parts = body.Split(new[] { '-', '.', '_', '+', '=' });

                Assert.Equal(parts.Length, parts.Distinct().Count());
            }
        }

        [Fact]
        public void Passphrase_EntropyGrowsWithWordCount()
        {
            var generator = Create();

            var four = generator.Generate(new GenerationOptions { WordCount = 4 }).EntropyBits;
            var five = generator.Generate(new GenerationOptions { WordCount = 5 }).EntropyBits;

            // Una parola in più vale circa log2(N) bit, con N ≥ 1000.
            Assert.True(five - four > 9.5, four + " -> " + five);
        }

        [Fact]
        public void Passphrase_EntropyMatchesTheChoicesMade()
        {
            var generator = Create();
            var password = generator.Generate(new GenerationOptions { WordCount = 4 });

            var n = Words.Count;
            var wordBits = Math.Log(n, 2) + Math.Log(n - 1, 2) + Math.Log(n - 2, 2) + Math.Log(n - 3, 2);
            var expected = wordBits
                + Math.Log(5, 2)                    // separatore tra 5 possibili
                + 2 * Math.Log(8, 2)                // due cifre senza 0 e 1
                + Math.Log(PasswordPolicy.DefaultSpecials.Length, 2);

            Assert.Equal(expected, password.EntropyBits, 6);
        }

        [Fact]
        public void Syllables_AreMadeOfPronounceableGroups()
        {
            var generator = Create();
            var password = generator.Generate(new GenerationOptions { Mode = GenerationMode.Syllables, SyllableCount = 9 });

            var body = password.Text.Substring(0, password.Text.Length - 3);
            var groups = body.Split(new[] { '-', '.', '_', '+', '=' });

            Assert.Equal(3, groups.Length);
            foreach (var group in groups)
            {
                Assert.Equal(6, group.Length);
                Assert.True(char.IsUpper(group[0]));
                for (var i = 0; i < group.Length; i++)
                {
                    var isConsonantPosition = i % 2 == 0;
                    var c = char.ToLowerInvariant(group[i]);
                    Assert.Equal(isConsonantPosition, "aeiou".IndexOf(c) < 0);
                }
            }
        }

        [Fact]
        public void Random_AvoidsAmbiguousCharacters()
        {
            var generator = Create();
            var policy = new PasswordPolicy { AvoidAmbiguous = true };

            for (var i = 0; i < 300; i++)
            {
                var text = generator.Generate(new GenerationOptions { Mode = GenerationMode.Random, Policy = policy, RandomLength = 32 }).Text;

                Assert.DoesNotContain("0", text);
                Assert.DoesNotContain("1", text);
                Assert.DoesNotContain("O", text);
                Assert.DoesNotContain("I", text);
                Assert.DoesNotContain("l", text);
            }
        }

        [Fact]
        public void Random_WithOnlyLettersAndDigitsRequired_HasNoSpecials()
        {
            var generator = Create();
            var policy = new PasswordPolicy { RequireSpecial = false };

            for (var i = 0; i < 100; i++)
            {
                var text = generator.Generate(new GenerationOptions { Mode = GenerationMode.Random, Policy = policy }).Text;

                Assert.True(text.All(char.IsLetterOrDigit), text);
            }
        }

        [Fact]
        public void Random_EntropyIsLengthTimesLogOfAlphabet()
        {
            var generator = Create();
            var policy = new PasswordPolicy { AvoidAmbiguous = false };

            var password = generator.Generate(new GenerationOptions { Mode = GenerationMode.Random, Policy = policy, RandomLength = 20 });

            var alphabet = 26 + 26 + 10 + PasswordPolicy.DefaultSpecials.Length;
            Assert.Equal(20 * Math.Log(alphabet, 2), password.EntropyBits, 6);
        }

        [Theory]
        [InlineData(GenerationMode.Passphrase)]
        [InlineData(GenerationMode.Syllables)]
        [InlineData(GenerationMode.Random)]
        public void Generate_AvoidsBeingSimilarToThePreviousPassword(GenerationMode mode)
        {
            var generator = Create();
            const string previous = "Lampo-Cavallo-Nebbia-Fiume47!";

            for (var i = 0; i < 100; i++)
            {
                var password = generator.Generate(new GenerationOptions { Mode = mode, PreviousPassword = previous });

                Assert.True(PasswordSimilarity.IsSufficientlyDifferent(previous, password.Text), password.Text);
            }
        }

        [Fact]
        public void GenerateMany_ReturnsDistinctSuggestions()
        {
            var generator = Create();

            var items = generator.GenerateMany(new GenerationOptions(), 8);

            Assert.Equal(8, items.Count);
            Assert.Equal(8, items.Select(i => i.Text).Distinct().Count());
        }

        [Fact]
        public void Generate_IsDeterministicWithTheSameRandomSequence()
        {
            var first = Create(new CounterRandom(5)).Generate(new GenerationOptions());
            var second = Create(new CounterRandom(5)).Generate(new GenerationOptions());

            Assert.Equal(first.Text, second.Text);
        }

        [Fact]
        public void Generate_WhenNoCandidateIsDifferentEnough_Throws()
        {
            // Con una sorgente costante la modalità casuale produce sempre la stessa password.
            var generator = Create(new ConstantRandom());
            var policy = new PasswordPolicy { MaxRepeatRun = 0 };
            var first = generator.Generate(new GenerationOptions { Mode = GenerationMode.Random, Policy = policy });

            var options = new GenerationOptions { Mode = GenerationMode.Random, Policy = policy, PreviousPassword = first.Text };

            Assert.Throws<InvalidOperationException>(() => generator.Generate(options));
        }

        [Theory]
        [InlineData(10, StrengthLevel.Weak)]
        [InlineData(39.9, StrengthLevel.Weak)]
        [InlineData(40, StrengthLevel.Fair)]
        [InlineData(59.9, StrengthLevel.Fair)]
        [InlineData(60, StrengthLevel.Good)]
        [InlineData(80, StrengthLevel.Excellent)]
        public void Strength_ClassifiesEntropy(double bits, StrengthLevel expected)
        {
            Assert.Equal(expected, PasswordStrength.Classify(bits));
        }

        private sealed class ConstantRandom : IRandomSource
        {
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
