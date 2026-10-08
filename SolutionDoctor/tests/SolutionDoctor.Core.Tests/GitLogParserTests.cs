using SolutionDoctor.Core.Git;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class GitLogParserTests
    {
        [Fact]
        public void ContaIlNumeroDiCommitPerFile()
        {
            var churn = GitLogParser.Parse("App/Form1.cs\nApp/Form1.Designer.cs\n\nApp/Form1.cs\r\n\nLib/Util.cs\n");

            Assert.Equal(2, churn["App/Form1.cs"]);
            Assert.Equal(1, churn["App/Form1.Designer.cs"]);
            Assert.Equal(1, churn["Lib/Util.cs"]);
            Assert.Equal(3, churn.Count);
        }

        [Fact]
        public void SeparatoriWindowsENomiSenzaDistinzioneMaiuscole()
        {
            var churn = GitLogParser.Parse("App\\Form1.cs\nAPP/FORM1.CS\n");

            Assert.Equal(2, churn["app/form1.cs"]);
        }

        [Fact]
        public void OutputVuotoONullo_NessunDato()
        {
            Assert.Empty(GitLogParser.Parse(string.Empty));
            Assert.Empty(GitLogParser.Parse(null));
        }

        [Theory]
        [InlineData(@"C:\Progetti\App", @"""C:\Progetti\App""")]
        [InlineData(@"C:\Progetti\App\", @"""C:\Progetti\App\\""")]   // '\' finale raddoppiato
        [InlineData(@"C:\", @"""C:\\""")]
        [InlineData("/home/utente/app", @"""/home/utente/app""")]
        public void PercorsoTraVirgolette_ConBackslashFinale(string path, string expected)
        {
            Assert.Equal(expected, GitChurnProvider.Quote(path));
        }
    }
}
