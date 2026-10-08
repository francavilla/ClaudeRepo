using System;
using System.Threading;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Data;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Prepara l'archivio all'avvio: con un database applica le migrazioni dello schema e, se il database è appena stato
    /// creato, inserisce le attività di esempio. Con l'archivio su file non fa nulla (se ne occupa il repository).
    /// </summary>
    internal static class StorageBootstrapper
    {
        public static void Initialize(IServiceProvider provider)
        {
            var migrator = provider.GetService<IDatabaseMigrator>();
            if (migrator == null)
                return;

            // Siamo prima dell'avvio dell'interfaccia: nessun contesto di sincronizzazione, l'attesa sincrona è sicura.
            var result = migrator.MigrateAsync().GetAwaiter().GetResult();
            if (!result.IsNewDatabase)
                return;

            var repository = provider.GetRequiredService<ITaskRepository>();
            var clock = provider.GetRequiredService<IClock>();
            foreach (var sample in SampleTasks.Create(clock.Now))
                repository.AddAsync(sample, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
