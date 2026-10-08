using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Messaging;

namespace DesktopAppTemplate.Features.Tasks.AddTask
{
    /// <summary>Crea una nuova attività.</summary>
    public sealed class AddTaskCommand : IRequest<TaskItem>
    {
        public AddTaskCommand(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    public sealed class AddTaskValidator : IValidator<AddTaskCommand>
    {
        public const int MaxTitleLength = 120;

        public IEnumerable<string> Validate(AddTaskCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                yield return "Il titolo dell'attività è obbligatorio.";
            else if (request.Title.Trim().Length > MaxTitleLength)
                yield return $"Il titolo non può superare {MaxTitleLength} caratteri.";
        }
    }

    public sealed class AddTaskHandler : IRequestHandler<AddTaskCommand, TaskItem>
    {
        private readonly ITaskRepository _repository;
        private readonly IClock _clock;

        public AddTaskHandler(ITaskRepository repository, IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        public async Task<TaskItem> Handle(AddTaskCommand request, CancellationToken cancellationToken)
        {
            var item = new TaskItem(Guid.NewGuid(), request.Title.Trim(), _clock.Now);
            await _repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
            return item;
        }
    }
}
