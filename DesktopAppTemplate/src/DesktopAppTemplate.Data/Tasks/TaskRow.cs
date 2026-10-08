namespace DesktopAppTemplate.Data.Tasks
{
    /// <summary>
    /// Riga della tabella Tasks, identica per tutte le tecnologie di accesso. Tipi volutamente "semplici" e uguali
    /// in SQLite e SQL Server (testo e interi): niente conversioni dipendenti dal driver.
    /// </summary>
    public sealed class TaskRow
    {
        /// <summary>Guid in formato testo "D".</summary>
        public string Id { get; set; }

        public string Title { get; set; }

        /// <summary>Data in formato ISO 8601 ("o"), per mantenere l'ordinamento e l'offset.</summary>
        public string CreatedAt { get; set; }

        /// <summary>0 = da fare, 1 = completata.</summary>
        public long IsCompleted { get; set; }
    }
}
