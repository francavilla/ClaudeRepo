using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionDoctor.Core.Inventory;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Analizza tutti i sorgenti scritti a mano di un progetto WinForms.</summary>
    internal static class CodeScanner
    {
        private static readonly string[] IgnoredDirectories = { "bin", "obj" };

        public static void Scan(ProjectInfo project, string rootDirectory, List<Finding> findings, List<UiClassInfo> uiClasses)
        {
            var parts = new List<ClassPart>();

            foreach (var file in EnumerateSources(project.Directory))
            {
                var analysis = CodeSmellDetector.AnalyzeSource(File.ReadAllText(file), PathUtil.Relative(rootDirectory, file));
                foreach (var finding in analysis.Findings)
                {
                    finding.Project = project.Name;
                }

                findings.AddRange(analysis.Findings);
                parts.AddRange(analysis.Parts);
            }

            uiClasses.AddRange(UiClassAggregator.Aggregate(project.Name, parts, findings));
        }

        /// <summary>File .cs scritti a mano: esclude bin/obj, codice generato e Designer.</summary>
        internal static IEnumerable<string> EnumerateSources(string projectDirectory)
        {
            return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsExcluded(projectDirectory, f))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsExcluded(string projectDirectory, string file)
        {
            var relative = PathUtil.Relative(projectDirectory, file);
            var segments = relative.Split('/');
            if (segments.Take(segments.Length - 1).Any(s => IgnoredDirectories.Contains(s, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            var name = segments[segments.Length - 1];
            return name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
