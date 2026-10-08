using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Messaging;

namespace DesktopAppTemplate.Features.Tasks.ToggleTask
{
    /// <summary>Inverte lo stato "completata" di un'attività.</summary>
    public sealed class ToggleTaskCommand : IRequest<TaskItem>
    {
        public ToggleTaskCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }

    public sealed class ToggleTaskHandler : IRequestHandler<ToggleTaskCommand, TaskItem>
    {
        private readonly ITaskRepository _repository;

        public ToggleTaskHandler(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<TaskItem> Handle(ToggleTaskCommand request, CancellationToken cancellationToken)
        {
            var item = await _repository.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (item == null)
                throw new InvalidOperationException("L'attività non esiste più.");

            var updated = item.WithCompleted(!item.IsCompleted);
            await _repository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            return updated;
        }
    }
}
