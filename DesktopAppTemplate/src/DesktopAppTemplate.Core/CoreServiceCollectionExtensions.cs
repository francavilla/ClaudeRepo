using System;
using System.Reflection;
using DesktopAppTemplate.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesktopAppTemplate.Core
{
    public static class CoreServiceCollectionExtensions
    {
        /// <summary>Registra il mediator.</summary>
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IMediator, Mediator>();
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
