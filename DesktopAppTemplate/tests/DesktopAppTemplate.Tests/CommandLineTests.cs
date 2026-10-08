using DesktopAppTemplate.Core.Hosting;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class CommandLineTests
    {
        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        [InlineData("/?")]
        [InlineData("--HELP")]
        public void Riconosce_le_richieste_di_aiuto(string flag)
        {
            Assert.True(CommandLine.IsHelpRequested(new[] { "--ui", "wpf", flag }));
            Assert.False(CommandLine.IsVersionRequested(new[] { flag }));
        }

        [Theory]
        [InlineData("--version")]
        [InlineData("-v")]
        public void Riconosce_le_richieste_di_versione(string flag)
        {
            Assert.True(CommandLine.IsVersionRequested(new[] { flag }));
            Assert.False(CommandLine.IsHelpRequested(new[] { flag }));
        }

        [Fact]
        public void Senza_opzioni_informative_non_richiede_nulla()
        {
            Assert.False(CommandLine.IsHelpRequested(new[] { "--ui", "winforms" }));
            Assert.False(CommandLine.IsVersionRequested(null));
        }

        [Fact]
        public void Il_testo_d_aiuto_riporta_nome_versione_e_opzioni()
        {
            var text = CommandLine.GetUsage("MiaApp", "2.3.4");

            Assert.Contains("MiaApp 2.3.4", text);
            Assert.Contains("MiaApp.exe", text);
            Assert.Contains("--ui", text);
            Assert.Contains("--help", text);
            Assert.Contains("--version", text);
        }
    }
}
