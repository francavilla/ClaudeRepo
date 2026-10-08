using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Hosting;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class ConfigurationTests
    {
        private static readonly OptionDefinition Ui = UiSettings.UiOption;
        private static readonly OptionDefinition Folder = new OptionDefinition("DataFolder", "Cartella dei dati.", "cartella");
        private static readonly OptionDefinition ReadOnly = new OptionDefinition("ReadOnly", "Sola lettura.", isFlag: true);
        private static readonly OptionDefinition[] All = { Ui, Folder, ReadOnly };

        private static KeyValuePair<string, string> P(string key, string value) => new KeyValuePair<string, string>(key, value);

        // --- OptionDefinition ---

        [Theory]
        [InlineData("Ui", "--ui")]
        [InlineData("DataFolder", "--data-folder")]
        [InlineData("ReadOnly", "--read-only")]
        public void Il_nome_da_riga_di_comando_deriva_dalla_chiave(string key, string expected)
        {
            Assert.Equal(expected, new OptionDefinition(key, "x").CommandLineName);
        }

        [Fact]
        public void Un_flag_ha_false_come_valore_predefinito()
        {
            Assert.Equal("false", ReadOnly.DefaultValue);
            Assert.Null(ReadOnly.Validate("true"));
            Assert.NotNull(ReadOnly.Validate("forse"));
        }

        // --- CommandLineParser ---

        [Fact]
        public void La_riga_di_comando_accetta_valore_separato_o_con_uguale_e_flag()
        {
            var result = CommandLineParser.Parse(new[] { "--ui", "winforms", "--data-folder=C:\\Dati", "--read-only" }, All);

            Assert.Empty(result.Errors);
            Assert.Equal("winforms", result.Values["Ui"]);
            Assert.Equal("C:\\Dati", result.Values["DataFolder"]);
            Assert.Equal("true", result.Values["ReadOnly"]);
        }

        [Fact]
        public void Il_flag_puo_essere_disattivato_con_uguale()
        {
            var result = CommandLineParser.Parse(new[] { "--read-only=false" }, All);

            Assert.Equal("false", result.Values["ReadOnly"]);
        }

        [Fact]
        public void I_nomi_non_distinguono_maiuscole_e_l_ultimo_valore_vince()
        {
            var result = CommandLineParser.Parse(new[] { "--UI", "wpf", "--ui", "winforms" }, All);

            Assert.Equal("winforms", result.Values["Ui"]);
        }

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        [InlineData("/?")]
        public void Riconosce_la_richiesta_di_aiuto(string flag)
        {
            var result = CommandLineParser.Parse(new[] { "--ui", "wpf", flag }, All);

            Assert.True(result.HelpRequested);
            Assert.False(result.VersionRequested);
        }

        [Theory]
        [InlineData("--version")]
        [InlineData("-v")]
        public void Riconosce_la_richiesta_di_versione(string flag)
        {
            var result = CommandLineParser.Parse(new[] { flag }, All);

            Assert.True(result.VersionRequested);
            Assert.False(result.HelpRequested);
        }

        [Fact]
        public void Argomenti_sconosciuti_o_senza_valore_sono_segnalati()
        {
            var result = CommandLineParser.Parse(new[] { "--boh", "posizionale", "--ui" }, All);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, e => e.Contains("--boh"));
            Assert.Contains(result.Errors, e => e.Contains("posizionale"));
            Assert.Contains(result.Errors, e => e.Contains("--ui") && e.Contains("valore"));
        }

        [Fact]
        public void Senza_argomenti_non_ci_sono_ne_valori_ne_errori()
        {
            var result = CommandLineParser.Parse(null, All);

            Assert.Empty(result.Values);
            Assert.Empty(result.Errors);
        }

        // --- ConfigurationBuilder ---

        [Fact]
        public void Priorita_predefinito_poi_AppConfig_poi_riga_di_comando()
        {
            var built = new ConfigurationBuilder(All)
                .AddSource("App.config", new[] { P("Ui", "winforms"), P("DataFolder", "C:\\A") })
                .AddSource(ConfigurationBuilder.CommandLineSourceName, new[] { P("DataFolder", "C:\\B") })
                .Build();

            Assert.Empty(built.Errors);
            var c = built.Configuration;
            Assert.Equal("winforms", c.GetString("Ui"));
            Assert.Equal("App.config", c.GetSource("Ui"));
            Assert.Equal("C:\\B", c.GetString("DataFolder"));
            Assert.Equal(ConfigurationBuilder.CommandLineSourceName, c.GetSource("DataFolder"));
            Assert.Equal(ConfigurationBuilder.DefaultSourceName, c.GetSource("ReadOnly"));
            Assert.False(c.GetBool("ReadOnly"));
        }

        [Fact]
        public void Le_chiavi_non_dichiarate_sono_ignorate_e_le_chiavi_non_distinguono_maiuscole()
        {
            var built = new ConfigurationBuilder(All)
                .AddSource("App.config", new[] { P("altro", "x"), P("ui", "winforms") })
                .Build();

            Assert.Null(built.Configuration.GetString("altro"));
            Assert.Equal("winforms", built.Configuration.GetString("UI"));
        }

        [Fact]
        public void Un_valore_non_valido_e_segnalato_con_l_origine()
        {
            var built = new ConfigurationBuilder(All)
                .AddSource(ConfigurationBuilder.CommandLineSourceName, new[] { P("Ui", "boh") })
                .AddSource("App.config", new[] { P("ReadOnly", "forse") })
                .Build();

            Assert.Equal(2, built.Errors.Count);
            Assert.Contains(built.Errors, e => e.Contains("--ui") && e.Contains("boh") && e.Contains("riga di comando"));
            Assert.Contains(built.Errors, e => e.Contains("ReadOnly") && e.Contains("forse") && e.Contains("App.config"));
        }

        [Fact]
        public void Il_NameValueCollection_funziona_come_sorgente()
        {
            var settings = new NameValueCollection { { "Ui", "winforms" }, { "DataFolder", "  C:\\X  " } };

            var built = new ConfigurationBuilder(All).AddNameValueSource("App.config", settings).Build();

            Assert.Equal("winforms", built.Configuration.GetString("Ui"));
            Assert.Equal("C:\\X", built.Configuration.GetString("DataFolder"));
        }

        [Fact]
        public void Le_voci_sono_ordinate_per_chiave_e_riportano_l_origine()
        {
            var built = new ConfigurationBuilder(All).Build();

            Assert.Equal(new[] { "ReadOnly", "Ui" }, built.Configuration.Entries.Select(e => e.Key));
        }

        // --- UiSettings ---

        [Theory]
        [InlineData("wpf", UiKind.Wpf)]
        [InlineData("WinForms", UiKind.WinForms)]
        [InlineData("wf", UiKind.WinForms)]
        public void UiSettings_legge_la_configurazione(string value, UiKind expected)
        {
            var built = new ConfigurationBuilder(All).AddSource("App.config", new[] { P("Ui", value) }).Build();

            Assert.Equal(expected, UiSettings.From(built.Configuration).Ui);
        }

        [Fact]
        public void Senza_valore_UiSettings_usa_il_predefinito()
        {
            Assert.Equal(UiSettings.Default, UiSettings.From(new ConfigurationBuilder(All).Build().Configuration).Ui);
        }

        // --- Testo di aiuto ---

        [Fact]
        public void Il_testo_d_aiuto_deriva_dalle_opzioni()
        {
            var text = UsageText.Build("MiaApp", "2.3.4", All);

            Assert.Contains("MiaApp 2.3.4", text);
            Assert.Contains("MiaApp.exe", text);
            Assert.Contains("--ui <wpf|winforms>", text);
            Assert.Contains("--data-folder <cartella>", text);
            Assert.Contains("--read-only", text);
            Assert.Contains("predefinito: wpf", text);
            Assert.Contains("App.config: DataFolder", text);
            Assert.Contains("--version", text);
            Assert.Contains("--help", text);
        }
    }
}
