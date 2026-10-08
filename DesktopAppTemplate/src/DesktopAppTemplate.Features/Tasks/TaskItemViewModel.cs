using System;
using System.Globalization;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Riga dell'elenco attività (sola lettura: dopo ogni comando l'elenco viene ricaricato).</summary>
    public sealed class TaskItemViewModel
    {
        public TaskItemViewModel(TaskItem item)
        {
            Id = item.Id;
            Title = item.Title;
            IsCompleted = item.IsCompleted;
            CreatedText = item.CreatedAt.ToString("g", CultureInfo.CurrentCulture);
        }

        public Guid Id { get; }
        public string Title { get; }
        public bool IsCompleted { get; }
        public string CreatedText { get; }
    }
}
