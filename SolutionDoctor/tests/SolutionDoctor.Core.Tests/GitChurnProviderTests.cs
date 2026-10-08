using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Git;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    /// <summary>Test d'integrazione con un repository git reale; si ignorano se git non è installato.</summary>
    public class GitChurnProviderTests
    {
        private static bool Git(string directory, string arguments)
        {
            var info = new ProcessStartInfo("git", "-C \"" + directory + "\" -c user.name=t -c user.email=t@t -c commit.gpgsign=false " + arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(info))
                {
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        [Fact]
        public void ContaICommitPerFile_ConPercorsiRelativiAllaCartellaAnalizzata()
        {
            using (var dir = new TempDirectory())
            {
                if (!Git(dir.Path, "init -q"))
                {
                    return; // git non disponibile
                }

                // La solution sta in una sottocartella del repository: i percorsi devono essere relativi ad essa.
                dir.Write("src/App/Form1.cs", "// v1");
                dir.Write("src/App/Form1.Designer.cs", "// v1");
                dir.Write("README.md", "fuori dalla solution");
                Assert.True(Git(dir.Path, "add -A") && Git(dir.Path, "commit -q -m uno"));
                dir.Write("src/App/Form1.cs", "// v2");
                Assert.True(Git(dir.Path, "commit -q -am due"));

                var churn = new GitChurnProvider().TryGetChurn(Path.Combine(dir.Path, "src"), 12);

                Assert.NotNull(churn);
                Assert.Equal(2, churn["App/Form1.cs"]);
                Assert.Equal(1, churn["App/Form1.Designer.cs"]);
                Assert.False(churn.ContainsKey("README.md"));
            }
        }

        [Fact]
        public void CartellaFuoriDaUnRepository_RestituisceNull()
        {
            using (var dir = new TempDirectory())
            {
                // La cartella temporanea del sistema non è in un repository; se lo fosse il test non è significativo.
                if (Git(dir.Path, "rev-parse --git-dir"))
                {
                    return;
                }

                Assert.Null(new GitChurnProvider().TryGetChurn(dir.Path, 12));
            }
        }

        [Fact]
        public void EndToEnd_LaCronologiaGitCambiaLOrdineDelBacklog()
        {
            using (var dir = new TempDirectory())
            {
                if (!Git(dir.Path, "init -q"))
                {
                    return;
                }

                dir.Write("App.csproj", Samples.ClassicWinFormsProject.Replace("<ProjectReference Include=\"..\\Lib\\Lib.csproj\" />", string.Empty));
                var sql = "using System.Windows.Forms; using System.Data.SqlClient; namespace A { public class {0} : Form { void M(string n) { var c = new SqlCommand(\"S\" + n); } } }";
                dir.Write("Calma.cs", sql.Replace("{0}", "Calma"));
                dir.Write("Attiva.cs", sql.Replace("{0}", "Attiva"));
                Assert.True(Git(dir.Path, "add -A") && Git(dir.Path, "commit -q -m uno"));
                for (var i = 0; i < 3; i++)
                {
                    dir.Write("Attiva.cs", sql.Replace("{0}", "Attiva") + "// modifica " + i);
                    Assert.True(Git(dir.Path, "commit -q -am modifica"));
                }

                var report = new SolutionAnalyzer().Analyze(dir.Path, new AnalysisOptions());

                Assert.True(report.ChurnAvailable);
                Assert.Equal("A.Attiva", report.Backlog[0].Subject);
                Assert.Equal(4, report.Backlog[0].Churn);
                Assert.Equal("A.Calma", report.Backlog[1].Subject);
                Assert.Equal(1, report.Backlog[1].Churn);
            }
        }
    }
}
