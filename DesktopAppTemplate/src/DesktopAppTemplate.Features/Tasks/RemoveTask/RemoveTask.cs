using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Messaging;

namespace DesktopAppTemplate.Features.Tasks.RemoveTask
{
    /// <summary>Elimina un'attività.</summary>
    public sealed class RemoveTaskCommand : IRequest<Unit>
    {
        public RemoveTaskCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }

    public sealed class RemoveTaskHandler : IRequestHandler<RemoveTaskCommand, Unit>
    {
        private readonly ITaskRepository _repository;

        public RemoveTaskHandler(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(RemoveTaskCommand request, CancellationToken cancellationToken)
        {
            await _repository.RemoveAsync(request.Id, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
