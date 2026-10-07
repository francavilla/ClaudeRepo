using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BuildExe.Core.Model;

namespace BuildExe.Core.Analysis
{
    /// <summary>
    /// Legge l'elenco dei progetti da una solution .sln (formato testo) o .slnx (formato XML, VS 2022 17.13+).
    /// </summary>
    public sealed class SolutionParser
    {
        private static readonly Regex ProjectLine = new Regex(
            "^Project\\(\"\\{(?<type>[^}]+)\\}\"\\)\\s*=\\s*\"(?<name>[^\"]+)\"\\s*,\\s*\"(?<path>[^\"]+)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsSolutionFile(string path)
        {
            var extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            return extension == ".sln" || extension == ".slnx";
        }

        public IReadOnlyList<SolutionProject> Parse(string solutionPath)
        {
            if (!File.Exists(solutionPath))
            {
                throw new FileNotFoundException("Solution non trovata.", solutionPath);
            }

            var fullPath = Path.GetFullPath(solutionPath);
            var relativePaths = string.Equals(Path.GetExtension(fullPath), ".slnx", StringComparison.OrdinalIgnoreCase)
                ? ParseSlnx(fullPath)
                : ParseSln(fullPath);

            var directory = Path.GetDirectoryName(fullPath);
            return relativePaths
                .Select(p => NormalizeSeparators(p))
                .Where(ProjectAnalyzer.IsProjectFile)
                .Select(p => Path.GetFullPath(Path.Combine(directory, p)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new SolutionProject(Path.GetFileNameWithoutExtension(p), p))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> ParseSln(string path)
        {
            foreach (var line in File.ReadLines(path))
            {
                var match = ProjectLine.Match(line.Trim());
                if (match.Success)
                {
                    // Le cartelle di solution hanno come "path" il loro nome: le scarta il filtro sull'estensione.
                    yield return match.Groups["path"].Value;
                }
            }
        }

        private static IEnumerable<string> ParseSlnx(string path)
        {
            var document = XDocument.Load(path);
            return document.Descendants()
                .Where(e => e.Name.LocalName == "Project")
                .Select(e => (string)e.Attribute("Path"))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        private static string NormalizeSeparators(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
