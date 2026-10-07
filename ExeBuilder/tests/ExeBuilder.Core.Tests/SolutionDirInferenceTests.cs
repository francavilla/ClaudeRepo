using System.IO;
using System.Linq;
using ExeBuilder.Core.Analysis;
using ExeBuilder.Core.Model;
using ExeBuilder.Core.Planning;
using Xunit;

namespace ExeBuilder.Core.Tests
{
    /// <summary>
    /// Progetto classico con packages.config scelto da solo (senza .sln): senza $(SolutionDir)
    /// MSBuild -restore fallisce con "Non è stata trovata alcuna soluzione".
    /// </summary>
    public class SolutionDirInferenceTests
    {
        private const string LegacyWpfProject = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Newtonsoft.Json, Version=13.0.0.0"">
      <HintPath>..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>";

        private static Toolchains Tools()
        {
            var t = new Toolchains { MsBuildPath = @"C:\VS\MSBuild.exe", MsBuildVersion = new System.Version(17, 11) };
            t.InstalledTargetingPacks.Add(new System.Version(4, 8));
            return t;
        }

        [Fact]
        public void Trova_la_solution_nella_cartella_superiore_e_passa_SolutionDir()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("PopolaTabelle.v1_3/PopolaTabelle.sln", @"
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""PopolaTabelle.Wpf"", ""PopolaTabelle.Wpf\PopolaTabelle.Wpf.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject");
                dir.Write("PopolaTabelle.v1_3/PopolaTabelle.Wpf/packages.config", "<packages />");
                var project = dir.Write("PopolaTabelle.v1_3/PopolaTabelle.Wpf/PopolaTabelle.Wpf.csproj", LegacyWpfProject);

                var info = new ProjectAnalyzer().Analyze(project);
                var plan = new BuildPlanner().CreatePlan(info, new BuildOptions { OutputDirectory = @"C:\temp\out" }, Tools());

                var expectedDir = Path.Combine(dir.Path, "PopolaTabelle.v1_3");
                Assert.Equal(expectedDir, info.InferredSolutionDir);
                Assert.Contains("PopolaTabelle.sln", info.InferredSolutionDirSource);
                Assert.Contains("-p:SolutionDir=" + expectedDir + Path.DirectorySeparatorChar, plan.Arguments);
                Assert.Contains("-p:RestorePackagesConfig=true", plan.Arguments);
                Assert.Single(plan.Notes);
            }
        }

        [Fact]
        public void Senza_solution_usa_la_cartella_packages_degli_HintPath()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Repo/App/packages.config", "<packages />");
                var project = dir.Write("Repo/App/App.csproj", LegacyWpfProject);

                var info = new ProjectAnalyzer().Analyze(project);

                Assert.Equal(Path.Combine(dir.Path, "Repo"), info.InferredSolutionDir);
                Assert.Contains("HintPath", info.InferredSolutionDirSource);
            }
        }

        [Fact]
        public void Una_solution_che_non_contiene_il_progetto_viene_ignorata()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Altro.sln", @"
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Altro"", ""Altro\Altro.csproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject");
                var project = dir.Write("App/App.csproj", @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

                var info = new ProjectAnalyzer().Analyze(project);

                Assert.Null(info.InferredSolutionDir);
            }
        }

        [Fact]
        public void Con_la_solution_scelta_dall_utente_non_serve_dedurla()
        {
            using (var dir = new TempDirectory())
            {
                var sln = dir.Write("All.sln", string.Empty);
                var project = dir.Write("App/App.csproj", LegacyWpfProject);

                var info = new ProjectAnalyzer().Analyze(project, solutionPath: sln);
                var plan = new BuildPlanner().CreatePlan(info, new BuildOptions { OutputDirectory = @"C:\out", SolutionPath = sln }, Tools());

                Assert.Null(info.InferredSolutionDir);
                Assert.Single(plan.Arguments.Where(a => a.StartsWith("-p:SolutionDir=", System.StringComparison.Ordinal)));
                Assert.Empty(plan.Notes);
            }
        }

        [Fact]
        public void Packages_config_senza_alcuna_solution_genera_un_avviso()
        {
            var info = new ProjectInfo(Path.Combine(Path.GetTempPath(), "Solo", "Solo.csproj"))
            {
                OutputKind = OutputKind.Exe,
                UsesPackagesConfig = true
            };
            info.TargetFrameworks.Add(TargetFramework.FromIdentifier(null, "v4.8"));

            var plan = new BuildPlanner().CreatePlan(info, new BuildOptions { OutputDirectory = @"C:\out" }, Tools());

            Assert.Contains(plan.Warnings, w => w.Contains("packages.config"));
        }
    }
}
