using System.IO;
using SolutionDoctor.Cli;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class CliRunnerTests
    {
        private static int Run(out string output, out string error, params string[] args)
        {
            var o = new StringWriter();
            var e = new StringWriter();
            var code = CliRunner.Run(args, o, e);
            output = o.ToString();
            error = e.ToString();
            return code;
        }

        [Fact]
        public void SenzaArgomenti_MostraLUsoEdEErrore()
        {
            Assert.Equal(CliRunner.Error, Run(out var output, out _));
            Assert.Contains("solutiondoctor analyze <percorso>", output);
        }

        [Fact]
        public void HelpEVersion()
        {
            Assert.Equal(CliRunner.Ok, Run(out var help, out _, "--help"));
            Assert.Contains("--fail-on", help);

            Assert.Equal(CliRunner.Ok, Run(out var version, out _, "--version"));
            Assert.StartsWith("SolutionDoctor ", version);
        }

        [Theory]
        [InlineData("analizza")]
        public void ComandoSconosciuto_Errore(string command)
        {
            Assert.Equal(CliRunner.Error, Run(out _, out var error, command));
            Assert.Contains("Comando sconosciuto", error);
        }

        [Theory]
        [InlineData("--fail-on")]
        [InlineData("--fail-on", "gravissimo")]
        [InlineData("--months", "zero")]
        [InlineData("--months", "0")]
        [InlineData("--output")]
        [InlineData("--sconosciuta")]
        public void OpzioniNonValide_Errore(params string[] extra)
        {
            using (var dir = new TempDirectory())
            {
                var args = new[] { "analyze", dir.Path };
                var all = new string[args.Length + extra.Length];
                args.CopyTo(all, 0);
                extra.CopyTo(all, args.Length);

                Assert.Equal(CliRunner.Error, Run(out _, out var error, all));
                Assert.NotEmpty(error);
            }
        }

        [Fact]
        public void PercorsoMancanteOInesistente_Errore()
        {
            Assert.Equal(CliRunner.Error, Run(out _, out var e1, "analyze"));
            Assert.Contains("Indicare il percorso", e1);

            Assert.Equal(CliRunner.Error, Run(out _, out var e2, "analyze", "/non/esiste/x.sln"));
            Assert.Contains("non trovato", e2);
        }

        [Fact]
        public void SenzaOutput_ScriveILReportSuStandardOutput()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);

                var code = Run(out var output, out _, "analyze", dir.Path, "--no-git");

                Assert.Equal(CliRunner.Ok, code);
                Assert.Contains("# SolutionDoctor — Legacy", output);
            }
        }

        [Fact]
        public void ConOutput_ScriveIlFileEUnRiepilogo()
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);
                var file = Path.Combine(dir.Path, "out", "report.md");

                var code = Run(out var output, out _, "analyze", dir.Path, "--no-git", "-o", file);

                Assert.Equal(CliRunner.Ok, code);
                Assert.True(File.Exists(file));
                Assert.Contains("# SolutionDoctor", File.ReadAllText(file));
                Assert.Contains("2 progetti, 1 form", output);
                Assert.DoesNotContain("# SolutionDoctor", output);
            }
        }

        [Theory]
        [InlineData("high", 1)]      // c'è SQL concatenato (alta)
        [InlineData("info", 1)]
        public void FailOn_RestituisceUnoSeCiSonoProblemiAllaSoglia(string level, int expected)
        {
            using (var dir = new TempDirectory())
            {
                LegacySolution.Create(dir);

                Assert.Equal(expected, Run(out _, out _, "analyze", dir.Path, "--no-git", "--fail-on", level));
            }
        }

        [Fact]
        public void FailOn_ZeroSeNonCiSonoProblemiAllaSoglia()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Lib/Lib.csproj", Samples.LibProject());

                Assert.Equal(CliRunner.Ok, Run(out _, out _, "analyze", dir.Path, "--no-git", "--fail-on", "info"));
            }
        }
    }
}
