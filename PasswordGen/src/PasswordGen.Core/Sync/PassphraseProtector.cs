using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PasswordGen.Core.History;
using PasswordGen.Core.Security;

namespace PasswordGen.Core.Sync
{
    /// <summary>
    /// Cifratura con una frase segreta, per il file di scambio tra le due app (sincronizzazione, esportazione, importazione).
    /// Formato: "PGX1" + iterazioni (4 byte) + sale (16 byte) + dati cifrati con AES-256-CBC e HMAC-SHA256 (<see cref="AesHmacProtector"/>).
    /// La chiave deriva dalla frase con PBKDF2-SHA256 e un sale casuale diverso a ogni scrittura.
    /// </summary>
    public sealed class PassphraseProtector : ISecretProtector
    {
        public const int DefaultIterations = 150000;
        public const int MinIterations = 1000;
        public const int MaxIterations = 2000000;

        private const int SaltLength = 16;
        private static readonly byte[] Magic = { (byte)'P', (byte)'G', (byte)'X', (byte)'1' };
        private const int HeaderLength = 4 + 4 + SaltLength;

        private readonly string _passphrase;
        private readonly int _iterations;

        public PassphraseProtector(string passphrase, int iterations = DefaultIterations)
        {
            if (string.IsNullOrEmpty(passphrase))
            {
                throw new ArgumentException("La frase segreta è vuota.", nameof(passphrase));
            }

            if (iterations < MinIterations || iterations > MaxIterations)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations));
            }

            _passphrase = passphrase;
            _iterations = iterations;
        }

        public byte[] Protect(byte[] data)
        {
            var salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var payload = new AesHmacProtector(DeriveKey(_passphrase, salt, _iterations)).Protect(data);
            var result = new byte[HeaderLength + payload.Length];
            Buffer.BlockCopy(Magic, 0, result, 0, 4);
            result[4] = (byte)(_iterations >> 24);
            result[5] = (byte)(_iterations >> 16);
            result[6] = (byte)(_iterations >> 8);
            result[7] = (byte)_iterations;
            Buffer.BlockCopy(salt, 0, result, 8, SaltLength);
            Buffer.BlockCopy(payload, 0, result, HeaderLength, payload.Length);
            return result;
        }

        /// <exception cref="InvalidDataException">Il file non è un file di scambio di PasswordGen.</exception>
        /// <exception cref="CryptographicException">Frase segreta errata o file alterato.</exception>
        public byte[] Unprotect(byte[] data)
        {
            if (data == null || data.Length < HeaderLength)
            {
                throw new InvalidDataException("Il file non è un file di scambio di PasswordGen.");
            }

            for (var i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i])
                {
                    throw new InvalidDataException("Il file non è un file di scambio di PasswordGen.");
                }
            }

            var iterations = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
            if (iterations < MinIterations || iterations > MaxIterations)
            {
                throw new InvalidDataException("Il file di scambio non è valido.");
            }

            var salt = new byte[SaltLength];
            Buffer.BlockCopy(data, 8, salt, 0, SaltLength);
            var payload = new byte[data.Length - HeaderLength];
            Buffer.BlockCopy(data, HeaderLength, payload, 0, payload.Length);

            return new AesHmacProtector(DeriveKey(_passphrase, salt, iterations)).Unprotect(payload);
        }

        /// <summary>Chiave di 64 byte: PBKDF2 una sola volta, poi due sottochiavi (cifratura e MAC) con HMAC.</summary>
        private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations)
        {
            var master = Pbkdf2.Derive(passphrase, salt, iterations, 32);
            var key = new byte[AesHmacProtector.KeyLength];
            using (var hmac = new HMACSHA256(master))
            {
                Buffer.BlockCopy(hmac.ComputeHash(Encoding.ASCII.GetBytes("PasswordGen.enc")), 0, key, 0, 32);
                Buffer.BlockCopy(hmac.ComputeHash(Encoding.ASCII.GetBytes("PasswordGen.mac")), 0, key, 32, 32);
            }

            return key;
        }
    }
}
