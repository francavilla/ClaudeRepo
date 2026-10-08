using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class JsonFileTaskRepositoryTests : IDisposable
    {
        private static readonly CancellationToken None = default(CancellationToken);

        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dat-tests-" + Guid.NewGuid().ToString("N"));

        private string FilePath => Path.Combine(_folder, "dati", "tasks.json");

        public void Dispose()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        private static TaskItem NewTask(string title, bool completed = false)
        {
            return new TaskItem(Guid.NewGuid(), title, new DateTime(2026, 10, 8, 9, 30, 15, DateTimeKind.Local), completed);
        }

        [Fact]
        public async Task I_dati_sopravvivono_a_una_nuova_istanza()
        {
            var task = NewTask("Persistente", true);
            await new JsonFileTaskRepository(FilePath).AddAsync(task, None);

            var reloaded = await new JsonFileTaskRepository(FilePath).GetAllAsync(None);

            var item = Assert.Single(reloaded);
            Assert.Equal(task.Id, item.Id);
            Assert.Equal("Persistente", item.Title);
            Assert.Equal(task.CreatedAt, item.CreatedAt);
            Assert.True(item.IsCompleted);
        }

        [Fact]
        public async Task La_cartella_viene_creata_se_manca()
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(FilePath)));

            await new JsonFileTaskRepository(FilePath).AddAsync(NewTask("a"), None);

            Assert.True(File.Exists(FilePath));
            Assert.False(File.Exists(FilePath + ".tmp"));
        }

        [Fact]
        public async Task Update_e_Remove_vengono_salvati()
        {
            var first = NewTask("uno");
            var second = NewTask("due");
            var repository = new JsonFileTaskRepository(FilePath);
            await repository.AddAsync(first, None);
            await repository.AddAsync(second, None);

            await repository.UpdateAsync(first.WithCompleted(true), None);
            await repository.RemoveAsync(second.Id, None);

            var reloaded = await new JsonFileTaskRepository(FilePath).GetAllAsync(None);
            var item = Assert.Single(reloaded);
            Assert.Equal(first.Id, item.Id);
            Assert.True(item.IsCompleted);
        }

        [Fact]
        public async Task Senza_file_parte_dai_dati_iniziali_e_li_salva()
        {
            Func<IReadOnlyList<TaskItem>> initial = () => new[] { NewTask("a"), NewTask("b") };

            var first = await new JsonFileTaskRepository(FilePath, initial).GetAllAsync(None);
            Assert.Equal(2, first.Count);
            Assert.True(File.Exists(FilePath));

            // Con il file presente i dati iniziali non vengono più usati.
            var second = await new JsonFileTaskRepository(FilePath, () => new[] { NewTask("altro") }).GetAllAsync(None);
            Assert.Equal(first.Select(t => t.Id), second.Select(t => t.Id));
        }

        [Fact]
        public async Task Un_elenco_svuotato_dall_utente_non_viene_ripopolato()
        {
            Func<IReadOnlyList<TaskItem>> initial = () => new[] { NewTask("a") };
            var repository = new JsonFileTaskRepository(FilePath, initial);
            var only = (await repository.GetAllAsync(None)).Single();
            await repository.RemoveAsync(only.Id, None);

            var reloaded = await new JsonFileTaskRepository(FilePath, initial).GetAllAsync(None);

            Assert.Empty(reloaded);
        }

        [Fact]
        public async Task Senza_dati_iniziali_il_file_non_viene_creato_in_lettura()
        {
            var items = await new JsonFileTaskRepository(FilePath).GetAllAsync(None);

            Assert.Empty(items);
            Assert.False(File.Exists(FilePath));
        }

        [Fact]
        public async Task File_non_valido_solleva_un_errore_chiaro_e_non_viene_modificato()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, "{ questo non è json");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new JsonFileTaskRepository(FilePath).GetAllAsync(None));

            Assert.Contains(FilePath, ex.Message);
            Assert.Equal("{ questo non è json", File.ReadAllText(FilePath));
        }

        [Fact]
        public async Task Aggiunte_concorrenti_non_si_perdono()
        {
            var repository = new JsonFileTaskRepository(FilePath);

            await Task.WhenAll(Enumerable.Range(0, 20).Select(i => repository.AddAsync(NewTask("t" + i), None)));

            var reloaded = await new JsonFileTaskRepository(FilePath).GetAllAsync(None);
            Assert.Equal(20, reloaded.Count);
        }

        [Fact]
        public void La_cartella_predefinita_sta_in_LocalAppData_con_il_nome_dell_app()
        {
            var folder = new StorageSettings(null).ResolveFolder("MiaApp");

            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiaApp"), folder);
        }

        [Fact]
        public void La_cartella_configurata_espande_le_variabili_d_ambiente()
        {
            var folder = new StorageSettings(@"%TEMP%\MieiDati").ResolveFolder("MiaApp");

            Assert.Equal(Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "MieiDati"), folder);
        }
    }
}
