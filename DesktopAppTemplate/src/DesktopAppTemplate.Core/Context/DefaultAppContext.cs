using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;

namespace DesktopAppTemplate.Core.Context
{
    /// <summary>Contesto predefinito: raccoglie le parti già registrate nel contenitore.</summary>
    public sealed class DefaultAppContext : IAppContext
    {
        public DefaultAppContext(IAppConfiguration configuration, IAppInfo app, IAppEnvironment environment, ISessionState session)
        {
            Configuration = configuration;
            App = app;
            Environment = environment;
            Session = session;
        }

        public IAppConfiguration Configuration { get; }
        public IAppInfo App { get; }
        public IAppEnvironment Environment { get; }
        public ISessionState Session { get; }
    }
}
