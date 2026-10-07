using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BuildExe.Core.Execution
{
    public interface IProcessRunner
    {
        /// <summary>Esegue il processo inoltrando ogni riga di output; restituisce l'exit code.</summary>
        Task<int> RunAsync(string fileName, string arguments, string workingDirectory, IProgress<string> output, CancellationToken cancellationToken);
    }

    public sealed class ProcessRunner : IProcessRunner
    {
        public Task<int> RunAsync(string fileName, string arguments, string workingDirectory, IProgress<string> output, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Output pulito e niente processi MSBuild residenti dopo la build.
            startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
            startInfo.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";

            var process = new Process { StartInfo = startInfo };
            DataReceivedEventHandler forward = (s, e) =>
            {
                if (e.Data != null && output != null)
                {
                    output.Report(e.Data);
                }
            };
            process.OutputDataReceived += forward;
            process.ErrorDataReceived += forward;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var registration = cancellationToken.Register(() => KillTree(process));

            return Task.Run(() =>
            {
                try
                {
                    // WaitForExit() senza timeout attende anche lo svuotamento degli stream asincroni.
                    process.WaitForExit();
                    cancellationToken.ThrowIfCancellationRequested();
                    return process.ExitCode;
                }
                finally
                {
                    registration.Dispose();
                    process.Dispose();
                }
            });
        }

        private static void KillTree(Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }

                // .NET Framework non ha Process.Kill(entireProcessTree): taskkill /T termina anche i nodi MSBuild figli.
                using (var killer = Process.Start(new ProcessStartInfo("taskkill", "/PID " + process.Id + " /T /F")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                }))
                {
                    killer.WaitForExit(10000);
                }
            }
            catch (InvalidOperationException)
            {
                // Processo già terminato.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
