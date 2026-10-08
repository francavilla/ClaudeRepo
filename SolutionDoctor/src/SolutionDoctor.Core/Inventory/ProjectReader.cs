using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Inventory
{
    /// <summary>Legge un file .csproj (classico o SDK-style) senza caricare MSBuild.</summary>
    public static class ProjectReader
    {
        public static ProjectInfo Read(string projectPath, string rootDirectory)
        {
            var fullPath = Path.GetFullPath(projectPath);
            var root = XDocument.Load(fullPath).Root;
            if (root == null || root.Name.LocalName != "Project")
            {
                throw new InvalidDataException("Non è un file di progetto MSBuild: " + fullPath);
            }

            var info = new ProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(fullPath),
                Path = fullPath,
                Directory = Path.GetDirectoryName(fullPath),
                RelativePath = PathUtil.Relative(rootDirectory, fullPath),
                IsSdkStyle = root.Attribute("Sdk") != null || root.Elements().Any(e => e.Name.LocalName == "Sdk"),
                OutputType = FirstValue(root, "OutputType") ?? "Library"
            };

            ReadTargetFrameworks(root, info);

            foreach (var reference in Elements(root, "Reference"))
            {
                var include = (string)reference.Attribute("Include");
                if (!string.IsNullOrWhiteSpace(include))
                {
                    info.AssemblyReferences.Add(include.Split(',')[0].Trim());
                }
            }

            info.ComReferenceCount = Elements(root, "COMReference").Count();

            foreach (var reference in Elements(root, "PackageReference"))
            {
                var name = (string)reference.Attribute("Include");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var version = (string)reference.Attribute("Version")
                    ?? reference.Elements().Where(e => e.Name.LocalName == "Version").Select(e => e.Value).FirstOrDefault();
                info.Packages.Add(new PackageRef(name, version));
            }

            foreach (var reference in Elements(root, "ProjectReference"))
            {
                var include = (string)reference.Attribute("Include");
                if (!string.IsNullOrWhiteSpace(include))
                {
                    info.ProjectReferences.Add(Path.GetFullPath(Path.Combine(info.Directory, PathUtil.Normalize(include))));
                }
            }

            ReadPackagesConfig(info);

            var useWindowsForms = FirstValue(root, "UseWindowsForms");
            info.IsWinForms = string.Equals(useWindowsForms, "true", StringComparison.OrdinalIgnoreCase)
                || info.AssemblyReferences.Contains("System.Windows.Forms", StringComparer.OrdinalIgnoreCase);

            return info;
        }

        private static void ReadTargetFrameworks(XElement root, ProjectInfo info)
        {
            var multiple = FirstValue(root, "TargetFrameworks");
            var single = FirstValue(root, "TargetFramework");
            var legacy = FirstValue(root, "TargetFrameworkVersion");

            if (!string.IsNullOrWhiteSpace(multiple))
            {
                info.TargetFrameworks.AddRange(multiple.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            else if (!string.IsNullOrWhiteSpace(single))
            {
                info.TargetFrameworks.Add(single.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(legacy))
            {
                info.TargetFrameworks.Add(TargetFrameworks.FromVersion(legacy));
            }
        }

        private static void ReadPackagesConfig(ProjectInfo info)
        {
            var file = Path.Combine(info.Directory, "packages.config");
            if (!File.Exists(file))
            {
                return;
            }

            info.UsesPackagesConfig = true;
            var root = XDocument.Load(file).Root;
            if (root == null)
            {
                return;
            }

            foreach (var package in Elements(root, "package"))
            {
                var id = (string)package.Attribute("id");
                if (!string.IsNullOrWhiteSpace(id) && !info.Packages.Any(p => string.Equals(p.Name, id, StringComparison.OrdinalIgnoreCase)))
                {
                    info.Packages.Add(new PackageRef(id, (string)package.Attribute("version")));
                }
            }
        }

        private static IEnumerable<XElement> Elements(XElement root, string localName)
        {
            return root.Descendants().Where(e => e.Name.LocalName == localName);
        }

        private static string FirstValue(XElement root, string localName)
        {
            return Elements(root, localName).Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0);
        }
    }
}
