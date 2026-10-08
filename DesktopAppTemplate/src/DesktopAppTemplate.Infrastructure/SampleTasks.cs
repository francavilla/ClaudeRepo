using System;
using System.Collections.Generic;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>Attività di esempio con cui parte l'applicazione al primo avvio.</summary>
    public static class SampleTasks
    {
        public static IReadOnlyList<TaskItem> Create(DateTime now)
        {
            return new List<TaskItem>
            {
                new TaskItem(Guid.NewGuid(), "Provare l'interfaccia WPF e Windows Forms", now.AddMinutes(-30)),
                new TaskItem(Guid.NewGuid(), "Aggiungere una nuova slice in Features", now.AddMinutes(-20)),
                new TaskItem(Guid.NewGuid(), "Leggere il README", now.AddMinutes(-10), true)
            };
        }
    }
}
