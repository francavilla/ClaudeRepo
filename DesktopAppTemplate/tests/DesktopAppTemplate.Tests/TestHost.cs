using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Messaging;
using DesktopAppTemplate.Features;
using DesktopAppTemplate.Features.Tasks;
using DesktopAppTemplate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopAppTemplate.Tests
{
    /// <summary>Costruisce un mediator con gli handler reali delle slice e un archivio in memoria vuoto.</summary>
    internal sealed class TestHost
    {
        public TestHost()
        {
            Repository = new InMemoryTaskRepository();
            Clock = new FakeClock();

            var services = new ServiceCollection();
            services.AddCore();
            services.AddHandlersFromAssembly(typeof(FeaturesServiceCollectionExtensions).Assembly);
            services.AddSingleton<ITaskRepository>(Repository);
            services.AddSingleton<IClock>(Clock);

            Provider = services.BuildServiceProvider();
            Mediator = Provider.GetRequiredService<IMediator>();
        }

        public InMemoryTaskRepository Repository { get; }
        public FakeClock Clock { get; }
        public ServiceProvider Provider { get; }
        public IMediator Mediator { get; }
    }
}
