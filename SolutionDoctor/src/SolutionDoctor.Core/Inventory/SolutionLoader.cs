using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Inventory
{
    /// <summary>
    /// Carica l'elenco dei progetti C# da un .sln, da una cartella (tutti i .csproj contenuti) o da un singolo .csproj.
    /// </summary>
    public static class SolutionLoader
    {
        private static readonly Regex ProjectLine = new Regex(
            "^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"(?<name>[^\"]*)\"\\s*,\\s*\"(?<path>[^\"]*)\"",
            RegexOptions.Multiline);

        private static readonly string[] IgnoredDirectories = { "bin", "obj", "packages", "node_modules", ".git", ".vs" };

        public static SolutionInfo Load(string path)
        {
            if (Directory.Exists(path))
            {
                var solutions = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly);
                if (solutions.Length > 1)
                {
                    throw new InvalidOperationException("La cartella contiene più file .sln: indicare quale analizzare.");
                }

                return solutions.Length == 1 ? LoadSolution(solutions[0]) : LoadFolder(path);
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Percorso non trovato: " + path);
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".sln")
            {
                return LoadSolution(path);
            }

            if (extension == ".csproj")
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(path));
                var single = new SolutionInfo { Name = Path.GetFileNameWithoutExtension(path), RootDirectory = folder };
                AddProject(single, path);
                return single;
            }

            throw new InvalidOperationException("Indicare un file .sln, un file .csproj o una cartella.");
        }

        private static SolutionInfo LoadSolution(string solutionPath)
        {
            var fullPath = Path.GetFullPath(solutionPath);
            var folder = Path.GetDirectoryName(fullPath);
            var solution = new SolutionInfo
            {
                Name = Path.GetFileNameWithoutExtension(fullPath),
                Path = fullPath,
                RootDirectory = folder
            };

            foreach (Match match in ProjectLine.Matches(File.ReadAllText(fullPath)))
            {
                var relative = match.Groups["path"].Value;
                if (!relative.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // cartelle di solution, progetti VB/F#/C++ non sono oggetto dell'analisi
                }

                var projectPath = Path.GetFullPath(Path.Combine(folder, PathUtil.Normalize(relative)));
                if (!File.Exists(projectPath))
                {
                    solution.Warnings.Add("Progetto non trovato: " + relative);
                    continue;
                }

                AddProject(solution, projectPath);
            }

            return solution;
        }

        private static SolutionInfo LoadFolder(string folder)
        {
            var fullPath = Path.GetFullPath(folder);
            var solution = new SolutionInfo { Name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar)), RootDirectory = fullPath };

            var projects = Directory.EnumerateFiles(fullPath, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !IsInIgnoredDirectory(fullPath, p))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects)
            {
                AddProject(solution, project);
            }

            return solution;
        }

        private static void AddProject(SolutionInfo solution, string projectPath)
        {
            try
            {
                solution.Projects.Add(ProjectReader.Read(projectPath, solution.RootDirectory));
            }
            catch (Exception ex) when (ex is XmlException || ex is InvalidDataException || ex is IOException)
            {
                solution.Warnings.Add("Progetto illeggibile (" + Path.GetFileName(projectPath) + "): " + ex.Message);
            }
        }

        internal static bool IsInIgnoredDirectory(string root, string file)
        {
            var segments = PathUtil.Relative(root, file).Split('/');
            return segments.Take(segments.Length - 1).Any(s => IgnoredDirectories.Contains(s, StringComparer.OrdinalIgnoreCase));
        }
    }
}
