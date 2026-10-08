using System;

namespace Catalog.Domain.Common
{
    /// <summary>
    /// Base per le entità del dominio: l'uguaglianza è basata sull'identità.
    /// </summary>
    public abstract class Entity
    {
        public Guid Id { get; protected set; }

        public override bool Equals(object obj)
        {
            var other = obj as Entity;
            if (ReferenceEquals(other, null) || other.GetType() != GetType())
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id != Guid.Empty && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return (GetType().FullName + Id).GetHashCode();
        }
    }
}
