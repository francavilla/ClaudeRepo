using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopAppTemplate.Core;
using DesktopAppTemplate.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DesktopAppTemplate.Tests
{
    public class MediatorTests
    {
        private sealed class Ping : IRequest<string>
        {
            public string Text { get; set; }
        }

        private sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult("pong " + request.Text);
        }

        private sealed class PingValidator : IValidator<Ping>
        {
            public IEnumerable<string> Validate(Ping request)
            {
                if (string.IsNullOrEmpty(request.Text))
                    yield return "Testo mancante.";
            }
        }

        private sealed class Orphan : IRequest<int>
        {
        }

        private static IMediator Create(bool withValidator)
        {
            var services = new ServiceCollection();
            services.AddCore();
            services.AddTransient<IRequestHandler<Ping, string>, PingHandler>();
            if (withValidator)
                services.AddTransient<IValidator<Ping>, PingValidator>();
            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Send_instrada_la_richiesta_all_handler()
        {
            var result = await Create(false).Send(new Ping { Text = "a" });

            Assert.Equal("pong a", result);
        }

        [Fact]
        public async Task Send_con_richiesta_non_valida_solleva_ValidationException()
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(() => Create(true).Send(new Ping()));

            Assert.Equal(new[] { "Testo mancante." }, ex.Errors);
        }

        [Fact]
        public async Task Send_con_richiesta_valida_supera_la_validazione()
        {
            var result = await Create(true).Send(new Ping { Text = "ok" });

            Assert.Equal("pong ok", result);
        }

        [Fact]
        public async Task Send_senza_handler_solleva_InvalidOperationException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Create(false).Send(new Orphan()));
        }
    }
}
