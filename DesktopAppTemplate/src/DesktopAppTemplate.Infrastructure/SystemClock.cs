using System;
using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
