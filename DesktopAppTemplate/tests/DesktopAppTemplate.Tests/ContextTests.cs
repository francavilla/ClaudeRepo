using System.Collections.Generic;
using System.Linq;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Context;
using DesktopAppTemplate.Features.About;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class ContextTests
    {
        private sealed class FakeAppInfo : IAppInfo
        {
            public string Name => "MiaApp";
            public string Version => "2.3.4";
            public string Description => "Descrizione";
            public string RuntimeDescription => "Runtime X";
        }

        private sealed class FakeUi : IUiDescriptor
        {
            public string Name => "UI di prova";
        }

        private static IAppContext CreateContext()
        {
            var configuration = new ConfigurationBuilder(new[] { new OptionDefinition("Ui", "x", defaultValue: "wpf") }).Build().Configuration;

            var services = new ServiceCollection();
            services.AddAppContext(configuration);
            services.AddSingleton<IAppInfo>(new FakeAppInfo());
            services.AddSingleton<IUiDescriptor>(new FakeUi());
            return services.BuildServiceProvider().GetRequiredService<IAppContext>();
        }

        [Fact]
        public void Il_contesto_espone_configurazione_info_ambiente_e_sessione()
        {
            var context = CreateContext();

            Assert.Equal("wpf", context.Configuration.GetString("Ui"));
            Assert.Equal("MiaApp", context.App.Name);
            Assert.Equal("UI di prova", context.Environment.UiName);
            Assert.False(string.IsNullOrEmpty(context.Environment.MachineName));
            Assert.NotNull(context.Session);
        }

        [Fact]
        public void Il_contesto_e_condiviso_tra_tutti_i_consumatori()
        {
            var configuration = new ConfigurationBuilder(new OptionDefinition[0]).Build().Configuration;
            var services = new ServiceCollection();
            services.AddAppContext(configuration);
            services.AddSingleton<IAppInfo>(new FakeAppInfo());
            services.AddSingleton<IUiDescriptor>(new FakeUi());
            var provider = services.BuildServiceProvider();

            Assert.Same(provider.GetRequiredService<IAppContext>(), provider.GetRequiredService<IAppContext>());
            Assert.Same(provider.GetRequiredService<IAppContext>().Session, provider.GetRequiredService<ISessionState>());
            Assert.Same(configuration, provider.GetRequiredService<IAppConfiguration>());
        }

        [Fact]
        public void AboutViewModel_mostra_info_e_configurazione_con_origine()
        {
            var about = new AboutViewModel(CreateContext());

            Assert.Equal("MiaApp", about.AppName);
            Assert.Equal("v2.3.4", about.Version);
            Assert.Equal("UI di prova", about.UiName);
            var entry = Assert.Single(about.Configuration);
            Assert.Equal("Ui", entry.Key);
            Assert.Equal(ConfigurationBuilder.DefaultSourceName, entry.Source);
        }

        // --- Stato di sessione ---

        [Fact]
        public void La_sessione_restituisce_il_valore_o_il_predefinito()
        {
            var session = new SessionState();

            Assert.Equal(7, session.Get("n", 7));
            session.Set("n", 3);
            Assert.Equal(3, session.Get("n", 7));
            Assert.Equal("fallback", session.Get<string>("n", "fallback"));
            Assert.Equal(3, session.Get<int>("N"));
        }

        [Fact]
        public void La_sessione_notifica_solo_i_cambiamenti_reali()
        {
            var session = new SessionState();
            var changes = new List<string>();
            session.Changed += (s, e) => changes.Add(e.Key);

            session.Set("cliente", "Rossi");
            session.Set("cliente", "Rossi");
            session.Set("cliente", "Bianchi");
            Assert.True(session.Remove("cliente"));
            Assert.False(session.Remove("cliente"));

            Assert.Equal(new[] { "cliente", "cliente", "cliente" }, changes);
        }
    }
}
