using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>Archivio in memoria (thread-safe). Sostituibile con file, SQLite, ecc. senza toccare le slice.</summary>
    public sealed class InMemoryTaskRepository : ITaskRepository
    {
        private readonly object _gate = new object();
        private readonly List<TaskItem> _items = new List<TaskItem>();

        /// <summary>Inserisce alcune attività di esempio.</summary>
        public void Seed(DateTime now)
        {
            lock (_gate)
            {
                _items.Add(new TaskItem(Guid.NewGuid(), "Provare l'interfaccia WPF e Windows Forms", now.AddMinutes(-30)));
                _items.Add(new TaskItem(Guid.NewGuid(), "Aggiungere una nuova slice in Features", now.AddMinutes(-20)));
                _items.Add(new TaskItem(Guid.NewGuid(), "Leggere il README", now.AddMinutes(-10), true));
            }
        }

        public Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult((IReadOnlyList<TaskItem>)_items.ToList());
            }
        }

        public Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_items.FirstOrDefault(t => t.Id == id));
            }
        }

        public Task AddAsync(TaskItem item, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _items.Add(item);
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(TaskItem item, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var index = _items.FindIndex(t => t.Id == item.Id);
                if (index >= 0)
                    _items[index] = item;
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _items.RemoveAll(t => t.Id == id);
            }

            return Task.CompletedTask;
        }
    }
}
