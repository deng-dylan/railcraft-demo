using NUnit.Framework;
using UnityEngine;

namespace RailCraft.ThirdPerson.Player.Tests
{
    public sealed class PlayerInteractionScannerTests
    {
        private static readonly Vector3 TestOrigin = new Vector3(10000f, 10000f, 10000f);

        [Test]
        public void ScanChoosesTheBestEligibleTargetAndInteractsOnce()
        {
            var player = new GameObject("InteractionPlayerTest");
            player.transform.position = TestOrigin;
            var nearTarget = CreateTarget("NearTarget", TestOrigin + new Vector3(0f, 0f, 1f), true);
            var farTarget = CreateTarget("FarTarget", TestOrigin + new Vector3(0f, 0f, 2f), true);
            player.SetActive(false);
            try
            {
                var inputLock = player.AddComponent<ThirdPersonInputLock>();
                var scanner = player.AddComponent<PlayerInteractionScanner>();
                scanner.ConfigurePlayer(player);
                scanner.Configure(player.transform, inputLock);
                scanner.ConfigureScan(5f, 180f, ~0);

                scanner.ScanNow();

                Assert.That(scanner.CurrentTarget, Is.SameAs(nearTarget));
                Assert.That(scanner.CurrentPrompt, Is.EqualTo("Use NearTarget"));
                Assert.That(scanner.TryInteract(), Is.True);
                Assert.That(nearTarget.InteractionCount, Is.EqualTo(1));
                Assert.That(farTarget.InteractionCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(farTarget.gameObject);
                Object.DestroyImmediate(nearTarget.gameObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LockedScannerClearsItsTargetAndRejectsInteraction()
        {
            var player = new GameObject("LockedInteractionPlayerTest");
            player.transform.position = TestOrigin;
            var target = CreateTarget("LockableTarget", TestOrigin + new Vector3(0f, 0f, 1f), true);
            player.SetActive(false);
            try
            {
                var inputLock = player.AddComponent<ThirdPersonInputLock>();
                var scanner = player.AddComponent<PlayerInteractionScanner>();
                scanner.Configure(player.transform, inputLock);
                scanner.ConfigureScan(5f, 180f, ~0);
                scanner.ScanNow();
                Assert.That(scanner.CurrentTarget, Is.SameAs(target));

                inputLock.SetInputLocked(true);
                scanner.ScanNow();

                Assert.That(scanner.CurrentTarget, Is.Null);
                Assert.That(scanner.TryInteract(), Is.False);
                Assert.That(target.InteractionCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(target.gameObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ScanIgnoresTargetsOutsideTheViewAngleOrWithoutAuthorization()
        {
            var player = new GameObject("FilteredInteractionPlayerTest");
            player.transform.position = TestOrigin;
            var behindTarget = CreateTarget("BehindTarget", TestOrigin + new Vector3(0f, 0f, -1f), true);
            var deniedTarget = CreateTarget("DeniedTarget", TestOrigin + new Vector3(0f, 0f, 1f), false);
            player.SetActive(false);
            try
            {
                var scanner = player.AddComponent<PlayerInteractionScanner>();
                scanner.Configure(player.transform, null);
                scanner.ConfigureScan(5f, 60f, ~0);

                scanner.ScanNow();

                Assert.That(scanner.CurrentTarget, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(deniedTarget.gameObject);
                Object.DestroyImmediate(behindTarget.gameObject);
                Object.DestroyImmediate(player);
            }
        }

        private static TestInteractable CreateTarget(string name, Vector3 position, bool allowed)
        {
            var target = new GameObject(name);
            target.transform.position = position;
            target.AddComponent<BoxCollider>();
            var interactable = target.AddComponent<TestInteractable>();
            interactable.Allowed = allowed;
            return interactable;
        }
    }

    internal sealed class TestInteractable : MonoBehaviour, IPlayerInteractable
    {
        public bool Allowed { get; set; }
        public int InteractionCount { get; private set; }
        public string InteractionPrompt => $"Use {name}";

        public bool CanInteract(InteractionContext context)
        {
            return Allowed && context.Player != null;
        }

        public void Interact(InteractionContext context)
        {
            InteractionCount++;
        }
    }
}
