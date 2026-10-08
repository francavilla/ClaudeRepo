namespace PasswordGen.Core.Generation
{
    public enum GenerationMode
    {
        /// <summary>Parole italiane casuali: Lampo-Cavallo-Nebbia-Fiume47!</summary>
        Passphrase = 0,

        /// <summary>Sillabe pronunciabili: Bamelo-Tirusa-Pevo83=</summary>
        Syllables = 1,

        /// <summary>Caratteri completamente casuali: k7Q#mP2v!xR4</summary>
        Random = 2
    }
}
