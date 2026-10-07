using System.Linq;
using ExeBuilder.Core.Analysis;
using ExeBuilder.Core.Model;
using Xunit;

namespace ExeBuilder.Core.Tests
{
    public class ProjectAnalyzerTests
    {
        private readonly ProjectAnalyzer _analyzer = new ProjectAnalyzer();

        [Fact]
        public void Progetto_SDK_WPF_net8_windows()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("App/App.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.True(info.IsSdkStyle);
                Assert.Equal("Microsoft.NET.Sdk", info.Sdk);
                Assert.Equal(OutputKind.WinExe, info.OutputKind);
                Assert.True(info.UseWpf);
                Assert.Equal("net8.0-windows", info.TargetFrameworks.Single().Moniker);
                Assert.Empty(info.Warnings);
            }
        }

        [Fact]
        public void Progetto_classico_net48_con_gruppi_condizionali_Debug_Release()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Legacy/packages.config", "<packages />");
                var path = dir.Write("Legacy/Legacy.csproj", @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <AssemblyName>LegacyApp</AssemblyName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <OutputType>WinExe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <COMReference Include=""Excel"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.False(info.IsSdkStyle);
                Assert.Equal(OutputKind.WinExe, info.OutputKind);
                Assert.Equal("LegacyApp", info.AssemblyName);
                Assert.Equal(".NET Framework 4.8", info.TargetFrameworks.Single().DisplayName);
                Assert.True(info.UsesPackagesConfig);
                Assert.True(info.HasComReferences);
            }
        }

        [Fact]
        public void TargetFrameworks_plurale_prevale_e_il_default_e_il_NET_piu_recente()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("Multi.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <TargetFrameworks>net48;net8.0;net6.0</TargetFrameworks>
  </PropertyGroup>
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.Equal(new[] { "net48", "net8.0", "net6.0" }, info.TargetFrameworks.Select(t => t.Moniker));
                Assert.Equal("net8.0", info.DefaultTargetFramework.Moniker);
            }
        }

        [Fact]
        public void Proprieta_da_Directory_Build_props_e_import_locali()
        {
            using (var dir = new TempDirectory())
            {
                dir.Write("Directory.Build.props", @"
<Project>
  <PropertyGroup>
    <AppTarget>net48</AppTarget>
  </PropertyGroup>
  <Import Project=""build\common.props"" />
</Project>");
                dir.Write("build/common.props", @"
<Project>
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>");
                var path = dir.Write("src/Tool/Tool.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>$(AppTarget)</TargetFramework>
  </PropertyGroup>
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.NotNull(info.DirectoryBuildProps);
                Assert.Equal(OutputKind.Exe, info.OutputKind);
                Assert.Equal(".NET Framework 4.8", info.TargetFrameworks.Single().DisplayName);
            }
        }

        [Fact]
        public void Choose_When_Otherwise()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("Choose.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><OutputType>Exe</OutputType></PropertyGroup>
  <Choose>
    <When Condition="" '$(Configuration)' == 'Debug' "">
      <PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup>
    </Otherwise>
  </Choose>
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.Equal("net9.0", info.TargetFrameworks.Single().Moniker);
            }
        }

        [Fact]
        public void Condizione_non_valutabile_genera_un_avviso()
        {
            using (var dir = new TempDirectory())
            {
                var path = dir.Write("Fn.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <TargetFramework Condition=""$([MSBuild]::IsOSPlatform('Linux'))"">net9.0</TargetFramework>
  </PropertyGroup>
</Project>");

                var info = _analyzer.Analyze(path);

                Assert.Equal("net8.0", info.TargetFrameworks.Single().Moniker);
                Assert.Single(info.Warnings);
            }
        }

        [Fact]
        public void Sdk_web_e_libreria_non_sono_eseguibili()
        {
            using (var dir = new TempDirectory())
            {
                var lib = dir.Write("Lib.csproj", @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>");
                var web = dir.Write("Web/Web.csproj", @"<Project Sdk=""Microsoft.NET.Sdk.Web""><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>");

                Assert.False(_analyzer.Analyze(lib).IsExecutable);
                Assert.True(_analyzer.Analyze(web).IsWebProject);
            }
        }
    }
}
