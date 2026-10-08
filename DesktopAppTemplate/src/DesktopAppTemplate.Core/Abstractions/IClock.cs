using System;

namespace DesktopAppTemplate.Core.Abstractions
{
    /// <summary>Fornisce l'ora corrente (sostituibile nei test).</summary>
    public interface IClock
    {
        DateTime Now { get; }
    }
}
