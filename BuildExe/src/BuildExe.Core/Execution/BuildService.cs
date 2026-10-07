using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildExe.Core.Model;

namespace BuildExe.Core.Execution
{
    public sealed class BuildResult
    {
        public BuildResult(int exitCode, TimeSpan duration, IReadOnlyList<string> executables)
        {
            ExitCode = exitCode;
            Duration = duration;
            Executables = executables;
        }

        public int ExitCode { get; private set; }

        public bool Succeeded
        {
            get { return ExitCode == 0; }
        }

        public TimeSpan Duration { get; private set; }

        /// <summary>Eseguibili presenti nella cartella di output (quello del progetto per primo).</summary>
        public IReadOnlyList<string> Executables { get; private set; }
    }

    /// <summary>Esegue un <see cref="BuildPlan"/> e verifica l'output prodotto.</summary>
    public sealed class BuildService
    {
        private readonly IProcessRunner _runner;

        public BuildService(IProcessRunner runner)
        {
            _runner = runner;
        }

        public async Task<BuildResult> RunAsync(BuildPlan plan, ProjectInfo project, string outputDirectory, IProgress<string> log, CancellationToken cancellationToken)
        {
            if (!plan.CanBuild)
            {
                throw new InvalidOperationException("Il piano di build contiene errori.");
            }

            Directory.CreateDirectory(outputDirectory);

            log.Report("> " + plan.CommandLineText);
            log.Report(string.Empty);

            var stopwatch = Stopwatch.StartNew();
            var exitCode = await _runner.RunAsync(plan.Executable, plan.ArgumentsText, plan.WorkingDirectory, log, cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();

            var executables = exitCode == 0 ? FindExecutables(outputDirectory, project.AssemblyName) : new List<string>();
            return new BuildResult(exitCode, stopwatch.Elapsed, executables);
        }

        internal static List<string> FindExecutables(string outputDirectory, string assemblyName)
        {
            if (!Directory.Exists(outputDirectory))
            {
                return new List<string>();
            }

            return Directory.GetFiles(outputDirectory, "*.exe", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => string.Equals(Path.GetFileNameWithoutExtension(f), assemblyName, StringComparison.OrdinalIgnoreCase))
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
