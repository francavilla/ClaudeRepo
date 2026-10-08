using System;
using PasswordGen.Core.Randomness;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class SecureRandomTests
    {
        [Fact]
        public void Next_StaysInRange()
        {
            using (var random = new SecureRandom())
            {
                for (var i = 0; i < 5000; i++)
                {
                    var value = random.Next(7);
                    Assert.InRange(value, 0, 6);
                }
            }
        }

        [Fact]
        public void Next_WithOne_ReturnsZero()
        {
            using (var random = new SecureRandom())
            {
                Assert.Equal(0, random.Next(1));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void Next_WithInvalidLimit_Throws(int max)
        {
            using (var random = new SecureRandom())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(max));
            }
        }

        [Fact]
        public void Next_CoversAllValuesRoughlyEvenly()
        {
            using (var random = new SecureRandom())
            {
                var counts = new int[6];
                const int draws = 60000;
                for (var i = 0; i < draws; i++)
                {
                    counts[random.Next(6)]++;
                }

                // Atteso 10000 per valore; la tolleranza larga evita falsi allarmi (deviazione standard ≈ 91).
                foreach (var count in counts)
                {
                    Assert.InRange(count, 9000, 11000);
                }
            }
        }

        [Fact]
        public void Shuffle_KeepsAllElements()
        {
            using (var random = new SecureRandom())
            {
                var items = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
                random.Shuffle(items);
                items.Sort();
                Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, items);
            }
        }
    }
}
