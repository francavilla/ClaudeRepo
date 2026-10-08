namespace PasswordGen.Core.Generation
{
    public sealed class GeneratedPassword
    {
        public GeneratedPassword(string text, double entropyBits, GenerationMode mode)
        {
            Text = text;
            EntropyBits = entropyBits;
            Mode = mode;
        }

        public string Text { get; private set; }

        /// <summary>Stima dell'entropia in bit (logaritmo del numero di password possibili con le stesse scelte).</summary>
        public double EntropyBits { get; private set; }

        public GenerationMode Mode { get; private set; }

        public StrengthLevel Level
        {
            get { return PasswordStrength.Classify(EntropyBits); }
        }
    }
}
