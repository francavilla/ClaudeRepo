using System.Linq;
using SolutionDoctor.Core.Inventory;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class ProjectReaderTests
    {
        [Fact]
        public void ProgettoClassico_LeggeFrameworkRiferimentiEPackagesConfig()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("App/App.csproj", Samples.ClassicWinFormsProject);
                dir.Write("App/packages.config", Samples.PackagesConfig);

                var project = ProjectReader.Read(path, dir.Path);

                Assert.Equal("App", project.Name);
                Assert.Equal("App/App.csproj", project.RelativePath);
                Assert.False(project.IsSdkStyle);
                Assert.Equal(new[] { "net452" }, project.TargetFrameworks);
                Assert.Equal("WinExe", project.OutputType);
                Assert.True(project.IsWinForms);
                Assert.True(project.UsesPackagesConfig);
                Assert.Equal(2, project.Packages.Count);
                Assert.Contains("System.Web", project.AssemblyReferences);
                Assert.Contains("Newtonsoft.Json", project.AssemblyReferences);
                Assert.Equal(1, project.ComReferenceCount);
                Assert.Equal(System.IO.Path.GetFullPath(System.IO.Path.Combine(dir.Path, "Lib", "Lib.csproj")), project.ProjectReferences.Single());
            }
        }

        [Fact]
        public void ProgettoSdkStyle_LeggeMultiTargetingEPackageReference()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("App/App.csproj", Samples.SdkWinFormsProject);

                var project = ProjectReader.Read(path, dir.Path);

                Assert.True(project.IsSdkStyle);
                Assert.Equal(new[] { "net48", "net8.0-windows" }, project.TargetFrameworks);
                Assert.True(project.IsWinForms);
                Assert.False(project.UsesPackagesConfig);
                Assert.Equal(new[] { "Serilog", "Dapper" }, project.Packages.Select(p => p.Name));
                Assert.Equal("2.1.35", project.Packages.Single(p => p.Name == "Dapper").Version);
            }
        }

        [Fact]
        public void LibreriaSenzaWinForms_NonEWinForms()
        {
            using (var dir = new TempDirectory())
            {
                var project = ProjectReader.Read(dir.Write("Lib/Lib.csproj", Samples.LibProject()), dir.Path);

                Assert.False(project.IsWinForms);
                Assert.Equal("Library", project.OutputType);
            }
        }

        [Theory]
        [InlineData("net35", true)]
        [InlineData("net40", true)]
        [InlineData("net45", true)]
        [InlineData("net452", true)]
        [InlineData("net461", true)]
        [InlineData("net462", false)]
        [InlineData("net472", false)]
        [InlineData("net48", false)]
        [InlineData("net8.0", false)]
        [InlineData("net8.0-windows", false)]
        [InlineData("netstandard2.0", false)]
        public void FrameworkFuoriSupporto(string moniker, bool expected)
        {
            Assert.Equal(expected, TargetFrameworks.IsOutOfSupport(moniker));
        }
    }
}
