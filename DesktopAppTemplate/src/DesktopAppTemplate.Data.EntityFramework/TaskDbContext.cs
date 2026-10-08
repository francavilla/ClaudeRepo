using System.Data.Common;
using System.Data.Entity;
using DesktopAppTemplate.Data.Tasks;

namespace DesktopAppTemplate.Data.EntityFramework
{
    /// <summary>
    /// Contesto EF6 sulla tabella Tasks già creata dagli script di migrazione (nessuna creazione o migrazione EF:
    /// lo schema è uno solo per tutte le tecnologie di accesso).
    /// </summary>
    [DbConfigurationType(typeof(TaskDbConfiguration))]
    internal sealed class TaskDbContext : DbContext
    {
        static TaskDbContext()
        {
            Database.SetInitializer<TaskDbContext>(null);
        }

        /// <summary>Il contesto possiede la connessione: la chiude e la elimina insieme a sé.</summary>
        public TaskDbContext(DbConnection connection)
            : base(connection, true)
        {
        }

        public DbSet<TaskRow> Tasks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var task = modelBuilder.Entity<TaskRow>();
            task.ToTable("Tasks");
            task.HasKey(r => r.Id);
            task.Property(r => r.Id).IsRequired().HasMaxLength(36);
            task.Property(r => r.Title).IsRequired().HasMaxLength(200);
            task.Property(r => r.CreatedAt).IsRequired().HasMaxLength(40);
        }
    }
}
