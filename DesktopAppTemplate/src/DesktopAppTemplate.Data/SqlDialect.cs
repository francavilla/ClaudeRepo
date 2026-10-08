using System;
using System.Text.RegularExpressions;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Data
{
    /// <summary>Dialetto predefinito: parametri con prefisso "@" (SQL Server, SQLite). Con un altro prefisso riscrive i segnaposto.</summary>
    public sealed class SqlDialect : ISqlDialect
    {
        private static readonly Regex Placeholder = new Regex(@"@(\w+)", RegexOptions.Compiled);

        public SqlDialect(string parameterPrefix = "@")
        {
            if (string.IsNullOrEmpty(parameterPrefix))
                throw new ArgumentException("Il prefisso dei parametri è obbligatorio.", nameof(parameterPrefix));

            ParameterPrefix = parameterPrefix;
        }

        public string ParameterPrefix { get; }

        public string Adapt(string sql)
        {
            return ParameterPrefix == "@" ? sql : Placeholder.Replace(sql, ParameterPrefix + "$1");
        }
    }
}
