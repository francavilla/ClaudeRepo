using System.Collections.Generic;

namespace SolutionDoctor.Core.Git
{
    /// <summary>Fornisce la frequenza di modifica dei file (quanti commit li hanno toccati).</summary>
    public interface IChurnProvider
    {
        /// <summary>
        /// Commit per file negli ultimi <paramref name="months"/> mesi, con percorsi relativi a <paramref name="directory"/>
        /// e '/' come separatore. Null se l'informazione non è disponibile (git assente, non è un repository).
        /// </summary>
        IDictionary<string, int> TryGetChurn(string directory, int months);
    }
}
