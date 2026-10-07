using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Catalog.WebApi.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Catalog.WebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Composition root: unico punto in cui si conoscono tutte le implementazioni concrete.
            ContainerConfig.Register(config);

            config.MapHttpAttributeRoutes();

            // Solo JSON, camelCase, date in UTC ISO-8601.
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            var json = config.Formatters.JsonFormatter.SerializerSettings;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            json.NullValueHandling = NullValueHandling.Ignore;

            config.Filters.Add(new ValidateModelAttribute());

            config.Services.Replace(typeof(IExceptionHandler), new ApiExceptionHandler());
            config.Services.Add(typeof(IExceptionLogger), new TraceExceptionLogger());

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.LocalOnly;
        }
    }
}
