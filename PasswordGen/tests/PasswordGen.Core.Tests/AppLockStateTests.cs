using System;
using PasswordGen.Core.Security;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class AppLockStateTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 10, 0, 0);

        private static AppLockState Create(bool enabled = true, int graceSeconds = 30)
        {
            return new AppLockState(TimeSpan.FromSeconds(graceSeconds)) { Enabled = enabled };
        }

        [Fact]
        public void Start_WhenEnabled_IsLocked()
        {
            var state = Create();

            state.Start();

            Assert.True(state.IsLocked);
        }

        [Fact]
        public void Start_WhenDisabled_IsNotLocked()
        {
            var state = Create(enabled: false);

            state.Start();

            Assert.False(state.IsLocked);
        }

        [Fact]
        public void Unlocked_ClearsTheLock()
        {
            var state = Create();
            state.Start();

            state.Unlocked();

            Assert.False(state.IsLocked);
        }

        [Fact]
        public void Foregrounded_WithinTheGracePeriod_StaysUnlocked()
        {
            var state = Create(graceSeconds: 30);
            state.Start();
            state.Unlocked();

            state.Backgrounded(T0);

            Assert.False(state.Foregrounded(T0.AddSeconds(29)));
        }

        [Fact]
        public void Foregrounded_AfterTheGracePeriod_Locks()
        {
            var state = Create(graceSeconds: 30);
            state.Start();
            state.Unlocked();

            state.Backgrounded(T0);

            Assert.True(state.Foregrounded(T0.AddSeconds(30)));
            Assert.True(state.IsLocked);
        }

        [Fact]
        public void Foregrounded_WithZeroGrace_AlwaysLocks()
        {
            var state = Create(graceSeconds: 0);
            state.Start();
            state.Unlocked();

            state.Backgrounded(T0);

            Assert.True(state.Foregrounded(T0));
        }

        [Fact]
        public void Foregrounded_WhenDisabled_NeverLocks()
        {
            var state = Create(enabled: false, graceSeconds: 0);
            state.Backgrounded(T0);

            Assert.False(state.Foregrounded(T0.AddHours(5)));
        }

        [Fact]
        public void Foregrounded_WithoutBackgrounding_KeepsTheCurrentState()
        {
            var state = Create();
            state.Start();
            state.Unlocked();

            Assert.False(state.Foregrounded(T0.AddHours(1)));
        }

        [Fact]
        public void Backgrounded_Twice_UsesTheFirstTime()
        {
            var state = Create(graceSeconds: 30);
            state.Start();
            state.Unlocked();

            state.Backgrounded(T0);
            state.Backgrounded(T0.AddSeconds(25));   // secondo evento di arresto senza ritorno in primo piano

            Assert.True(state.Foregrounded(T0.AddSeconds(31)));
        }

        [Fact]
        public void Foregrounded_WhileStillLocked_RemainsLocked()
        {
            var state = Create();
            state.Start();   // bloccato all'avvio

            state.Backgrounded(T0);

            Assert.True(state.Foregrounded(T0.AddSeconds(1)));
        }

        [Fact]
        public void DisablingTheLock_UnlocksImmediately()
        {
            var state = Create();
            state.Start();

            state.Enabled = false;

            Assert.False(state.IsLocked);
        }

        [Fact]
        public void NegativeGrace_IsTreatedAsZero()
        {
            var state = new AppLockState(TimeSpan.FromSeconds(-5)) { Enabled = true };

            Assert.Equal(TimeSpan.Zero, state.GracePeriod);
        }
    }
}
