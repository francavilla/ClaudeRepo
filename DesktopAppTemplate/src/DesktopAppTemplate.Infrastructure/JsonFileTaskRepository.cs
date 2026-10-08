using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Features.Tasks;
using Newtonsoft.Json;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>
    /// Archivio delle attività su un file JSON. Il file viene letto una volta sola e a ogni modifica riscritto
    /// in modo sicuro (file temporaneo + sostituzione), così un'interruzione non lo lascia a metà.
    /// Pensato per un'istanza dell'applicazione alla volta: non coordina più processi che scrivono lo stesso file.
    /// </summary>
    public sealed class JsonFileTaskRepository : ITaskRepository
    {
        private const int FormatVersion = 1;

        private readonly string _filePath;
        private readonly Func<IReadOnlyList<TaskItem>> _initialItems;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private List<TaskItem> _items;

        /// <param name="filePath">Percorso del file JSON (la cartella viene creata se manca).</param>
        /// <param name="initialItems">Contenuto con cui partire se il file non esiste (es. dati di esempio); facoltativo.</param>
        public JsonFileTaskRepository(string filePath, Func<IReadOnlyList<TaskItem>> initialItems = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Il percorso del file è obbligatorio.", nameof(filePath));

            _filePath = filePath;
            _initialItems = initialItems;
        }

        public Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            return ReadAsync(items => (IReadOnlyList<TaskItem>)items.ToList(), cancellationToken);
        }

        public Task<TaskItem> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return ReadAsync(items => items.FirstOrDefault(t => t.Id == id), cancellationToken);
        }

        public Task AddAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return ChangeAsync(items =>
            {
                items.Add(item);
                return true;
            }, cancellationToken);
        }

        public Task UpdateAsync(TaskItem item, CancellationToken cancellationToken)
        {
            return ChangeAsync(items =>
            {
                var index = items.FindIndex(t => t.Id == item.Id);
                if (index < 0)
                    return false;

                items[index] = item;
                return true;
            }, cancellationToken);
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            return ChangeAsync(items => items.RemoveAll(t => t.Id == id) > 0, cancellationToken);
        }

        private async Task<T> ReadAsync<T>(Func<List<TaskItem>, T> read, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                return read(_items);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Applica la modifica a una copia e, solo se il salvataggio riesce, la rende effettiva.</summary>
        private async Task ChangeAsync(Func<List<TaskItem>, bool> change, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);

                var copy = _items.ToList();
                if (!change(copy))
                    return;

                await SaveAsync(copy).ConfigureAwait(false);
                _items = copy;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task EnsureLoadedAsync()
        {
            if (_items != null)
                return;

            if (File.Exists(_filePath))
            {
                _items = await LoadAsync().ConfigureAwait(false);
                return;
            }

            var initial = _initialItems?.Invoke();
            if (initial == null)
            {
                _items = new List<TaskItem>();
                return;
            }

            var seeded = initial.ToList();
            await SaveAsync(seeded).ConfigureAwait(false);
            _items = seeded;
        }

        private async Task<List<TaskItem>> LoadAsync()
        {
            string json;
            using (var reader = new StreamReader(_filePath, Encoding.UTF8))
                json = await reader.ReadToEndAsync().ConfigureAwait(false);

            try
            {
                var file = JsonConvert.DeserializeObject<TaskFile>(json);
                if (file == null || file.Tasks == null)
                    throw new JsonSerializationException("il contenuto non è nel formato atteso.");

                return file.Tasks.Select(r => new TaskItem(r.Id, r.Title ?? string.Empty, r.CreatedAt, r.IsCompleted)).ToList();
            }
            catch (JsonException ex)
            {
                // Il file non viene toccato: così si può correggere a mano o eliminare per ripartire da zero.
                throw new InvalidOperationException(
                    $"Il file dei dati \"{_filePath}\" non è valido ({ex.Message}). Correggilo oppure eliminalo per ripartire da zero.", ex);
            }
        }

        private async Task SaveAsync(List<TaskItem> items)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var file = new TaskFile
            {
                Version = FormatVersion,
                Tasks = items.Select(t => new TaskRecord { Id = t.Id, Title = t.Title, CreatedAt = t.CreatedAt, IsCompleted = t.IsCompleted }).ToList()
            };
            var json = JsonConvert.SerializeObject(file, Formatting.Indented);

            var temp = _filePath + ".tmp";
            using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
                await writer.WriteAsync(json).ConfigureAwait(false);

            if (File.Exists(_filePath))
                File.Replace(temp, _filePath, null);
            else
                File.Move(temp, _filePath);
        }

        // Formato su disco: separato dal modello di dominio, così i due possono evolvere in modo indipendente.
        internal sealed class TaskFile
        {
            public int Version { get; set; }
            public List<TaskRecord> Tasks { get; set; }
        }

        internal sealed class TaskRecord
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsCompleted { get; set; }
        }
    }
}
