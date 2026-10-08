using System;
using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.Tests
{
    internal sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; } = new DateTime(2026, 10, 8, 9, 30, 0);
    }

    internal sealed class FakeDialogService : IDialogService
    {
        public bool ConfirmResult { get; set; } = true;

        public bool Confirm(string title, string message) => ConfirmResult;

        public void ShowError(string title, string message)
        {
        }
    }
}
