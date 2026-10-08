using PasswordGen.Core.Policy;

namespace PasswordGen.Core.Generation
{
    public sealed class GenerationOptions
    {
        public const int MinWords = 3;
        public const int MaxWords = 8;
        public const int MinSyllables = 6;
        public const int MaxSyllables = 15;
        public const int MaxRandomLength = 64;

        public GenerationMode Mode { get; set; } = GenerationMode.Passphrase;
        public PasswordPolicy Policy { get; set; } = new PasswordPolicy();

        /// <summary>Numero di parole della passphrase.</summary>
        public int WordCount { get; set; } = 4;

        /// <summary>Numero totale di sillabe della password pronunciabile.</summary>
        public int SyllableCount { get; set; } = 9;

        /// <summary>Lunghezza della password casuale.</summary>
        public int RandomLength { get; set; } = 16;

        /// <summary>
        /// Password attuale (facoltativa): le proposte sufficientemente simili vengono scartate.
        /// Resta solo in memoria e non viene mai salvata.
        /// </summary>
        public string PreviousPassword { get; set; }

        /// <summary>Altre password passate (per esempio dallo storico) da cui le proposte devono differire.</summary>
        public System.Collections.Generic.IReadOnlyCollection<string> PreviousPasswords { get; set; }
    }
}
