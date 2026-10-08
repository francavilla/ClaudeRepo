namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>Risposta "vuota" per i comandi che non restituiscono alcun valore.</summary>
    public struct Unit
    {
        public static readonly Unit Value = new Unit();
    }
}
