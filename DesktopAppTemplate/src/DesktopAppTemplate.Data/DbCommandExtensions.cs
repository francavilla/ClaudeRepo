using System;
using System.Data.Common;

namespace DesktopAppTemplate.Data
{
    public static class DbCommandExtensions
    {
        /// <summary>Aggiunge un parametro ADO.NET (il nome comprende già il prefisso).</summary>
        public static void AddParameter(this DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
