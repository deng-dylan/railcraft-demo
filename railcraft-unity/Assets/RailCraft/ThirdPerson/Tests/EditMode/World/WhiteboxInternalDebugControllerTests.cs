using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WhiteboxInternalDebugControllerTests
    {
        private GameObject root;
        private WhiteboxGameSessionHost host;
        private WhiteboxInternalDebugController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("InternalDebugTests", typeof(Canvas));
            host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(new DomainWorldGameSession());
            controller = root.AddComponent<WhiteboxInternalDebugController>();
            controller.Configure(host, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void LaunchAuthorizationRequiresExactInternalArgument()
        {
            Assert.That(WhiteboxInternalDebugController.IsLaunchAuthorized(null), Is.False);
            Assert.That(WhiteboxInternalDebugController.IsLaunchAuthorized(new[] { "game.exe", "-debug" }), Is.False);
            Assert.That(WhiteboxInternalDebugController.IsLaunchAuthorized(new[]
                { "game.exe", WhiteboxInternalDebugController.LaunchArgument }), Is.True);
        }

        [Test]
        public void LockedControllerRejectsDebugActions()
        {
            Assert.Throws<System.InvalidOperationException>(() => controller.PrepareAllMaterials());
        }

        [Test]
        public void AuthorizedDebugActionsCanCompleteStandardTraining()
        {
            controller.ArmForTests();
            controller.UnlockForTests();
            Assert.That(controller.IsUnlocked, Is.True);
            Assert.That(controller.IsPanelVisible, Is.True);

            controller.CompleteTraining();

            Assert.That(host.Session.InventoryParts, Is.Empty);
            Assert.That(host.Session.IsLandingComplete, Is.True);
            Assert.That(host.Session.IsVehicleComplete, Is.True);
        }
    }
}
