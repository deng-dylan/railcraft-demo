using NUnit.Framework;
using UnityEngine;

namespace RailCraft.ThirdPerson.Player.Tests
{
    public sealed class ThirdPersonInputLockTests
    {
        [Test]
        public void LockStateChangesAreObservableAndIdempotent()
        {
            var player = new GameObject("PlayerInputLockTest");
            try
            {
                var inputLock = player.AddComponent<ThirdPersonInputLock>();
                var notificationCount = 0;
                var lastState = false;
                inputLock.InputLockChanged += state =>
                {
                    notificationCount++;
                    lastState = state;
                };

                inputLock.SetInputLocked(true);
                inputLock.SetInputLocked(true);

                Assert.That(inputLock.InputLocked, Is.True);
                Assert.That(notificationCount, Is.EqualTo(1));
                Assert.That(lastState, Is.True);

                inputLock.SetInputLocked(false);

                Assert.That(inputLock.InputLocked, Is.False);
                Assert.That(notificationCount, Is.EqualTo(2));
                Assert.That(lastState, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
