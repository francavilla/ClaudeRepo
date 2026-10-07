using System.Data.Entity;
using System.Web;
using System.Web.Http;
using Catalog.Infrastructure.Persistence;

namespace Catalog.WebApi
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Skeleton: crea il DB al primo accesso. In produzione usare le EF Migrations
            // (Enable-Migrations) e impostare l'initializer a null.
            Database.SetInitializer(new CreateDatabaseIfNotExists<CatalogDbContext>());

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
