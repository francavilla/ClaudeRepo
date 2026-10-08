using PasswordGen.Core.Randomness;

namespace PasswordGen.Core.Tests
{
    /// <summary>Sorgente deterministica per i test: restituisce valori crescenti (modulo il limite).</summary>
    internal sealed class CounterRandom : IRandomSource
    {
        private int _next;

        public CounterRandom(int start = 0)
        {
            _next = start;
        }

        public int Next(int maxExclusive)
        {
            return _next++ % maxExclusive;
        }
    }
}
