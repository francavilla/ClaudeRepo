using System;
using System.Collections.Generic;

namespace Catalog.Application.Common
{
    /// <summary>
    /// Input non valido a livello di caso d'uso (diverso da una violazione di regola di dominio).
    /// </summary>
    [Serializable]
    public class ValidationException : Exception
    {
        public ValidationException(IDictionary<string, string> errors)
            : base("Uno o più campi non sono validi.")
        {
            Errors = new Dictionary<string, string>(errors);
        }

        public ValidationException(string field, string error)
            : this(new Dictionary<string, string> { { field, error } })
        {
        }

        public IReadOnlyDictionary<string, string> Errors { get; private set; }
    }
}
