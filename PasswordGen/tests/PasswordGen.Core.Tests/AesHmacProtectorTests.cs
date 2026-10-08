using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PasswordGen.Core.History;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class AesHmacProtectorTests
    {
        private static readonly byte[] Plain = Encoding.UTF8.GetBytes("Lampo-Cavallo-Nebbia-Fiume47!");

        [Fact]
        public void GenerateKey_HasTheRightLengthAndIsRandom()
        {
            var a = AesHmacProtector.GenerateKey();
            var b = AesHmacProtector.GenerateKey();

            Assert.Equal(AesHmacProtector.KeyLength, a.Length);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Protect_ThenUnprotect_RoundTrips()
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());

            Assert.Equal(Plain, protector.Unprotect(protector.Protect(Plain)));
        }

        [Fact]
        public void Protect_HidesThePlaintext()
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());

            var text = Encoding.UTF8.GetString(protector.Protect(Plain));

            Assert.DoesNotContain("Lampo", text);
        }

        [Fact]
        public void Protect_UsesANewIvEveryTime()
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());

            Assert.NotEqual(protector.Protect(Plain), protector.Protect(Plain));
        }

        [Fact]
        public void Unprotect_WithAnotherKey_Throws()
        {
            var encrypted = new AesHmacProtector(AesHmacProtector.GenerateKey()).Protect(Plain);

            Assert.Throws<CryptographicException>(() => new AesHmacProtector(AesHmacProtector.GenerateKey()).Unprotect(encrypted));
        }

        [Theory]
        [InlineData(0)]      // IV
        [InlineData(20)]     // testo cifrato
        [InlineData(-1)]     // MAC
        public void Unprotect_WithATamperedByte_Throws(int index)
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());
            var encrypted = protector.Protect(Plain);
            var position = index >= 0 ? index : encrypted.Length + index;
            encrypted[position] ^= 0x01;

            Assert.Throws<CryptographicException>(() => protector.Unprotect(encrypted));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(63)]
        public void Unprotect_WithTooShortData_Throws(int length)
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());

            Assert.Throws<CryptographicException>(() => protector.Unprotect(new byte[length]));
        }

        [Fact]
        public void Unprotect_WithNull_Throws()
        {
            Assert.Throws<CryptographicException>(() => new AesHmacProtector(AesHmacProtector.GenerateKey()).Unprotect(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(65)]
        public void Constructor_RejectsAWrongKeyLength(int length)
        {
            Assert.Throws<ArgumentException>(() => new AesHmacProtector(new byte[length]));
            Assert.Throws<ArgumentException>(() => new AesHmacProtector(null));
        }

        [Fact]
        public void Protect_WorksWithEmptyAndLargeData()
        {
            var protector = new AesHmacProtector(AesHmacProtector.GenerateKey());
            var large = Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray();

            Assert.Empty(protector.Unprotect(protector.Protect(new byte[0])));
            Assert.Equal(large, protector.Unprotect(protector.Protect(large)));
        }

        [Fact]
        public void HistoryStore_WorksWithThisProtector()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PasswordGenAes_" + Guid.NewGuid().ToString("N"), "history.dat");
            try
            {
                var store = new HistoryStore(path, new AesHmacProtector(AesHmacProtector.GenerateKey()));
                var history = new PasswordHistory();
                history.Add("Lampo-Cavallo47!", PasswordGen.Core.Generation.GenerationMode.Passphrase, new DateTime(2026, 10, 8));
                store.Save(history);

                // Chiave diversa (per esempio dopo la reinstallazione): lo storico riparte vuoto, senza errori.
                var other = new HistoryStore(path, new AesHmacProtector(AesHmacProtector.GenerateKey()));
                Assert.Empty(other.Load().Entries);
            }
            finally
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.Delete(directory, true);
                }
            }
        }
    }
}
