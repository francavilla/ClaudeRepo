using System;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Attività da svolgere (immutabile: le modifiche producono una nuova istanza).</summary>
    public sealed class TaskItem
    {
        public TaskItem(Guid id, string title, DateTime createdAt, bool isCompleted = false)
        {
            Id = id;
            Title = title;
            CreatedAt = createdAt;
            IsCompleted = isCompleted;
        }

        public Guid Id { get; }
        public string Title { get; }
        public DateTime CreatedAt { get; }
        public bool IsCompleted { get; }

        public TaskItem WithCompleted(bool isCompleted) => new TaskItem(Id, Title, CreatedAt, isCompleted);
    }
}
