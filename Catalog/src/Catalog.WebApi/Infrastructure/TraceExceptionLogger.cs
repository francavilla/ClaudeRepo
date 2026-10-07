using System.Diagnostics;
using System.Web.Http.ExceptionHandling;

namespace Catalog.WebApi.Infrastructure
{
    /// <summary>
    /// Logger minimale su System.Diagnostics.Trace. Sostituibile con Serilog/NLog/log4net.
    /// </summary>
    public sealed class TraceExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            Trace.TraceError("{0} {1} -> {2}", context.Request.Method, context.Request.RequestUri, context.Exception);
        }
    }
}
