namespace DesktopAppTemplate.Core.Data
{
    /// <summary>
    /// PUNTO DI AGGANCIO: convenzioni del database per i segnaposto dei parametri. Gli SQL del modello sono scritti
    /// con parametri nominati nella forma <c>@Nome</c>; il dialetto li adatta alla convenzione del database
    /// (es. <c>:Nome</c> per Oracle) e fornisce il prefisso per i parametri ADO.NET.
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>Prefisso dei parametri nominati ("@" per SQL Server e SQLite).</summary>
        string ParameterPrefix { get; }

        /// <summary>Converte un testo SQL scritto con <c>@Nome</c> nella convenzione del database.</summary>
        string Adapt(string sql);
    }
}
