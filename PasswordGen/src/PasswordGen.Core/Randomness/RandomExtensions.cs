using System;
using System.Collections.Generic;

namespace PasswordGen.Core.Randomness
{
    public static class RandomExtensions
    {
        /// <summary>Un carattere a caso della stringa.</summary>
        public static char Pick(this IRandomSource random, string characters)
        {
            if (string.IsNullOrEmpty(characters))
            {
                throw new ArgumentException("L'insieme di caratteri è vuoto.", nameof(characters));
            }

            return characters[random.Next(characters.Length)];
        }

        /// <summary>Un elemento a caso dell'elenco.</summary>
        public static T Pick<T>(this IRandomSource random, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("L'elenco è vuoto.", nameof(items));
            }

            return items[random.Next(items.Count)];
        }

        /// <summary>Mescolamento di Fisher-Yates (ogni permutazione ha la stessa probabilità).</summary>
        public static void Shuffle<T>(this IRandomSource random, IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
