namespace SolutionDoctor.Core.Tests
{
    /// <summary>Contenuti di esempio di file di progetto e solution.</summary>
    internal static class Samples
    {
        public const string ClassicWinFormsProject = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.5.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" />
    <Reference Include=""System.Runtime.Remoting"" />
    <Reference Include=""Newtonsoft.Json, Version=13.0.0.0"">
      <HintPath>..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll</HintPath>
    </Reference>
    <COMReference Include=""Excel"" />
  </ItemGroup>
  <ItemGroup>
    <None Include=""packages.config"" />
    <ProjectReference Include=""..\Lib\Lib.csproj"" />
  </ItemGroup>
</Project>";

        public const string PackagesConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Newtonsoft.Json"" version=""13.0.3"" targetFramework=""net452"" />
  <package id=""Dapper"" version=""2.1.35"" targetFramework=""net452"" />
</packages>";

        public const string SdkWinFormsProject = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
    <PackageReference Include=""Dapper""><Version>2.1.35</Version></PackageReference>
  </ItemGroup>
</Project>";

        public static string LibProject(string targetFramework = "net48")
        {
            return @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFramework>" + targetFramework + "</TargetFramework></PropertyGroup></Project>";
        }

        public static string Solution(params string[] projects)
        {
            var text = "Microsoft Visual Studio Solution File, Format Version 12.00\r\n";
            foreach (var project in projects)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(project);
                text += "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"" + name + "\", \"" + project + "\", \"{00000000-0000-0000-0000-000000000001}\"\r\nEndProject\r\n";
            }

            text += "Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Cartella\", \"Cartella\", \"{00000000-0000-0000-0000-000000000002}\"\r\nEndProject\r\n";
            return text;
        }
    }
}
