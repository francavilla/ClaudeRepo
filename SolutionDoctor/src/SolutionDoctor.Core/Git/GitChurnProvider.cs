using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SolutionDoctor.Core.Git
{
    /// <summary>Calcola la frequenza di modifica con il comando <c>git</c> installato sulla macchina.</summary>
    public sealed class GitChurnProvider : IChurnProvider
    {
        private const int TimeoutMilliseconds = 60000;

        public IDictionary<string, int> TryGetChurn(string directory, int months)
        {
            var startInfo = new ProcessStartInfo("git")
            {
                // --relative: percorsi relativi alla cartella indicata, che è anche la base dei percorsi del report.
                Arguments = "-C \"" + directory + "\" log --relative --since=\"" + months + " months ago\" --name-only --pretty=format:",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return null;
                    }

                    // L'errore va svuotato in parallelo per non bloccare il processo su un buffer pieno.
                    process.ErrorDataReceived += (s, e) => { };
                    process.BeginErrorReadLine();
                    var output = process.StandardOutput.ReadToEnd();
                    if (!process.WaitForExit(TimeoutMilliseconds) || process.ExitCode != 0)
                    {
                        return null;
                    }

                    return GitLogParser.Parse(output);
                }
            }
            catch (Win32Exception)
            {
                return null; // git non installato
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
