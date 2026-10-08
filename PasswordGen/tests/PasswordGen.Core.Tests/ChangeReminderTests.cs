using System;
using PasswordGen.Core.Reminder;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class ChangeReminderTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 8);

        [Fact]
        public void Disabled_ReportsDisabled()
        {
            var state = ChangeReminder.Evaluate(false, Today, 30, 5, Today);

            Assert.Equal(ReminderStatus.Disabled, state.Status);
            Assert.False(state.ShouldAlert);
        }

        [Fact]
        public void NoRecordedChange_CannotComputeTheDeadline()
        {
            var state = ChangeReminder.Evaluate(true, null, 30, 5, Today);

            Assert.Equal(ReminderStatus.NeverRecorded, state.Status);
            Assert.Null(state.DaysRemaining);
            Assert.False(state.ShouldAlert);
        }

        [Theory]
        [InlineData(0, 30, ReminderStatus.Ok)]
        [InlineData(10, 20, ReminderStatus.Ok)]
        [InlineData(24, 6, ReminderStatus.Ok)]
        [InlineData(25, 5, ReminderStatus.DueSoon)]
        [InlineData(29, 1, ReminderStatus.DueSoon)]
        [InlineData(30, 0, ReminderStatus.DueSoon)]
        [InlineData(31, -1, ReminderStatus.Expired)]
        [InlineData(45, -15, ReminderStatus.Expired)]
        public void Evaluate_ComputesStatusFromElapsedDays(int elapsedDays, int expectedRemaining, ReminderStatus expected)
        {
            var state = ChangeReminder.Evaluate(true, Today.AddDays(-elapsedDays), 30, 5, Today);

            Assert.Equal(expected, state.Status);
            Assert.Equal(expectedRemaining, state.DaysRemaining);
            Assert.Equal(expected == ReminderStatus.DueSoon || expected == ReminderStatus.Expired, state.ShouldAlert);
        }

        [Theory]
        [InlineData(10, "tra 20 giorni")]
        [InlineData(29, "domani")]
        [InlineData(30, "oggi")]
        [InlineData(31, "scaduta da 1 giorno")]
        [InlineData(35, "scaduta da 5 giorni")]
        public void Evaluate_WritesAReadableMessage(int elapsedDays, string fragment)
        {
            var state = ChangeReminder.Evaluate(true, Today.AddDays(-elapsedDays), 30, 5, Today);

            Assert.Contains(fragment, state.Message);
        }

        [Fact]
        public void Evaluate_IgnoresTheTimeOfDay()
        {
            var state = ChangeReminder.Evaluate(true, new DateTime(2026, 9, 8, 23, 59, 0), 30, 5, new DateTime(2026, 10, 8, 0, 1, 0));

            Assert.Equal(0, state.DaysRemaining);
        }
    }
}
