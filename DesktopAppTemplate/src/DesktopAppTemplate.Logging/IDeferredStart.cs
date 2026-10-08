namespace DesktopAppTemplate.Logging
{
    /// <summary>
    /// Destinazione di log che inizia a scrivere solo quando l'applicazione lo decide (es. il database, dopo le migrazioni).
    /// Nel frattempo i messaggi restano in coda: non si perde nulla di ciò che accade all'avvio.
    /// </summary>
    public interface IDeferredStart
    {
        void Start();
    }
}
