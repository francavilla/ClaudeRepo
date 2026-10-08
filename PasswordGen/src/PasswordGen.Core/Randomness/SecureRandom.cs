using System;
using System.Security.Cryptography;

namespace PasswordGen.Core.Randomness
{
    /// <summary>
    /// Generatore crittograficamente sicuro (<see cref="RandomNumberGenerator"/> del sistema operativo).
    /// La scelta è uniforme: i valori che causerebbero un bias (modulo) vengono scartati e ripescati.
    /// </summary>
    public sealed class SecureRandom : IRandomSource, IDisposable
    {
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Il limite deve essere positivo.");
            }

            if (maxExclusive == 1)
            {
                return 0;
            }

            const ulong range = 1UL << 32;
            var max = (ulong)maxExclusive;
            var accept = range - (range % max);
            var buffer = new byte[4];

            while (true)
            {
                _rng.GetBytes(buffer);
                ulong value = BitConverter.ToUInt32(buffer, 0);
                if (value < accept)
                {
                    return (int)(value % max);
                }
            }
        }

        public void Dispose()
        {
            _rng.Dispose();
        }
    }
}
