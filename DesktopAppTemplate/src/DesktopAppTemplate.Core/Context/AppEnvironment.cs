using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.Core.Context
{
    public sealed class AppEnvironment : IAppEnvironment
    {
        private readonly IUiDescriptor _ui;

        public AppEnvironment(IUiDescriptor ui)
        {
            _ui = ui;
        }

        public string UiName => _ui.Name;
        public string UserName => System.Environment.UserName;
        public string MachineName => System.Environment.MachineName;
    }
}
