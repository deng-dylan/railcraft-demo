using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

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
            // EditMode does not dispatch runtime-only MonoBehaviour lifecycle calls.
            if (controller != null) InvokeLifecycle("OnDestroy");
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
            Assert.Throws<System.InvalidOperationException>(() => controller.ToggleCollisionBoxes());
        }

        [Test]
        public void CollisionBoxesStayHiddenUntilAuthorizedAndCanBeToggled()
        {
            Assert.That(controller.AreCollisionBoxesVisible, Is.False);
            controller.ArmForTests();
            controller.UnlockForTests();
            controller.ToggleCollisionBoxes();
            Assert.That(controller.AreCollisionBoxesVisible, Is.True);
            controller.ToggleCollisionBoxes();
            Assert.That(controller.AreCollisionBoxesVisible, Is.False);
        }

        [Test]
        public void CollisionBoxesBuildTwelveRuntimeEdgesPerActiveSolidCollider()
        {
            var solid = CreateProbe("Solid", new Vector3(10000f, 10000f, 10000f));
            solid.center = new Vector3(0.25f, 0.5f, -0.75f);
            solid.size = new Vector3(2f, 3f, 4f);
            solid.transform.rotation = Quaternion.Euler(12f, 35f, 8f);
            solid.transform.localScale = new Vector3(2f, 1f, 0.5f);
            var trigger = CreateProbe("Trigger", new Vector3(20000f, 10000f, 10000f));
            trigger.isTrigger = true;
            var disabled = CreateProbe("Disabled", new Vector3(30000f, 10000f, 10000f));
            disabled.enabled = false;
            var inactive = CreateProbe("Inactive", new Vector3(40000f, 10000f, 10000f));
            inactive.gameObject.SetActive(false);
            Physics.SyncTransforms();
            UnlockAndShowCollisionBoxes();

            var filter = FindCollisionLines();
            var mesh = filter.sharedMesh;
            Assert.That(filter.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(filter.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported, Is.True);
            Assert.That(filter.transform.parent, Is.Null);
            Assert.That(filter.GetComponents<Collider>(), Is.Empty);
            Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Lines));
            Assert.That(mesh.vertexCount, Is.EqualTo(controller.VisibleCollisionBoxCount * 8));
            Assert.That(mesh.GetIndexCount(0), Is.EqualTo(controller.VisibleCollisionBoxCount * 24));
            Assert.That(mesh.vertices, Does.Contain(solid.bounds.min));
            Assert.That(mesh.vertices, Does.Contain(solid.bounds.max));
            Assert.That(mesh.vertices, Has.No.Member(trigger.bounds.min));
            Assert.That(mesh.vertices.Any(vertex => vertex.x > 25000f), Is.False);

            var countWithSolid = controller.VisibleCollisionBoxCount;
            solid.enabled = false;
            controller.RefreshCollisionBoxes();
            Assert.That(controller.VisibleCollisionBoxCount, Is.EqualTo(countWithSolid - 1));
        }

        [Test]
        public void CollisionBoxesFollowMovedBodiesAndUseDynamicColor()
        {
            var solid = CreateProbe("MovingSolid", new Vector3(10000f, 10000f, 10000f));
            var body = solid.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            Physics.SyncTransforms();
            UnlockAndShowCollisionBoxes();
            var oldMinimum = solid.bounds.min;

            solid.transform.position += new Vector3(15f, 3f, 7f);
            Physics.SyncTransforms();
            controller.RefreshCollisionBoxes();

            var mesh = FindCollisionLines().sharedMesh;
            Assert.That(mesh.vertices, Has.No.Member(oldMinimum));
            var corner = System.Array.IndexOf(mesh.vertices, solid.bounds.min);
            Assert.That(corner, Is.GreaterThanOrEqualTo(0));
            Assert.That(mesh.colors[corner], Is.EqualTo(Color.yellow));
        }

        [Test]
        public void CollisionBoxesHideOnToggleAndDisableAndReleaseRuntimeResources()
        {
            CreateProbe("Solid", new Vector3(10000f, 10000f, 10000f));
            UnlockAndShowCollisionBoxes();
            var filter = FindCollisionLines();
            var lines = filter.gameObject;
            var mesh = filter.sharedMesh;
            var material = lines.GetComponent<MeshRenderer>().sharedMaterial;

            controller.ToggleCollisionBoxes();
            Assert.That(lines.activeSelf, Is.False);
            Assert.That(controller.VisibleCollisionBoxCount, Is.Zero);
            controller.ToggleCollisionBoxes();
            Assert.That(lines.activeSelf, Is.True);
            controller.enabled = false;
            InvokeLifecycle("OnDisable");
            Assert.That(controller.AreCollisionBoxesVisible, Is.False);
            Assert.That(lines.activeSelf, Is.False);
            controller.RefreshCollisionBoxes();
            Assert.That(lines.activeSelf, Is.False);

            InvokeLifecycle("OnDestroy");
            Object.DestroyImmediate(root);
            root = null;
            Assert.That(lines == null, Is.True);
            Assert.That(mesh == null, Is.True);
            Assert.That(material == null, Is.True);
        }

        [Test]
        public void CollisionBoxesAreVisibleInCameraRenderWithoutEditorGizmos()
        {
            var solid = CreateProbe("RenderProbe", new Vector3(10000f, 10000f, 10000f));
            solid.size = Vector3.one * 2f;
            Physics.SyncTransforms();
            UnlockAndShowCollisionBoxes();
            var lines = FindCollisionLines();
            lines.gameObject.layer = 31;
            var cameraObject = new GameObject("CollisionRenderCamera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = solid.transform.position + Vector3.back * 6f;
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
            var target = new RenderTexture(128, 128, 24);
            var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
                pixels.Apply();
                var cyanPixels = pixels.GetPixels().Count(color =>
                    color.g > 0.4f && color.b > 0.4f && color.r < 0.2f);
                Assert.That(cyanPixels, Is.GreaterThan(30),
                    "The standalone-compatible collision mesh did not render cyan edges.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        [Test]
        public void DebugPanelKeepsAllFiveActionsAndStatusInsideItsBounds()
        {
            controller.ArmForTests();
            controller.UnlockForTests();
            var panel = (RectTransform)root.transform.Find("InternalDebugPanel");
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            var corners = new Vector3[4];
            foreach (RectTransform child in panel)
            {
                child.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = panel.InverseTransformPoint(corner);
                    Assert.That(local.y, Is.InRange(panel.rect.yMin - 0.1f, panel.rect.yMax + 0.1f), child.name);
                }
            }
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

        private BoxCollider CreateProbe(string name, Vector3 position)
        {
            var probe = new GameObject(name, typeof(BoxCollider));
            probe.transform.SetParent(root.transform, false);
            probe.transform.position = position;
            return probe.GetComponent<BoxCollider>();
        }

        private void UnlockAndShowCollisionBoxes()
        {
            controller.ArmForTests();
            controller.UnlockForTests();
            controller.ToggleCollisionBoxes();
        }

        private static MeshFilter FindCollisionLines()
        {
            // Runtime overlays carry DontSave, which FindObjectsByType excludes.
            return Resources.FindObjectsOfTypeAll<MeshFilter>()
                .Single(filter => filter.gameObject.scene.IsValid() && filter.name == "InternalDebugCollisionBoxes");
        }

        private void InvokeLifecycle(string name)
        {
            // Unity 6 rejects SendMessage lifecycle dispatch outside Play Mode.
            typeof(WhiteboxInternalDebugController).GetMethod(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(controller, null);
        }
    }
}
