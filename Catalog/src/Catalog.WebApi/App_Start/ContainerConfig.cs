using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Catalog.Application.Abstractions;
using Catalog.Application.Products.Commands;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Queries;
using Catalog.Infrastructure.Persistence.Repositories;

namespace Catalog.WebApi
{
    public static class ContainerConfig
    {
        public static IContainer Register(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterWebApiFilterProvider(config);

            // Application: tutti gli handler per convenzione (scansione dell'assembly).
            var applicationAssembly = typeof(CreateProductHandler).Assembly;
            builder.RegisterAssemblyTypes(applicationAssembly)
                .AsClosedTypesOf(typeof(ICommandHandler<>))
                .InstancePerRequest();
            builder.RegisterAssemblyTypes(applicationAssembly)
                .AsClosedTypesOf(typeof(ICommandHandler<,>))
                .InstancePerRequest();
            builder.RegisterAssemblyTypes(applicationAssembly)
                .AsClosedTypesOf(typeof(IQueryHandler<,>))
                .InstancePerRequest();

            // Infrastructure: un DbContext per richiesta, condiviso da repository, query e unit of work.
            builder.RegisterType<CatalogDbContext>()
                .AsSelf()
                .As<IUnitOfWork>()
                .InstancePerRequest();
            builder.RegisterAssemblyTypes(typeof(ProductRepository).Assembly)
                .Where(t => t.Namespace == typeof(ProductRepository).Namespace
                         || t.Namespace == typeof(ProductQueries).Namespace)
                .AsImplementedInterfaces()
                .InstancePerRequest();
            builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();

            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
            return container;
        }
    }
}
