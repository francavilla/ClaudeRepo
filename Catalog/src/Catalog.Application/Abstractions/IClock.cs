using System;

namespace Catalog.Application.Abstractions
{
    /// <summary>Astrazione del tempo: rende deterministici i test.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
