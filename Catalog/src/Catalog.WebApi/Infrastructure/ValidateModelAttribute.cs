using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Catalog.WebApi.Infrastructure
{
    /// <summary>
    /// Rifiuta con 400 le richieste con body mancante o ModelState non valido,
    /// prima che arrivino al controller.
    /// </summary>
    public sealed class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            foreach (var parameter in actionContext.ActionDescriptor.GetParameters())
            {
                var isBody = parameter.ParameterBinderAttribute is System.Web.Http.FromBodyAttribute;
                object value;
                actionContext.ActionArguments.TryGetValue(parameter.ParameterName, out value);
                if (isBody && value == null)
                {
                    actionContext.ModelState.AddModelError(parameter.ParameterName, "Il corpo della richiesta è obbligatorio.");
                }
            }

            if (!actionContext.ModelState.IsValid)
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest, actionContext.ModelState);
            }
        }
    }
}
