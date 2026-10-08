using System;

namespace Catalog.Application.Common
{
    [Serializable]
    public class NotFoundException : Exception
    {
        public NotFoundException(string resource, object key)
            : base(resource + " con chiave '" + key + "' non trovato.")
        {
        }
    }
}
