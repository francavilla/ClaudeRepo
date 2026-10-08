using System.Collections.Generic;
using DesktopAppTemplate.Core.Configuration;

namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Impostazioni della slice "Attività".</summary>
    public sealed class TaskSettings
    {
        public const string ReadOnlyKey = "ReadOnly";

        public static readonly OptionDefinition ReadOnlyOption = new OptionDefinition(
            ReadOnlyKey, "Mostra le attività senza permettere di modificarle.", isFlag: true);

        public TaskSettings(bool readOnly)
        {
            ReadOnly = readOnly;
        }

        /// <summary>Se true, aggiunta, completamento ed eliminazione sono disabilitati.</summary>
        public bool ReadOnly { get; }

        /// <summary>Opzioni da dichiarare nel catalogo dell'applicazione.</summary>
        public static IEnumerable<OptionDefinition> Options => new[] { ReadOnlyOption };

        public static TaskSettings From(IAppConfiguration configuration)
        {
            return new TaskSettings(configuration.GetBool(ReadOnlyKey));
        }
    }
}
