using System.Collections.Generic;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>Valida una richiesta prima che arrivi all'handler. Restituisce i messaggi di errore (vuoto = valida).</summary>
    public interface IValidator<TRequest>
    {
        IEnumerable<string> Validate(TRequest request);
    }
}
