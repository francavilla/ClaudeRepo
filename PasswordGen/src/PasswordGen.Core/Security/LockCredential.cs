using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGen.Core.Security
{
    public enum CredentialKind
    {
        /// <summary>Solo cifre, da 4 a 12.</summary>
        Pin = 1,

        /// <summary>Testo libero, da 6 a 64 caratteri.</summary>
        Password = 2
    }

    public enum CredentialCheck
    {
        Correct = 0,
        Wrong = 1,

        /// <summary>Troppi tentativi sbagliati: bisogna aspettare.</summary>
        LockedOut = 2
    }

    public sealed class CredentialCheckResult
    {
        public CredentialCheckResult(CredentialCheck outcome, TimeSpan retryAfter, int freeAttemptsLeft)
        {
            Outcome = outcome;
            RetryAfter = retryAfter;
            FreeAttemptsLeft = freeAttemptsLeft;
        }

        public CredentialCheck Outcome { get; private set; }

        /// <summary>Attesa prima del prossimo tentativo (zero se non c'è attesa).</summary>
        public TimeSpan RetryAfter { get; private set; }

        /// <summary>Tentativi sbagliati ancora possibili prima che scatti l'attesa.</summary>
        public int FreeAttemptsLeft { get; private set; }
    }

    /// <summary>
    /// PIN o password scelti nell'app per aprirla: si conserva solo un hash (PBKDF2-SHA256 con sale casuale), mai il testo.
    /// Dopo <see cref="FreeAttempts"/> tentativi sbagliati scatta un'attesa che raddoppia a ogni errore (30 s, 1 min, 2 min... al massimo 1 ora).
    /// </summary>
    [DataContract]
    public sealed class LockCredential
    {
        public const int DefaultIterations = 150000;
        public const int FreeAttempts = 5;
        public const int PinMinLength = 4;
        public const int PinMaxLength = 12;
        public const int PasswordMinLength = 6;
        public const int PasswordMaxLength = 64;

        private const int HashLength = 32;
        private const int SaltLength = 16;
        private const double BaseDelaySeconds = 30;
        private const double MaxDelaySeconds = 3600;

        [DataMember] public CredentialKind Kind { get; set; }

        [DataMember] public string Salt { get; set; }

        [DataMember] public string Hash { get; set; }

        [DataMember] public int Iterations { get; set; }

        [DataMember] public int FailedAttempts { get; set; }

        /// <summary>Fino a quando (UTC, in tick) non si può riprovare; zero se non c'è attesa.</summary>
        [DataMember] public long LockedUntilTicks { get; set; }

        /// <summary>Messaggio che spiega perché il PIN o la password non vanno bene, oppure null se vanno bene.</summary>
        public static string Validate(CredentialKind kind, string secret)
        {
            secret = secret ?? string.Empty;
            if (kind == CredentialKind.Pin)
            {
                if (secret.Length < PinMinLength || secret.Length > PinMaxLength || !secret.All(c => c >= '0' && c <= '9'))
                {
                    return "Il PIN deve avere da " + PinMinLength + " a " + PinMaxLength + " cifre.";
                }

                return null;
            }

            if (secret.Trim().Length < PasswordMinLength || secret.Length > PasswordMaxLength)
            {
                return "La password deve avere da " + PasswordMinLength + " a " + PasswordMaxLength + " caratteri.";
            }

            return null;
        }

        public static LockCredential Create(CredentialKind kind, string secret, int iterations = DefaultIterations)
        {
            var problem = Validate(kind, secret);
            if (problem != null)
            {
                throw new ArgumentException(problem, nameof(secret));
            }

            var salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return new LockCredential
            {
                Kind = kind,
                Salt = Convert.ToBase64String(salt),
                Iterations = iterations,
                Hash = Convert.ToBase64String(Derive(secret, salt, iterations))
            };
        }

        /// <summary>Attesa prima di poter riprovare dopo <paramref name="failures"/> errori consecutivi.</summary>
        public static TimeSpan DelayFor(int failures)
        {
            if (failures < FreeAttempts)
            {
                return TimeSpan.Zero;
            }

            var seconds = BaseDelaySeconds * Math.Pow(2, Math.Min(failures - FreeAttempts, 10));
            return TimeSpan.FromSeconds(Math.Min(seconds, MaxDelaySeconds));
        }

        public TimeSpan RemainingLock(DateTime utcNow)
        {
            if (LockedUntilTicks <= 0)
            {
                return TimeSpan.Zero;
            }

            var remaining = new DateTime(LockedUntilTicks, DateTimeKind.Utc) - utcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>Verifica il PIN o la password e aggiorna il conteggio degli errori (il chiamante deve poi salvare).</summary>
        public CredentialCheckResult Check(string secret, DateTime utcNow)
        {
            var remaining = RemainingLock(utcNow);
            if (remaining > TimeSpan.Zero)
            {
                return new CredentialCheckResult(CredentialCheck.LockedOut, remaining, 0);
            }

            if (Matches(secret))
            {
                FailedAttempts = 0;
                LockedUntilTicks = 0;
                return new CredentialCheckResult(CredentialCheck.Correct, TimeSpan.Zero, FreeAttempts);
            }

            FailedAttempts++;
            var delay = DelayFor(FailedAttempts);
            if (delay > TimeSpan.Zero)
            {
                LockedUntilTicks = (utcNow + delay).Ticks;
                return new CredentialCheckResult(CredentialCheck.Wrong, delay, 0);
            }

            return new CredentialCheckResult(CredentialCheck.Wrong, TimeSpan.Zero, FreeAttempts - FailedAttempts);
        }

        public bool Matches(string secret)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(Salt) || string.IsNullOrEmpty(Hash) || Iterations <= 0)
            {
                return false;
            }

            var actual = Derive(secret, Convert.FromBase64String(Salt), Iterations);
            var expected = Convert.FromBase64String(Hash);
            if (actual.Length != expected.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                difference |= actual[i] ^ expected[i];   // confronto a tempo costante
            }

            return difference == 0;
        }

        private static byte[] Derive(string secret, byte[] salt, int iterations)
        {
            // PBKDF2-HMAC-SHA256 (RFC 8018) scritto a mano: netstandard2.0 non offre Rfc2898DeriveBytes con SHA-256.
            // HashLength deve essere al massimo 32 byte (un solo blocco).
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var input = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
                input[salt.Length + 3] = 1;   // indice del blocco (big endian)

                var u = hmac.ComputeHash(input);
                var result = (byte[])u.Clone();
                for (var i = 1; i < iterations; i++)
                {
                    u = hmac.ComputeHash(u);
                    for (var j = 0; j < result.Length; j++)
                    {
                        result[j] ^= u[j];
                    }
                }

                var output = new byte[HashLength];
                Buffer.BlockCopy(result, 0, output, 0, HashLength);
                return output;
            }
        }
    }
}
