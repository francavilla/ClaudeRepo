using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>
    /// Mediator minimale: valida la richiesta (tutti gli <see cref="IValidator{TRequest}"/> registrati)
    /// e la inoltra all'unico <see cref="IRequestHandler{TRequest,TResponse}"/> registrato. Registra nel log ogni richiesta
    /// (Debug: eseguita, con la durata; Warning: non valida; Error: fallita) se è disponibile un <see cref="ILogger{TCategoryName}"/>.
    /// </summary>
    public sealed class Mediator : IMediator
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        public Mediator(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            // Il log è facoltativo: senza AddLogging (es. in alcuni test) non scrive nulla.
            _logger = provider.GetService(typeof(ILogger<Mediator>)) as ILogger ?? NullLogger.Instance;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Il tipo concreto della richiesta si conosce solo a runtime: l'invoker generico mantiene tutto tipizzato.
            var invokerType = typeof(Invoker<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var invoker = (InvokerBase<TResponse>)Activator.CreateInstance(invokerType);
            return invoker.Invoke(request, _provider, _logger, cancellationToken);
        }

        private abstract class InvokerBase<TResponse>
        {
            public abstract Task<TResponse> Invoke(object request, IServiceProvider provider, ILogger logger, CancellationToken cancellationToken);
        }

        private sealed class Invoker<TRequest, TResponse> : InvokerBase<TResponse> where TRequest : IRequest<TResponse>
        {
            public override async Task<TResponse> Invoke(object request, IServiceProvider provider, ILogger logger, CancellationToken cancellationToken)
            {
                var name = typeof(TRequest).Name;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var typed = (TRequest)request;

                    var validators = (IEnumerable<IValidator<TRequest>>)provider.GetService(typeof(IEnumerable<IValidator<TRequest>>))
                                     ?? Enumerable.Empty<IValidator<TRequest>>();
                    var errors = validators.SelectMany(v => v.Validate(typed)).ToList();
                    if (errors.Count > 0)
                    {
                        logger.LogWarning("Richiesta {Request} non valida: {Errors}", name, string.Join(" ", errors));
                        throw new ValidationException(errors);
                    }

                    var handler = (IRequestHandler<TRequest, TResponse>)provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));
                    if (handler == null)
                        throw new InvalidOperationException($"Nessun handler registrato per la richiesta {name}.");

                    var response = await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
                    logger.LogDebug("Richiesta {Request} eseguita in {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
                    return response;
                }
                catch (ValidationException)
                {
                    throw;   // già registrata come avviso
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Richiesta {Request} fallita dopo {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }
}
