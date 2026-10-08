using System;
using System.IO;
using System.Linq;
using System.Text;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Core.Reporting;

namespace SolutionDoctor.Cli
{
    /// <summary>
    /// Interfaccia a riga di comando. Codici di uscita: 0 = ok, 1 = trovati problemi almeno pari alla soglia --fail-on,
    /// 2 = uso errato o errore di lettura.
    /// </summary>
    public static class CliRunner
    {
        public const int Ok = 0;
        public const int FindingsAboveThreshold = 1;
        public const int Error = 2;

        public static int Run(string[] args, TextWriter output, TextWriter error)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                output.WriteLine(Usage());
                return args.Length == 0 ? Error : Ok;
            }

            if (args[0] == "--version")
            {
                output.WriteLine("SolutionDoctor " + SolutionAnalyzer.ToolVersion);
                return Ok;
            }

            if (args[0] != "analyze")
            {
                error.WriteLine("Comando sconosciuto: " + args[0]);
                error.WriteLine(Usage());
                return Error;
            }

            string path = null;
            string outputFile = null;
            Severity? failOn = null;
            var options = new AnalysisOptions();

            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--output":
                    case "-o":
                        if (!TryValue(args, ref i, out outputFile))
                        {
                            return Fail(error, "Manca il file dopo " + args[i]);
                        }

                        break;
                    case "--fail-on":
                        string level;
                        if (!TryValue(args, ref i, out level) || !TryParseSeverity(level, out failOn))
                        {
                            return Fail(error, "--fail-on richiede info, low, medium o high");
                        }

                        break;
                    case "--months":
                        string months;
                        int parsed;
                        if (!TryValue(args, ref i, out months) || !int.TryParse(months, out parsed) || parsed < 1)
                        {
                            return Fail(error, "--months richiede un numero intero positivo");
                        }

                        options.GitMonths = parsed;
                        break;
                    case "--no-git":
                        options.UseGit = false;
                        break;
                    default:
                        if (args[i].StartsWith("-", StringComparison.Ordinal) || path != null)
                        {
                            return Fail(error, "Argomento non riconosciuto: " + args[i]);
                        }

                        path = args[i];
                        break;
                }
            }

            if (path == null)
            {
                return Fail(error, "Indicare il percorso di un .sln, di un .csproj o di una cartella.");
            }

            AnalysisReport report;
            try
            {
                report = new SolutionAnalyzer().Analyze(path, options);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return Fail(error, ex.Message);
            }

            var markdown = MarkdownReportWriter.Write(report);
            if (outputFile == null)
            {
                output.Write(markdown);
            }
            else
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(outputFile));
                Directory.CreateDirectory(folder);
                File.WriteAllText(outputFile, markdown, new UTF8Encoding(false));
                output.WriteLine(Summary(report) + " Report: " + outputFile);
            }

            return failOn.HasValue && report.Findings.Any(f => f.Severity >= failOn.Value) ? FindingsAboveThreshold : Ok;
        }

        private static string Summary(AnalysisReport report)
        {
            return report.Solution.Projects.Count + " progetti, " + report.UiClasses.Count + " form, "
                + report.Findings.Count + " problemi (" + report.Findings.Count(f => f.Severity == Severity.High) + " alti).";
        }

        private static int Fail(TextWriter error, string message)
        {
            error.WriteLine(message);
            return Error;
        }

        private static bool TryValue(string[] args, ref int index, out string value)
        {
            if (index + 1 < args.Length)
            {
                value = args[++index];
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryParseSeverity(string text, out Severity? severity)
        {
            Severity parsed;
            if (Enum.TryParse(text, true, out parsed) && Enum.IsDefined(typeof(Severity), parsed))
            {
                severity = parsed;
                return true;
            }

            severity = null;
            return false;
        }

        private static string Usage()
        {
            return "SolutionDoctor " + SolutionAnalyzer.ToolVersion + " — analisi di solution WinForms per pianificare il refactoring\n"
                + "\n"
                + "Uso:\n"
                + "  solutiondoctor analyze <percorso> [opzioni]\n"
                + "  solutiondoctor --version | --help\n"
                + "\n"
                + "<percorso>  file .sln, file .csproj o cartella\n"
                + "\n"
                + "Opzioni:\n"
                + "  -o, --output <file>   scrive il report Markdown su file (altrimenti su standard output)\n"
                + "  --fail-on <livello>   codice di uscita 1 se esistono problemi >= livello (info|low|medium|high)\n"
                + "  --no-git              non usa la cronologia git per la priorità\n"
                + "  --months <n>          finestra della cronologia git, in mesi (predefinito 12)\n"
                + "\n"
                + "Codici di uscita: 0 ok, 1 soglia --fail-on superata, 2 errore.";
        }
    }
}
