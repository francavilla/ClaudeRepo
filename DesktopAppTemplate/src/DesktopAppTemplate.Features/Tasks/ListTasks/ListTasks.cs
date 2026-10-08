using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core.Messaging;

namespace DesktopAppTemplate.Features.Tasks.ListTasks
{
    /// <summary>Elenca le attività secondo il filtro indicato.</summary>
    public sealed class ListTasksQuery : IRequest<TaskListResult>
    {
        public ListTasksQuery(TaskFilter filter)
        {
            Filter = filter;
        }

        public TaskFilter Filter { get; }
    }

    public sealed class TaskListResult
    {
        public TaskListResult(IReadOnlyList<TaskItem> items, int totalCount, int completedCount)
        {
            Items = items;
            TotalCount = totalCount;
            CompletedCount = completedCount;
        }

        /// <summary>Attività che rispettano il filtro.</summary>
        public IReadOnlyList<TaskItem> Items { get; }

        /// <summary>Totale delle attività, indipendentemente dal filtro.</summary>
        public int TotalCount { get; }

        public int CompletedCount { get; }
    }

    public sealed class ListTasksHandler : IRequestHandler<ListTasksQuery, TaskListResult>
    {
        private readonly ITaskRepository _repository;

        public ListTasksHandler(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<TaskListResult> Handle(ListTasksQuery request, CancellationToken cancellationToken)
        {
            var all = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

            IEnumerable<TaskItem> filtered = all;
            if (request.Filter == TaskFilter.Active)
                filtered = all.Where(t => !t.IsCompleted);
            else if (request.Filter == TaskFilter.Completed)
                filtered = all.Where(t => t.IsCompleted);

            // Prima le attività aperte, poi le completate; a parità, le più recenti in alto.
            var items = filtered
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();

            return new TaskListResult(items, all.Count, all.Count(t => t.IsCompleted));
        }
    }
}
