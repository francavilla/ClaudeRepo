using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using DesktopAppTemplate.Features.Tasks;

namespace DesktopAppTemplate.Data.Tasks
{
    /// <summary>Conversioni tra il modello di dominio (<see cref="TaskItem"/>) e la riga del database (<see cref="TaskRow"/>).</summary>
    public static class TaskRowMapper
    {
        public static TaskRow ToRow(TaskItem item)
        {
            return new TaskRow
            {
                Id = item.Id.ToString("D"),
                Title = item.Title,
                CreatedAt = item.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                IsCompleted = item.IsCompleted ? 1 : 0
            };
        }

        public static TaskItem ToItem(TaskRow row)
        {
            return new TaskItem(
                Guid.Parse(row.Id),
                row.Title,
                DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                row.IsCompleted != 0);
        }

        /// <summary>Legge una riga nell'ordine delle colonne di <see cref="TaskSql.SelectAll"/>.</summary>
        public static TaskItem FromRecord(IDataRecord record)
        {
            return ToItem(new TaskRow
            {
                Id = record.GetString(0),
                Title = record.GetString(1),
                CreatedAt = record.GetString(2),
                IsCompleted = record.GetInt64(3)
            });
        }

        /// <summary>Parametri per INSERT e UPDATE (nomi senza prefisso).</summary>
        public static Dictionary<string, object> ToParameters(TaskRow row)
        {
            return new Dictionary<string, object>
            {
                { "Id", row.Id },
                { "Title", row.Title },
                { "CreatedAt", row.CreatedAt },
                { "IsCompleted", row.IsCompleted }
            };
        }

        public static Dictionary<string, object> IdParameter(Guid id)
        {
            return new Dictionary<string, object> { { "Id", id.ToString("D") } };
        }
    }
}
