using System;
using System.IO;
using System.Linq;
using SolutionDoctor.Core.Inventory;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class SolutionLoaderTests
    {
        [Fact]
        public void Sln_CaricaSoloProgettiCsharpEsistenti_ESegnalaQuelliMancanti()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("App/App.csproj", Samples.SdkWinFormsProject);
                dir.Write("Lib/Lib.csproj", Samples.LibProject());
                var sln = dir.Write("My.sln", Samples.Solution("App\\App.csproj", "Lib\\Lib.csproj", "Manca\\Manca.csproj"));

                var solution = SolutionLoader.Load(sln);

                Assert.Equal("My", solution.Name);
                Assert.Equal(new[] { "App", "Lib" }, solution.Projects.Select(p => p.Name));
                Assert.Single(solution.Warnings);
                Assert.Contains("Manca", solution.Warnings[0]);
            }
        }

        [Fact]
        public void Cartella_ConUnSln_UsaIlSln()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("App/App.csproj", Samples.SdkWinFormsProject);
                dir.Write("My.sln", Samples.Solution("App\\App.csproj"));
                dir.Write("Altro/Altro.csproj", Samples.LibProject());

                var solution = SolutionLoader.Load(dir.Path);

                Assert.Equal("My", solution.Name);
                Assert.Single(solution.Projects);
            }
        }

        [Fact]
        public void Cartella_SenzaSln_CercaITuttiICsproj_IgnorandoBinEObj()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("App/App.csproj", Samples.SdkWinFormsProject);
                dir.Write("Lib/Lib.csproj", Samples.LibProject());
                dir.Write("Lib/obj/Copia.csproj", Samples.LibProject());
                dir.Write("packages/Pkg/Pkg.csproj", Samples.LibProject());

                var solution = SolutionLoader.Load(dir.Path);

                Assert.Equal(new[] { "App", "Lib" }, solution.Projects.Select(p => p.Name));
            }
        }

        [Fact]
        public void ProgettoIlleggibile_DiventaUnAvviso()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Rotto/Rotto.csproj", "<Project><non chiuso>");
                dir.Write("Lib/Lib.csproj", Samples.LibProject());

                var solution = SolutionLoader.Load(dir.Path);

                Assert.Single(solution.Projects);
                Assert.Single(solution.Warnings);
            }
        }

        [Fact]
        public void PiuSlnNellaCartella_Errore()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("A.sln", Samples.Solution());
                dir.Write("B.sln", Samples.Solution());

                Assert.Throws<InvalidOperationException>(() => SolutionLoader.Load(dir.Path));
            }
        }

        [Fact]
        public void PercorsoInesistenteOEstensioneSconosciuta_Errore()
        {
            using (var dir = new TempDirectory())
            {
                var txt = dir.Write("note.txt", "x");

                Assert.Throws<FileNotFoundException>(() => SolutionLoader.Load(Path.Combine(dir.Path, "non-esiste.sln")));
                Assert.Throws<InvalidOperationException>(() => SolutionLoader.Load(txt));
            }
        }
    }
}
