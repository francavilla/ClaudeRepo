using DesktopAppTemplate.Core.Hosting;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class UiSelectorTests
    {
        [Theory]
        [InlineData("--ui", "winforms", UiKind.WinForms)]
        [InlineData("--ui", "WPF", UiKind.Wpf)]
        [InlineData("--ui=winforms", null, UiKind.WinForms)]
        public void L_argomento_ha_la_precedenza_sulla_configurazione(string first, string second, UiKind expected)
        {
            var args = second == null ? new[] { first } : new[] { first, second };

            var result = UiSelector.Resolve(args, expected == UiKind.Wpf ? "winforms" : "wpf");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Senza_argomenti_vale_la_configurazione()
        {
            Assert.Equal(UiKind.WinForms, UiSelector.Resolve(new string[0], "winforms"));
        }

        [Fact]
        public void Valori_non_validi_ripiegano_sul_predefinito()
        {
            Assert.Equal(UiSelector.Default, UiSelector.Resolve(new[] { "--ui", "boh" }, "altro"));
            Assert.Equal(UiSelector.Default, UiSelector.Resolve(null, null));
        }
    }
}
