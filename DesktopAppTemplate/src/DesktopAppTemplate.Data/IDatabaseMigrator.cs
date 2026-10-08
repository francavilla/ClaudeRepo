using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Data
{
    /// <summary>Risultato di una migrazione dello schema.</summary>
    public sealed class MigrationResult
    {
        public MigrationResult(int previousVersion, int appliedCount)
        {
            PreviousVersion = previousVersion;
            AppliedCount = appliedCount;
        }

        /// <summary>Versione dello schema prima della migrazione (0 = database nuovo).</summary>
        public int PreviousVersion { get; }

        /// <summary>Numero di script applicati in questa esecuzione.</summary>
        public int AppliedCount { get; }

        /// <summary>True se il database è stato creato adesso (utile per inserire i dati iniziali).</summary>
        public bool IsNewDatabase => PreviousVersion == 0 && AppliedCount > 0;
    }

    /// <summary>Crea e aggiorna lo schema applicando, in ordine, gli script non ancora eseguiti.</summary>
    public interface IDatabaseMigrator
    {
        Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
