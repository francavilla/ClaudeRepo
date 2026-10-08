using System;
using System.Reflection;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Context;
using DesktopAppTemplate.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesktopAppTemplate.Core
{
    public static class CoreServiceCollectionExtensions
    {
        /// <summary>Registra il mediator.</summary>
        public static IServiceCollection AddMediator(this IServiceCollection services)
        {
            services.TryAddSingleton<IMediator, Mediator>();
            return services;
        }

        /// <summary>
        /// Registra la configurazione e il contesto dell'applicazione. Richiede che siano registrati anche
        /// <see cref="DesktopAppTemplate.Core.Abstractions.IAppInfo"/> e <see cref="DesktopAppTemplate.Core.Abstractions.IUiDescriptor"/>
        /// (lo fanno l'Infrastructure e la UI scelta).
        /// </summary>
        public static IServiceCollection AddAppContext(this IServiceCollection services, IAppConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.AddSingleton<IAppConfiguration>(configuration);
            services.AddSingleton<ISessionState, SessionState>();
            services.AddSingleton<IAppEnvironment, AppEnvironment>();
            services.AddSingleton<IAppContext, DefaultAppContext>();
            return services;
        }

        /// <summary>
        /// Registra automaticamente tutti gli <see cref="IRequestHandler{TRequest,TResponse}"/> e gli
        /// <see cref="IValidator{TRequest}"/> dell'assembly: una nuova slice non richiede registrazioni manuali.
        /// </summary>
        public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                foreach (var contract in type.GetInterfaces())
                {
                    if (!contract.IsGenericType)
                        continue;

                    var definition = contract.GetGenericTypeDefinition();
                    if (definition == typeof(IRequestHandler<,>) || definition == typeof(IValidator<>))
                        services.AddTransient(contract, type);
                }
            }

            return services;
        }
    }
}
