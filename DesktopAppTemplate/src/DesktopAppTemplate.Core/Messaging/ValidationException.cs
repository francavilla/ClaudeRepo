using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopAppTemplate.Core.Messaging
{
    /// <summary>Sollevata dal mediator quando una richiesta non supera la validazione.</summary>
    public sealed class ValidationException : Exception
    {
        public ValidationException(IEnumerable<string> errors)
            : base(string.Join(" ", errors))
        {
            Errors = errors.ToList();
        }

        public IReadOnlyList<string> Errors { get; }
    }
}
