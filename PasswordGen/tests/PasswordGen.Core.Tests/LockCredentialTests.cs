using System;
using System.IO;
using System.Linq;
using PasswordGen.Core.History;
using PasswordGen.Core.Security;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class LockCredentialTests : IDisposable
    {
        private const int FastIterations = 1000;   // nei test non servono le 150.000 iterazioni di produzione
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenLock_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private string FilePath
        {
            get { return Path.Combine(_directory, "lock.dat"); }
        }

        private static LockCredentialStore NewStore(string path, byte[] key = null)
        {
            return new LockCredentialStore(path, new AesHmacProtector(key ?? new byte[AesHmacProtector.KeyLength]));
        }

        // ------------------------------------------------------------ regole

        [Theory]
        [InlineData("1234", true)]
        [InlineData("123456789012", true)]
        [InlineData("123", false)]
        [InlineData("1234567890123", false)]
        [InlineData("12a4", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Validate_Pin(string secret, bool valid)
        {
            Assert.Equal(valid, LockCredential.Validate(CredentialKind.Pin, secret) == null);
        }

        [Theory]
        [InlineData("abcdef", true)]
        [InlineData("una frase lunga ma valida", true)]
        [InlineData("abcde", false)]
        [InlineData("      ", false)]
        [InlineData("", false)]
        public void Validate_Password(string secret, bool valid)
        {
            Assert.Equal(valid, LockCredential.Validate(CredentialKind.Password, secret) == null);
        }

        [Fact]
        public void Create_WithAnInvalidSecret_Throws()
        {
            Assert.Throws<ArgumentException>(() => LockCredential.Create(CredentialKind.Pin, "12", FastIterations));
        }

        // ------------------------------------------------------------ hash

        [Fact]
        public void Matches_AcceptsTheRightSecretOnly()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "482915", FastIterations);

            Assert.True(credential.Matches("482915"));
            Assert.False(credential.Matches("482916"));
            Assert.False(credential.Matches(string.Empty));
            Assert.False(credential.Matches(null));
        }

        [Fact]
        public void Create_DoesNotKeepTheSecretAndUsesARandomSalt()
        {
            var a = LockCredential.Create(CredentialKind.Password, "una-password", FastIterations);
            var b = LockCredential.Create(CredentialKind.Password, "una-password", FastIterations);

            Assert.DoesNotContain("una-password", a.Hash + a.Salt);
            Assert.NotEqual(a.Salt, b.Salt);
            Assert.NotEqual(a.Hash, b.Hash);
        }

        // ------------------------------------------------------------ attesa crescente

        [Theory]
        [InlineData(0, 0)]
        [InlineData(4, 0)]
        [InlineData(5, 30)]
        [InlineData(6, 60)]
        [InlineData(7, 120)]
        [InlineData(8, 240)]
        [InlineData(20, 3600)]
        public void DelayFor_DoublesAfterTheFifthMistake(int failures, double seconds)
        {
            Assert.Equal(TimeSpan.FromSeconds(seconds), LockCredential.DelayFor(failures));
        }

        [Fact]
        public void Check_FourWrongAttemptsDoNotLock()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "4829", FastIterations);

            for (var i = 1; i <= 4; i++)
            {
                var result = credential.Check("0000", T0);

                Assert.Equal(CredentialCheck.Wrong, result.Outcome);
                Assert.Equal(TimeSpan.Zero, result.RetryAfter);
                Assert.Equal(LockCredential.FreeAttempts - i, result.FreeAttemptsLeft);
            }

            Assert.Equal(CredentialCheck.Correct, credential.Check("4829", T0).Outcome);
        }

        [Fact]
        public void Check_FifthWrongAttemptStartsAThirtySecondWait()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "4829", FastIterations);
            for (var i = 0; i < 4; i++)
            {
                credential.Check("0000", T0);
            }

            var fifth = credential.Check("0000", T0);

            Assert.Equal(CredentialCheck.Wrong, fifth.Outcome);
            Assert.Equal(TimeSpan.FromSeconds(30), fifth.RetryAfter);
        }

        [Fact]
        public void Check_DuringTheWait_EvenTheRightSecretIsRefused()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "4829", FastIterations);
            for (var i = 0; i < 5; i++)
            {
                credential.Check("0000", T0);
            }

            var result = credential.Check("4829", T0.AddSeconds(10));

            Assert.Equal(CredentialCheck.LockedOut, result.Outcome);
            Assert.Equal(TimeSpan.FromSeconds(20), result.RetryAfter);
        }

        [Fact]
        public void Check_AfterTheWait_TheNextMistakeDoublesIt()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "4829", FastIterations);
            for (var i = 0; i < 5; i++)
            {
                credential.Check("0000", T0);
            }

            var afterWait = T0.AddSeconds(31);
            var sixth = credential.Check("0000", afterWait);

            Assert.Equal(CredentialCheck.Wrong, sixth.Outcome);
            Assert.Equal(TimeSpan.FromSeconds(60), sixth.RetryAfter);
        }

        [Fact]
        public void Check_ASuccessResetsTheCounters()
        {
            var credential = LockCredential.Create(CredentialKind.Pin, "4829", FastIterations);
            for (var i = 0; i < 4; i++)
            {
                credential.Check("0000", T0);
            }

            credential.Check("4829", T0);

            Assert.Equal(0, credential.FailedAttempts);
            var wrong = credential.Check("0000", T0);
            Assert.Equal(CredentialCheck.Wrong, wrong.Outcome);
            Assert.Equal(LockCredential.FreeAttempts - 1, wrong.FreeAttemptsLeft);
        }

        // ------------------------------------------------------------ gestore e archivio

        [Fact]
        public void Manager_WithoutAFile_HasNoCredential()
        {
            var manager = new LockCredentialManager(NewStore(FilePath));

            Assert.False(manager.HasCredential);
            Assert.Null(manager.Kind);
        }

        [Fact]
        public void Manager_SetThenReload_KeepsTheCredential()
        {
            var manager = new LockCredentialManager(NewStore(FilePath), () => T0);
            manager.Set(CredentialKind.Pin, "482915", FastIterations);

            var reloaded = new LockCredentialManager(NewStore(FilePath), () => T0);

            Assert.True(reloaded.HasCredential);
            Assert.Equal(CredentialKind.Pin, reloaded.Kind);
            Assert.Equal(CredentialCheck.Correct, reloaded.Check("482915").Outcome);
            Assert.Equal(CredentialCheck.Wrong, reloaded.Check("000000").Outcome);
        }

        [Fact]
        public void Manager_WrongAttemptsSurviveARestart()
        {
            var manager = new LockCredentialManager(NewStore(FilePath), () => T0);
            manager.Set(CredentialKind.Pin, "482915", FastIterations);
            for (var i = 0; i < 5; i++)
            {
                manager.Check("000000");
            }

            // L'app viene chiusa e riaperta: l'attesa non si azzera.
            var reloaded = new LockCredentialManager(NewStore(FilePath), () => T0.AddSeconds(5));

            Assert.Equal(CredentialCheck.LockedOut, reloaded.Check("482915").Outcome);
            Assert.Equal(TimeSpan.FromSeconds(25), reloaded.RetryAfter());
        }

        [Fact]
        public void Manager_Clear_RemovesTheCredentialAndTheFile()
        {
            var manager = new LockCredentialManager(NewStore(FilePath));
            manager.Set(CredentialKind.Password, "una-password", FastIterations);

            manager.Clear();

            Assert.False(manager.HasCredential);
            Assert.False(File.Exists(FilePath));
        }

        [Fact]
        public void Manager_Set_ReplacesTheOldCredentialAndResetsTheAttempts()
        {
            var manager = new LockCredentialManager(NewStore(FilePath), () => T0);
            manager.Set(CredentialKind.Pin, "482915", FastIterations);
            for (var i = 0; i < 5; i++)
            {
                manager.Check("000000");
            }

            manager.Set(CredentialKind.Password, "nuova-password", FastIterations);

            Assert.Equal(CredentialKind.Password, manager.Kind);
            Assert.Equal(TimeSpan.Zero, manager.RetryAfter());
            Assert.Equal(CredentialCheck.Correct, manager.Check("nuova-password").Outcome);
        }

        [Fact]
        public void Store_FileIsEncryptedAndNotReadableWithAnotherKey()
        {
            var key = Enumerable.Range(1, AesHmacProtector.KeyLength).Select(i => (byte)i).ToArray();
            var manager = new LockCredentialManager(NewStore(FilePath, key));
            manager.Set(CredentialKind.Pin, "482915", FastIterations);

            var raw = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(FilePath));
            var withOtherKey = new LockCredentialManager(NewStore(FilePath, new byte[AesHmacProtector.KeyLength]));

            Assert.DoesNotContain("Salt", raw);
            Assert.DoesNotContain("482915", raw);
            Assert.False(withOtherKey.HasCredential);
        }

        [Fact]
        public void Manager_CheckWithoutACredential_IsWrong()
        {
            var manager = new LockCredentialManager(NewStore(FilePath));

            Assert.Equal(CredentialCheck.Wrong, manager.Check("1234").Outcome);
        }
    }
}
