using System;
using Catalog.Application.Abstractions;

namespace Catalog.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
