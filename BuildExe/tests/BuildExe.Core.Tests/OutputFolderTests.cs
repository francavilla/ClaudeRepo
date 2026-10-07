using System;
using System.IO;
using BuildExe.Core.Execution;
using BuildExe.Core.Model;
using BuildExe.Core.Planning;
using Xunit;

namespace BuildExe.Core.Tests
{
    public class OutputFolderTests
    {
        [Fact]
        public void Clean_svuota_file_sottocartelle_e_file_in_sola_lettura_ma_tiene_la_cartella()
        {
            using (var dir = new TempDirectory())
            {
                var output = Path.Combine(dir.Path, "out");
                dir.Write("out/vecchio.exe", "x");
                dir.Write("out/de/risorse.dll", "x");
                var readOnly = dir.Write("out/sola-lettura.config", "x");
                File.SetAttributes(readOnly, FileAttributes.ReadOnly);

                var deleted = OutputFolder.Clean(output);

                Assert.Equal(3, deleted);
                Assert.True(Directory.Exists(output));
                Assert.Empty(Directory.GetFileSystemEntries(output));
            }
        }

        [Fact]
        public void Clean_di_una_cartella_inesistente_non_fa_nulla()
        {
            Assert.Equal(0, OutputFolder.Clean(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        }

        [Fact]
        public void Una_sottocartella_del_progetto_e_valida()
        {
            using (var dir = new TempDirectory())
            {
                var project = Path.Combine(dir.Path, "App");

                Assert.Null(OutputFolder.ValidateForCleaning(Path.Combine(project, "publish"), new[] { project }));
            }
        }

        [Fact]
        public void Rifiuta_la_cartella_dei_sorgenti_e_le_sue_cartelle_superiori()
        {
            using (var dir = new TempDirectory())
            {
                var project = Path.Combine(dir.Path, "Solution", "App");

                Assert.NotNull(OutputFolder.ValidateForCleaning(project, new[] { project }));
                Assert.NotNull(OutputFolder.ValidateForCleaning(project + Path.DirectorySeparatorChar, new[] { project }));
                Assert.NotNull(OutputFolder.ValidateForCleaning(Path.Combine(dir.Path, "Solution"), new[] { project }));
            }
        }

        [Fact]
        public void Non_confonde_cartelle_con_lo_stesso_prefisso()
        {
            using (var dir = new TempDirectory())
            {
                // "App-out" non contiene "App": è solo un nome che inizia allo stesso modo.
                Assert.Null(OutputFolder.ValidateForCleaning(Path.Combine(dir.Path, "App-out"), new[] { Path.Combine(dir.Path, "App") }));
            }
        }

        [Fact]
        public void Rifiuta_la_radice_e_le_cartelle_di_sistema()
        {
            Assert.NotNull(OutputFolder.ValidateForCleaning(Path.GetPathRoot(Path.GetTempPath()), null));
            Assert.NotNull(OutputFolder.ValidateForCleaning(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), null));
            Assert.Throws<InvalidOperationException>(() => OutputFolder.Clean(Path.GetPathRoot(Path.GetTempPath())));
        }

        [Fact]
        public void Il_planner_blocca_un_output_che_coincide_con_la_cartella_del_progetto()
        {
            var info = new ProjectInfo(Path.Combine(Path.GetTempPath(), "Prog", "Prog.csproj")) { IsSdkStyle = true, Sdk = "Microsoft.NET.Sdk", OutputKind = OutputKind.Exe };
            info.TargetFrameworks.Add(TargetFramework.Parse("net8.0"));
            var tools = new Toolchains { DotNetPath = "dotnet.exe" };
            tools.DotNetSdks.Add(new Version(8, 0, 100));

            var plan = new BuildPlanner().CreatePlan(info, new BuildOptions { OutputDirectory = info.Directory }, tools);

            Assert.False(plan.CanBuild);
            Assert.Contains(plan.Errors, e => e.Contains("sorgenti"));
        }
    }
}
