using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using Catalog.Application.Common;
using Catalog.Domain.Common;

namespace Catalog.WebApi.Infrastructure
{
    /// <summary>
    /// Traduzione centralizzata eccezione → risposta HTTP. I controller non usano try/catch.
    /// </summary>
    public sealed class ApiExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            var exception = context.Exception;
            var request = context.Request;

            var notFound = exception as NotFoundException;
            if (notFound != null)
            {
                context.Result = Error(request, HttpStatusCode.NotFound, notFound.Message);
                return;
            }

            var validation = exception as ValidationException;
            if (validation != null)
            {
                var error = new HttpError(validation.Message);
                var modelState = new HttpError();
                foreach (var item in validation.Errors)
                {
                    modelState.Add(item.Key, new[] { item.Value });
                }

                error.Add("ModelState", modelState);
                context.Result = new ResponseMessageResult(request.CreateErrorResponse(HttpStatusCode.BadRequest, error));
                return;
            }

            var domain = exception as DomainException;
            if (domain != null)
            {
                context.Result = Error(request, (HttpStatusCode)422, domain.Message);
                return;
            }

            // Eccezione inattesa: nessun dettaglio interno verso il client (già tracciata dal logger).
            context.Result = Error(request, HttpStatusCode.InternalServerError, "Si è verificato un errore imprevisto.");
        }

        private static IHttpActionResult Error(HttpRequestMessage request, HttpStatusCode status, string message)
        {
            return new ResponseMessageResult(request.CreateErrorResponse(status, message));
        }
    }
}
