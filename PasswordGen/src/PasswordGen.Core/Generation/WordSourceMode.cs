namespace PasswordGen.Core.Generation
{
    /// <summary>Da dove vengono prese le parole delle passphrase.</summary>
    public enum WordSourceMode
    {
        /// <summary>Solo la lista italiana integrata.</summary>
        Builtin = 0,

        /// <summary>Lista integrata più le parole del file dell'utente.</summary>
        Combined = 1,

        /// <summary>Solo le parole del file dell'utente.</summary>
        CustomOnly = 2
    }
}
