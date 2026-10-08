using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data
{
    /// <summary>
    /// Controlla che le tabelle necessarie esistano e siano leggibili. Si usa quando le migrazioni sono disattivate
    /// (<c>--no-migrate</c>): meglio un messaggio chiaro all'avvio che un errore più tardi, alla prima operazione.
    /// </summary>
    public static class SchemaVerifier
    {
        public static async Task VerifyAsync(IDbExecutor executor, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // Non legge righe: serve solo a verificare che la tabella esista e sia accessibile a questo utente.
                await executor.QueryAsync("SELECT 1 FROM Tasks WHERE 1 = 0", null, record => 0, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Con --no-migrate lo schema del database deve già esistere, ma la tabella Tasks non è accessibile (" + ex.Message + "). " +
                    "Fai eseguire gli script di Data/Scripts per il tuo database oppure avvia l'app senza --no-migrate.", ex);
            }
        }
    }
}
