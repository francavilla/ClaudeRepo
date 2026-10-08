using System;
using System.Security.Cryptography;

namespace PasswordGen.Core.History
{
    /// <summary>
    /// Cifratura portabile (AES-256-CBC con IV casuale + HMAC-SHA256, "encrypt-then-MAC") con una chiave di 64 byte
    /// fornita da fuori. Su Android la chiave sta nel Keystore del sistema (SecureStorage); qui non si legge né si salva alcuna chiave.
    /// Un file alterato o decifrato con la chiave sbagliata viene rifiutato con <see cref="CryptographicException"/>.
    /// </summary>
    public sealed class AesHmacProtector : ISecretProtector
    {
        public const int KeyLength = 64;

        private const int AesKeyLength = 32;
        private const int IvLength = 16;
        private const int MacLength = 32;

        private readonly byte[] _encryptionKey = new byte[AesKeyLength];
        private readonly byte[] _macKey = new byte[AesKeyLength];

        public AesHmacProtector(byte[] key)
        {
            if (key == null || key.Length != KeyLength)
            {
                throw new ArgumentException("La chiave deve essere di " + KeyLength + " byte.", nameof(key));
            }

            Buffer.BlockCopy(key, 0, _encryptionKey, 0, AesKeyLength);
            Buffer.BlockCopy(key, AesKeyLength, _macKey, 0, AesKeyLength);
        }

        /// <summary>Genera una chiave casuale di <see cref="KeyLength"/> byte.</summary>
        public static byte[] GenerateKey()
        {
            var key = new byte[KeyLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            return key;
        }

        public byte[] Protect(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                byte[] cipher;
                using (var encryptor = aes.CreateEncryptor())
                {
                    cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
                }

                var result = new byte[IvLength + cipher.Length + MacLength];
                Buffer.BlockCopy(aes.IV, 0, result, 0, IvLength);
                Buffer.BlockCopy(cipher, 0, result, IvLength, cipher.Length);

                var mac = ComputeMac(result, IvLength + cipher.Length);
                Buffer.BlockCopy(mac, 0, result, IvLength + cipher.Length, MacLength);
                return result;
            }
        }

        public byte[] Unprotect(byte[] data)
        {
            // IV + almeno un blocco cifrato + MAC.
            if (data == null || data.Length < IvLength + 16 + MacLength)
            {
                throw new CryptographicException("Dati cifrati non validi.");
            }

            var macStart = data.Length - MacLength;
            var expected = ComputeMac(data, macStart);
            var difference = 0;
            for (var i = 0; i < MacLength; i++)
            {
                difference |= expected[i] ^ data[macStart + i];   // confronto a tempo costante
            }

            if (difference != 0)
            {
                throw new CryptographicException("I dati cifrati sono stati alterati o la chiave non è quella giusta.");
            }

            var iv = new byte[IvLength];
            Buffer.BlockCopy(data, 0, iv, 0, IvLength);

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(data, IvLength, macStart - IvLength);
                }
            }
        }

        private byte[] ComputeMac(byte[] data, int count)
        {
            using (var hmac = new HMACSHA256(_macKey))
            {
                return hmac.ComputeHash(data, 0, count);
            }
        }
    }
}
