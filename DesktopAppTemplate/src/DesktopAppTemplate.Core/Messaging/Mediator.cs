using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>
    /// Mediator minimale: valida la richiesta (tutti gli <see cref="IValidator{TRequest}"/> registrati)
    /// e la inoltra all'unico <see cref="IRequestHandler{TRequest,TResponse}"/> registrato.
    /// </summary>
    public sealed class Mediator : IMediator
    {
        private readonly IServiceProvider _provider;

        public Mediator(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Il tipo concreto della richiesta si conosce solo a runtime: l'invoker generico mantiene tutto tipizzato.
            var invokerType = typeof(Invoker<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var invoker = (InvokerBase<TResponse>)Activator.CreateInstance(invokerType);
            return invoker.Invoke(request, _provider, cancellationToken);
        }

        private abstract class InvokerBase<TResponse>
        {
            public abstract Task<TResponse> Invoke(object request, IServiceProvider provider, CancellationToken cancellationToken);
        }

        private sealed class Invoker<TRequest, TResponse> : InvokerBase<TResponse> where TRequest : IRequest<TResponse>
        {
            public override async Task<TResponse> Invoke(object request, IServiceProvider provider, CancellationToken cancellationToken)
            {
                var typed = (TRequest)request;

                var validators = (IEnumerable<IValidator<TRequest>>)provider.GetService(typeof(IEnumerable<IValidator<TRequest>>))
                                 ?? Enumerable.Empty<IValidator<TRequest>>();
                var errors = validators.SelectMany(v => v.Validate(typed)).ToList();
                if (errors.Count > 0)
                    throw new ValidationException(errors);

                var handler = (IRequestHandler<TRequest, TResponse>)provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));
                if (handler == null)
                    throw new InvalidOperationException($"Nessun handler registrato per la richiesta {typeof(TRequest).Name}.");

                return await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
