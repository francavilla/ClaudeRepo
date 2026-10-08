namespace DesktopAppTemplate.Data.Tasks
{
    /// <summary>
    /// SQL delle attività, scritto una volta sola con parametri <c>@Nome</c> e valido per SQLite e SQL Server
    /// (il dialetto adatta i segnaposto se il database ne usa altri).
    /// </summary>
    public static class TaskSql
    {
        public const string SelectAll = "SELECT Id, Title, CreatedAt, IsCompleted FROM Tasks ORDER BY CreatedAt";
        public const string SelectById = "SELECT Id, Title, CreatedAt, IsCompleted FROM Tasks WHERE Id = @Id";
        public const string Insert = "INSERT INTO Tasks (Id, Title, CreatedAt, IsCompleted) VALUES (@Id, @Title, @CreatedAt, @IsCompleted)";
        public const string Update = "UPDATE Tasks SET Title = @Title, CreatedAt = @CreatedAt, IsCompleted = @IsCompleted WHERE Id = @Id";
        public const string Delete = "DELETE FROM Tasks WHERE Id = @Id";
    }
}
