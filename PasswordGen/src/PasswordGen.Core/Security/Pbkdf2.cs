using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGen.Core.Security
{
    /// <summary>
    /// PBKDF2-HMAC-SHA256 (RFC 8018) scritto a mano: netstandard2.0 non offre <c>Rfc2898DeriveBytes</c> con SHA-256.
    /// Il risultato è identico a quello delle implementazioni standard.
    /// </summary>
    internal static class Pbkdf2
    {
        private const int BlockLength = 32;

        public static byte[] Derive(string secret, byte[] salt, int iterations, int length)
        {
            if (iterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations));
            }

            var output = new byte[length];
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var blocks = (length + BlockLength - 1) / BlockLength;
                for (var block = 1; block <= blocks; block++)
                {
                    var input = new byte[salt.Length + 4];
                    Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
                    input[salt.Length] = (byte)(block >> 24);
                    input[salt.Length + 1] = (byte)(block >> 16);
                    input[salt.Length + 2] = (byte)(block >> 8);
                    input[salt.Length + 3] = (byte)block;   // indice del blocco (big endian)

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

                    var offset = (block - 1) * BlockLength;
                    Buffer.BlockCopy(result, 0, output, offset, Math.Min(BlockLength, length - offset));
                }
            }

            return output;
        }
    }
}
