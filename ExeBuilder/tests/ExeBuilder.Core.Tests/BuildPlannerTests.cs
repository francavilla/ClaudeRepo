using System;
using System.IO;
using ExeBuilder.Core.Execution;
using ExeBuilder.Core.Model;
using ExeBuilder.Core.Planning;
using Xunit;

namespace ExeBuilder.Core.Tests
{
    public class BuildPlannerTests
    {
        private const string Out = @"C:\out\My App";

        private readonly BuildPlanner _planner = new BuildPlanner();

        private static ProjectInfo Project(bool sdkStyle, OutputKind kind, params string[] tfms)
        {
            var info = new ProjectInfo(Path.Combine(Path.GetTempPath(), "App", "App.csproj"))
            {
                IsSdkStyle = sdkStyle,
                Sdk = sdkStyle ? "Microsoft.NET.Sdk" : null,
                OutputKind = kind
            };

            foreach (var tfm in tfms)
            {
                info.TargetFrameworks.Add(sdkStyle ? TargetFramework.Parse(tfm) : TargetFramework.FromIdentifier(null, tfm));
            }

            return info;
        }

        private static Toolchains Tools(string sdks = "6.0.428;8.0.400", string msbuild = "17.11.2", string packs = "4.6.2;4.8")
        {
            var t = new Toolchains
            {
                DotNetPath = sdks.Length > 0 ? @"C:\Program Files\dotnet\dotnet.exe" : null,
                MsBuildPath = msbuild.Length > 0 ? @"C:\VS\MSBuild\Current\Bin\MSBuild.exe" : null,
                MsBuildVersion = msbuild.Length > 0 ? Version.Parse(msbuild) : null
            };
            foreach (var v in sdks.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                t.DotNetSdks.Add(Version.Parse(v));
            }

            foreach (var v in packs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                t.InstalledTargetingPacks.Add(Version.Parse(v));
            }

            return t;
        }

        private static BuildOptions Options()
        {
            return new BuildOptions { OutputDirectory = Out };
        }

        [Fact]
        public void Sdk_style_net8_usa_dotnet_publish_in_Release()
        {
            var plan = _planner.CreatePlan(Project(true, OutputKind.WinExe, "net8.0-windows"), Options(), Tools());

            Assert.True(plan.CanBuild, string.Join("; ", plan.Errors));
            Assert.Equal(BuildTool.DotNetCli, plan.Tool);
            Assert.Equal(new[] { "publish", plan.Arguments[1], "-c", "Release", "-f", "net8.0-windows", "-o", Out, "--nologo" }, plan.Arguments);
            Assert.Contains("SDK 8.0.400", plan.Description);
        }

        [Fact]
        public void Sdk_style_net9_senza_SDK_9_installato_e_un_errore()
        {
            var plan = _planner.CreatePlan(Project(true, OutputKind.Exe, "net9.0"), Options(), Tools());

            Assert.False(plan.CanBuild);
            Assert.Contains(plan.Errors, e => e.Contains(".NET SDK 9.0"));
        }

        [Fact]
        public void Self_contained_single_file_solo_per_NET()
        {
            var options = Options();
            options.SelfContained = true;
            options.SingleFile = true;
            options.RuntimeIdentifier = "win-arm64";

            var plan = _planner.CreatePlan(Project(true, OutputKind.Exe, "net8.0"), options, Tools());

            Assert.Contains("-r", plan.Arguments);
            Assert.Contains("win-arm64", plan.Arguments);
            Assert.Contains("-p:PublishSingleFile=true", plan.Arguments);
        }

        [Fact]
        public void Sdk_style_net48_usa_dotnet_e_ignora_self_contained()
        {
            var options = Options();
            options.SelfContained = true;

            var plan = _planner.CreatePlan(Project(true, OutputKind.WinExe, "net48"), options, Tools());

            Assert.Equal(BuildTool.DotNetCli, plan.Tool);
            Assert.DoesNotContain("--self-contained", plan.Arguments);
            Assert.Contains(plan.Warnings, w => w.Contains("Self-contained"));
        }

        [Fact]
        public void Classico_net48_usa_MSBuild_con_OutDir()
        {
            var project = Project(false, OutputKind.WinExe, "v4.8");
            project.UsesPackagesConfig = true;

            var plan = _planner.CreatePlan(project, Options(), Tools());

            Assert.True(plan.CanBuild, string.Join("; ", plan.Errors));
            Assert.Equal(BuildTool.MsBuild, plan.Tool);
            Assert.Contains("-p:Configuration=Release", plan.Arguments);
            Assert.Contains("-p:OutDir=" + Out + Path.DirectorySeparatorChar, plan.Arguments);
            Assert.Contains("-p:RestorePackagesConfig=true", plan.Arguments);
            Assert.Contains("-restore", plan.Arguments);
        }

        [Fact]
        public void Classico_senza_targeting_pack_e_un_errore()
        {
            var plan = _planner.CreatePlan(Project(false, OutputKind.Exe, "v4.7.2"), Options(), Tools());

            Assert.False(plan.CanBuild);
            Assert.Contains(plan.Errors, e => e.Contains("MSB3644"));
        }

        [Fact]
        public void Classico_con_solo_MSBuild_del_Framework_e_un_errore()
        {
            var plan = _planner.CreatePlan(Project(false, OutputKind.Exe, "v4.8"), Options(), Tools(msbuild: "4.8.9032"));

            Assert.False(plan.CanBuild);
            Assert.Contains(plan.Errors, e => e.Contains("Build Tools"));
        }

        [Fact]
        public void Libreria_non_produce_eseguibile()
        {
            var plan = _planner.CreatePlan(Project(true, OutputKind.Library, "net8.0"), Options(), Tools());

            Assert.False(plan.CanBuild);
            Assert.Contains(plan.Errors, e => e.Contains("OutputType"));
        }

        [Fact]
        public void Sdk_style_con_COMReference_usa_MSBuild_publish()
        {
            var project = Project(true, OutputKind.WinExe, "net48");
            project.HasComReferences = true;

            var plan = _planner.CreatePlan(project, Options(), Tools());

            Assert.Equal(BuildTool.MsBuild, plan.Tool);
            Assert.Contains("-t:Publish", plan.Arguments);
        }

        [Fact]
        public void Da_solution_passa_SolutionDir()
        {
            var options = Options();
            options.SolutionPath = Path.Combine(Path.GetTempPath(), "All.sln");

            var plan = _planner.CreatePlan(Project(true, OutputKind.Exe, "net8.0"), options, Tools());

            Assert.Contains(plan.Arguments, a => a.StartsWith("-p:SolutionDir=", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("net48", false, 2)]
        [InlineData("net48", true, 3)]
        [InlineData("netcoreapp3.1", false, 3)]
        [InlineData("net8.0", false, 8)]
        [InlineData("net5.0-windows10.0.19041.0", false, 5)]
        public void Versione_minima_dell_SDK(string tfm, bool wpf, int expected)
        {
            var project = Project(true, OutputKind.Exe, tfm);
            project.UseWpf = wpf;

            Assert.Equal(expected, BuildPlanner.RequiredSdkMajor(project, project.TargetFrameworks[0]));
        }
    }

    public class CommandLineTests
    {
        [Theory]
        [InlineData("semplice", "semplice")]
        [InlineData("con spazi", "\"con spazi\"")]
        [InlineData(@"-p:OutDir=C:\my out\", "\"-p:OutDir=C:\\my out\\\\\"")]
        [InlineData("a\"b", "\"a\\\"b\"")]
        [InlineData("", "\"\"")]
        public void Quote_segue_le_regole_di_CommandLineToArgvW(string input, string expected)
        {
            Assert.Equal(expected, CommandLine.Quote(input));
        }
    }
}
