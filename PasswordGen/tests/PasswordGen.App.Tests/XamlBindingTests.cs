using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace PasswordGen.App.Tests
{
    /// <summary>
    /// Controlli sul file XAML della finestra: un errore qui compare solo all'apertura dell'app (non in compilazione), quindi lo si cerca nel testo.
    /// </summary>
    public class XamlBindingTests
    {
        private static string ReadMainWindow([CallerFilePath] string thisFile = "")
        {
            var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile), "..", "..", "src", "PasswordGen", "MainWindow.xaml"));
            return File.ReadAllText(path);
        }

        [Fact]
        public void BarreDiAvanzamento_LeggonoLeProprietaInSolaLetturaInUnaDirezione()
        {
            // ProgressBar.Value si associa in due direzioni di base: con una proprietà di sola lettura l'app va in errore all'apertura.
            var xaml = ReadMainWindow();
            var bindings = Regex.Matches(xaml, "<ProgressBar[^>]*?Value=\"\\{Binding ([^}]*)\\}\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            Assert.NotEmpty(bindings);
            Assert.All(bindings, b => Assert.Contains("Mode=OneWay", b));
        }
    }
}
