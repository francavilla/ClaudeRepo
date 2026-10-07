using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExeBuilder.Core.Model;

namespace ExeBuilder.Core.Toolchain
{
    public interface IToolchainLocator
    {
        Toolchains Locate();
    }

    /// <summary>
    /// Individua gli strumenti di build installati su Windows:
    /// dotnet.exe (+ SDK), MSBuild.exe tramite vswhere e i targeting pack .NET Framework.
    /// </summary>
    public sealed class ToolchainLocator : IToolchainLocator
    {
        private static readonly Regex SdkLine = new Regex(@"^(?<version>\d+\.\d+\.\d+)(?<suffix>\S*)\s+\[", RegexOptions.Compiled);

        public Toolchains Locate()
        {
            var result = new Toolchains();

            result.DotNetPath = FindDotNet();
            if (result.DotNetPath != null)
            {
                result.DotNetSdks.AddRange(ListSdks(result.DotNetPath));
            }

            string msBuildPath;
            Version msBuildVersion;
            if (TryFindMsBuild(out msBuildPath, out msBuildVersion))
            {
                result.MsBuildPath = msBuildPath;
                result.MsBuildVersion = msBuildVersion;
            }

            result.InstalledTargetingPacks.AddRange(FindTargetingPacks());
            return result;
        }

        internal static IEnumerable<Version> ParseSdkList(string output)
        {
            foreach (var line in (output ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var match = SdkLine.Match(line.Trim());
                Version version;
                if (match.Success && Version.TryParse(match.Groups["version"].Value, out version))
                {
                    yield return version;
                }
            }
        }

        private static string FindDotNet()
        {
            var candidates = new List<string>();

            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot))
            {
                candidates.Add(Path.Combine(dotnetRoot, "dotnet.exe"));
            }

            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"));

            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            candidates.AddRange(path.Split(Path.PathSeparator)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.Combine(p.Trim().Trim('"'), "dotnet.exe")));

            return candidates.FirstOrDefault(SafeFileExists);
        }

        private static IEnumerable<Version> ListSdks(string dotnetPath)
        {
            string output;
            return TryRun(dotnetPath, "--list-sdks", out output)
                ? ParseSdkList(output).Distinct().OrderBy(v => v).ToList()
                : new List<Version>();
        }

        private static bool TryFindMsBuild(out string path, out Version version)
        {
            path = null;
            version = null;

            // 1) Visual Studio / Build Tools 2017+ tramite vswhere.
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
            string output;
            if (SafeFileExists(vswhere)
                && TryRun(vswhere, "-latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe", out output))
            {
                path = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .FirstOrDefault(SafeFileExists);
            }

            // 2) Ripiego: MSBuild 4.x del .NET Framework (solo C# 5, niente restore).
            if (path == null)
            {
                var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                path = new[] { "Framework64", "Framework" }
                    .Select(f => Path.Combine(windows, "Microsoft.NET", f, "v4.0.30319", "MSBuild.exe"))
                    .FirstOrDefault(SafeFileExists);
            }

            if (path == null)
            {
                return false;
            }

            var fileVersion = FileVersionInfo.GetVersionInfo(path);
            version = new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart);
            return true;
        }

        private static IEnumerable<Version> FindTargetingPacks()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Reference Assemblies", "Microsoft", "Framework", ".NETFramework");

            if (!Directory.Exists(root))
            {
                return Enumerable.Empty<Version>();
            }

            var versions = new List<Version>();
            foreach (var directory in Directory.GetDirectories(root, "v*"))
            {
                Version version;
                // Un targeting pack valido contiene mscorlib.dll (le cartelle vuote restano dopo una disinstallazione).
                if (Version.TryParse(Path.GetFileName(directory).Substring(1), out version)
                    && File.Exists(Path.Combine(directory, "mscorlib.dll")))
                {
                    versions.Add(version);
                }
            }

            return versions.OrderBy(v => v).ToList();
        }

        private static bool TryRun(string fileName, string arguments, out string output)
        {
            output = null;
            try
            {
                using (var process = Process.Start(new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(30000);
                    return process.HasExited && process.ExitCode == 0;
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                return false;
            }
        }

        private static bool SafeFileExists(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
