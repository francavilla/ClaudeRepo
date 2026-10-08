using System;
using DesktopAppTemplate.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    public static class LoggingServiceCollectionExtensions
    {
        /// <summary>
        /// Registra il logging (<see cref="ILogger{TCategoryName}"/>) con le destinazioni scelte. Con la destinazione database serve
        /// che sia registrato anche <see cref="IDbExecutor"/> (lo fa <c>AddDatabase</c>) e che, dopo le migrazioni, si chiami
        /// <see cref="StartDeferredLogSinks"/>.
        /// </summary>
        /// <param name="logFolder">Cartella dei file di log (e del file di ripiego del database).</param>
        public static IServiceCollection AddAppLogging(this IServiceCollection services, LoggingSettings settings, string logFolder)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            services.AddLogging(builder => builder.SetMinimumLevel(settings.Level));

            if (settings.Level == LogLevel.None || settings.Targets == LogTargets.None)
                return services;

            if ((settings.Targets & LogTargets.File) != 0)
                services.AddSingleton<ILoggerProvider>(provider => new FileLoggerProvider(logFolder, settings.Level, settings.RetentionDays));

            if ((settings.Targets & LogTargets.Database) != 0)
            {
                services.AddSingleton(provider => new DbLoggerProvider(
                    provider.GetRequiredService<IDbExecutor>(),
                    settings.Level,
                    new LogFileWriter(logFolder, "app-fallback", settings.RetentionDays),
                    settings.RetentionDays));
                services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<DbLoggerProvider>());
                services.AddSingleton<IDeferredStart>(provider => provider.GetRequiredService<DbLoggerProvider>());
            }

            return services;
        }

        /// <summary>Fa iniziare a scrivere le destinazioni in attesa (il database), dopo che lo schema è pronto.</summary>
        public static void StartDeferredLogSinks(this IServiceProvider provider)
        {
            foreach (var sink in provider.GetServices<IDeferredStart>())
                sink.Start();
        }
    }
}
