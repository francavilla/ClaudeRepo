using System;

namespace Catalog.Domain.Common
{
    /// <summary>
    /// Violazione di una regola di business. Il livello di presentazione la traduce in HTTP 400/422.
    /// </summary>
    [Serializable]
    public class DomainException : Exception
    {
        public DomainException(string message)
            : base(message)
        {
        }
    }
}
