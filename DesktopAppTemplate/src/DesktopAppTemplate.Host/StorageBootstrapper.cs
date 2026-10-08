using System;
using System.Threading;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Data;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Prepara l'archivio all'avvio: applica le migrazioni dello schema e, se il database è appena stato creato, inserisce le
    /// attività di esempio. Con <c>--no-migrate</c> non modifica nulla: controlla solo che lo schema esista già.
    /// </summary>
    internal static class StorageBootstrapper
    {
        /// <param name="migrate">False con <c>--no-migrate</c>: nessuna migrazione né dati di esempio, solo il controllo che lo schema esista.</param>
        /// <returns>Il risultato della migrazione, oppure null se non è stata eseguita.</returns>
        public static MigrationResult Initialize(IServiceProvider provider, bool migrate = true)
        {
            if (!migrate)
            {
                var executor = provider.GetService<IDbExecutor>();
                if (executor != null)
                    SchemaVerifier.VerifyAsync(executor).GetAwaiter().GetResult();
                return null;
            }

            var migrator = provider.GetService<IDatabaseMigrator>();
            if (migrator == null)
                return null;

            // Siamo prima dell'avvio dell'interfaccia: nessun contesto di sincronizzazione, l'attesa sincrona è sicura.
            var result = migrator.MigrateAsync().GetAwaiter().GetResult();
            if (!result.IsNewDatabase)
                return result;

            var repository = provider.GetRequiredService<ITaskRepository>();
            var clock = provider.GetRequiredService<IClock>();
            foreach (var sample in SampleTasks.Create(clock.Now))
                repository.AddAsync(sample, CancellationToken.None).GetAwaiter().GetResult();

            return result;
        }
    }
}
