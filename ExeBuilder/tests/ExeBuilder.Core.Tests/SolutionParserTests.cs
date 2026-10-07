using System.Linq;
using ExeBuilder.Core.Analysis;
using Xunit;

namespace ExeBuilder.Core.Tests
{
    public class SolutionParserTests
    {
        [Fact]
        public void Sln_restituisce_i_progetti_e_scarta_le_cartelle_di_solution()
        {
            using (var dir = new TempDirectory())
            {
                var sln = dir.Write("All.sln", @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""App"", ""src\App\App.csproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Legacy"", ""src\Legacy\Legacy.vbproj"", ""{33333333-3333-3333-3333-333333333333}""
EndProject
Project(""{54435603-DBB4-11D2-8724-00A0C9A8B90C}"") = ""Setup"", ""setup\Setup.vdproj"", ""{44444444-4444-4444-4444-444444444444}""
EndProject
");

                var projects = new SolutionParser().Parse(sln);

                Assert.Equal(new[] { "App", "Legacy" }, projects.Select(p => p.Name));
                Assert.All(projects, p => Assert.StartsWith(dir.Path, p.FullPath));
            }
        }

        [Fact]
        public void Slnx_formato_xml()
        {
            using (var dir = new TempDirectory())
            {
                var slnx = dir.Write("All.slnx", @"
<Solution>
  <Folder Name=""/src/"">
    <Project Path=""src/Tool/Tool.csproj"" />
  </Folder>
  <Project Path=""tests/Tool.Tests/Tool.Tests.csproj"" />
</Solution>");

                var projects = new SolutionParser().Parse(slnx);

                Assert.Equal(new[] { "Tool", "Tool.Tests" }, projects.Select(p => p.Name));
            }
        }
    }
}
