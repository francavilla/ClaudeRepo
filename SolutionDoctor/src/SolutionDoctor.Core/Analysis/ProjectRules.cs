using System;
using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Inventory;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Regole sull'inventario del progetto (formato, pacchetti, riferimenti, framework).</summary>
    public static class ProjectRules
    {
        // Riferimenti che vanno verificati prima di passare a .NET moderno, con la gravità del blocco.
        private static readonly Dictionary<string, Severity> Blockers = new Dictionary<string, Severity>(StringComparer.OrdinalIgnoreCase)
        {
            { "System.Runtime.Remoting", Severity.High },
            { "System.Data.OracleClient", Severity.High },
            { "System.EnterpriseServices", Severity.Medium },
            { "System.ServiceModel", Severity.Low }
        };

        public static IEnumerable<Finding> Evaluate(ProjectInfo project)
        {
            if (project.UsesPackagesConfig)
            {
                yield return Create(project, RuleCatalog.PackagesConfig, Severity.Medium,
                    "Usa packages.config (" + project.Packages.Count + (project.Packages.Count == 1 ? " pacchetto" : " pacchetti") + "): migrare a PackageReference.");
            }

            if (!project.IsSdkStyle)
            {
                yield return Create(project, RuleCatalog.ClassicProjectFormat, Severity.Low,
                    "Il file di progetto non è SDK-style.");
            }

            foreach (var framework in project.TargetFrameworks.Where(TargetFrameworks.IsOutOfSupport))
            {
                yield return Create(project, RuleCatalog.OutdatedFramework, Severity.Medium,
                    "Framework di destinazione " + framework + " fuori supporto.");
            }

            foreach (var reference in project.AssemblyReferences.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Severity severity;
                if (Blockers.TryGetValue(reference, out severity)
                    || (reference.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase) && Assign(out severity, Severity.Medium)))
                {
                    yield return Create(project, RuleCatalog.MigrationBlockers, severity,
                        "Riferimento a " + reference + ": non disponibile o diverso su .NET moderno.");
                }
            }

            if (project.ComReferenceCount > 0)
            {
                yield return Create(project, RuleCatalog.MigrationBlockers, Severity.Medium,
                    project.ComReferenceCount + (project.ComReferenceCount == 1 ? " riferimento COM" : " riferimenti COM") + ": richiede registrazione e interop su .NET moderno.");
            }
        }

        private static bool Assign(out Severity target, Severity value)
        {
            target = value;
            return true;
        }

        private static Finding Create(ProjectInfo project, string rule, Severity severity, string message)
        {
            return new Finding(rule, severity, message, project.RelativePath, 1, project.Name) { Project = project.Name };
        }
    }
}
