using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ReusableVisualGuidanceTests
    {
        private readonly List<Material> materials = new List<Material>();
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ReusableVisualGuidanceTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);

            for (var index = 0; index < materials.Count; index++)
            {
                if (materials[index] != null)
                    UnityEngine.Object.DestroyImmediate(materials[index]);
            }
            materials.Clear();
        }

        [Test]
        public void HighlightPulseFollowsScannerTargetAndRestoresBaseColor()
        {
            var scanner = root.AddComponent<PlayerInteractionScanner>();
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "InteractableTarget";
            target.transform.SetParent(root.transform, false);
            target.transform.position = Vector3.forward;
            var interactable = target.AddComponent<GuidanceTestInteractable>();
            var renderer = target.GetComponent<Renderer>();
            var baseColor = new Color(0.16f, 0.28f, 0.42f, 1f);
            var colorProperty = AssignColorMaterial(renderer, baseColor);

            scanner.Configure(root.transform, null);
            scanner.ConfigurePlayer(root);
            scanner.ConfigureScan(3f, 180f, ~0);

            var feedback = target.AddComponent<InteractableVisualFeedback>();
            feedback.Configure(scanner, interactable, new[] { renderer });
            feedback.ConfigureTimings(1f, 0.25f, 0.75f, 0.2f, 0.3f);
            feedback.Advance(0.125f);

            Assert.That(scanner.CurrentTarget, Is.SameAs(interactable));
            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Highlighted));
            Assert.That(
                ColorDistance(ReadDisplayedColor(renderer, colorProperty), baseColor),
                Is.GreaterThan(0.05f));

            target.transform.position = Vector3.forward * 10f;
            scanner.ScanNow();

            Assert.That(scanner.CurrentTarget, Is.Null);
            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Idle));
            AssertColorApproximately(baseColor, ReadDisplayedColor(renderer, colorProperty));
        }

        [Test]
        public void OutcomeFeedbackOverridesPulseThenRestoresOriginalPropertyBlock()
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.SetParent(root.transform, false);
            var renderer = target.GetComponent<Renderer>();
            var materialColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            var colorProperty = AssignColorMaterial(renderer, materialColor);
            var originalOverride = new Color(0.24f, 0.34f, 0.48f, 1f);
            var originalBlock = new MaterialPropertyBlock();
            originalBlock.SetColor(colorProperty, originalOverride);
            renderer.SetPropertyBlock(originalBlock, 0);

            var successColor = new Color(0.05f, 0.95f, 0.24f, 1f);
            var failureColor = new Color(0.95f, 0.08f, 0.04f, 1f);
            var feedback = target.AddComponent<InteractableVisualFeedback>();
            feedback.Configure(null, null, new[] { renderer });
            feedback.ConfigurePalette(Color.yellow, successColor, failureColor);
            feedback.ConfigureTimings(1f, 0.25f, 0.75f, 0.2f, 0.3f);

            feedback.SetHighlighted(true);
            feedback.ShowFailure();

            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Failure));
            AssertColorApproximately(failureColor, ReadDisplayedColor(renderer, colorProperty));

            feedback.Advance(0.55f);

            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Highlighted));
            Assert.That(
                ColorDistance(ReadDisplayedColor(renderer, colorProperty), originalOverride),
                Is.GreaterThan(0.05f));

            feedback.SetHighlighted(false);
            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Idle));
            AssertColorApproximately(
                originalOverride,
                ReadDisplayedColor(renderer, colorProperty));

            var restoredBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(restoredBlock, 0);
            Assert.That(restoredBlock.HasColor(colorProperty), Is.True);
            AssertColorApproximately(originalOverride, restoredBlock.GetColor(colorProperty));

            feedback.ShowSuccess();
            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Success));
            AssertColorApproximately(successColor, ReadDisplayedColor(renderer, colorProperty));
            feedback.Advance(0.55f);
            Assert.That(feedback.State, Is.EqualTo(InteractionVisualState.Idle));
            AssertColorApproximately(originalOverride, ReadDisplayedColor(renderer, colorProperty));
        }

        [Test]
        public void CompletedModuleEventFocusesCameraAndRestoresOrbitView()
        {
            var session = new GuidanceTestSession();
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(session, "Test objective");
            var player = Child("Player");
            player.transform.position = new Vector3(1f, 0f, -2f);
            var cameraObject = Child("Camera");
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            var orbit = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbit.Configure(camera, player.transform, null);
            orbit.ConfigureCollision(0, 0.1f, 0f, 0.1f);
            orbit.SetPivotOffset(new Vector3(0f, 1.2f, 0f));
            orbit.SetView(32f, 24f, 4.25f);

            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var originalYaw = orbit.Yaw;
            var originalPitch = orbit.Pitch;
            var originalDistance = orbit.Distance;

            var focusTarget = Child("FrameModule");
            focusTarget.transform.position = new Vector3(8f, 1f, 5f);
            var focusPose = Child("FrameFocusPose");
            focusPose.transform.position = new Vector3(5f, 4f, 1f);
            focusPose.transform.rotation = Quaternion.LookRotation(
                focusTarget.transform.position - focusPose.transform.position,
                Vector3.up);

            var director = cameraObject.AddComponent<AssemblyCameraFocusDirector>();
            director.Configure(
                host,
                camera,
                orbit,
                new[]
                {
                    new AssemblyFocusBinding(
                        ModuleId.Frame,
                        focusTarget.transform,
                        focusPose.transform)
                });
            director.ConfigureTimings(0.2f, 0.1f, 0.2f);

            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Idle));
            host.InstallPart(ModuleId.Frame, PartId.TractionRod);

            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Focusing));
            Assert.That(director.CurrentTarget, Is.SameAs(focusTarget.transform));

            director.Advance(0.2f);
            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Holding));
            AssertVectorApproximately(focusPose.transform.position, camera.transform.position);
            Assert.That(
                Quaternion.Angle(focusPose.transform.rotation, camera.transform.rotation),
                Is.LessThan(0.01f));

            director.Advance(0.1f);
            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Returning));
            director.Advance(0.2f);

            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Idle));
            Assert.That(director.CurrentTarget, Is.Null);
            Assert.That(orbit.Yaw, Is.EqualTo(originalYaw).Within(0.001f));
            Assert.That(orbit.Pitch, Is.EqualTo(originalPitch).Within(0.001f));
            Assert.That(orbit.Distance, Is.EqualTo(originalDistance).Within(0.001f));
            AssertVectorApproximately(originalPosition, camera.transform.position);
            Assert.That(
                Quaternion.Angle(originalRotation, camera.transform.rotation),
                Is.LessThan(0.01f));
        }

        [Test]
        public void SessionResetCancelsActiveFocusAndRestoresViewImmediately()
        {
            var session = new GuidanceTestSession();
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(session, "Test objective");
            var player = Child("Player");
            var cameraObject = Child("Camera");
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            var orbit = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbit.Configure(camera, player.transform, null);
            orbit.ConfigureCollision(0, 0.1f, 0f, 0.1f);
            orbit.SetView(18f, 20f, 4f);
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var target = Child("LandingModule");
            target.transform.position = new Vector3(12f, 2f, 4f);

            var director = cameraObject.AddComponent<AssemblyCameraFocusDirector>();
            director.Configure(
                host,
                camera,
                orbit,
                new[]
                {
                    new AssemblyFocusBinding(ModuleId.Landing, target.transform)
                });
            director.ConfigureTimings(1f, 1f, 1f);

            host.InstallPart(ModuleId.Landing, PartId.Carbody);
            director.Advance(0.5f);
            Assert.That(director.IsActive, Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, originalPosition),
                Is.GreaterThan(0.1f));

            host.ResetSession();

            Assert.That(director.State, Is.EqualTo(AssemblyCameraFocusState.Idle));
            AssertVectorApproximately(originalPosition, camera.transform.position);
            Assert.That(
                Quaternion.Angle(originalRotation, camera.transform.rotation),
                Is.LessThan(0.01f));
        }

        private int AssignColorMaterial(Renderer renderer, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null, "A color-capable shader is required for this test.");
            var material = new Material(shader);
            materials.Add(material);
            var colorProperty = material.HasProperty("_BaseColor")
                ? Shader.PropertyToID("_BaseColor")
                : Shader.PropertyToID("_Color");
            material.SetColor(colorProperty, color);
            renderer.sharedMaterial = material;
            return colorProperty;
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private static Color ReadDisplayedColor(Renderer renderer, int colorProperty)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            return block.HasColor(colorProperty)
                ? block.GetColor(colorProperty)
                : renderer.sharedMaterial.GetColor(colorProperty);
        }

        private static float ColorDistance(Color left, Color right)
        {
            var difference = new Vector4(
                left.r - right.r,
                left.g - right.g,
                left.b - right.b,
                left.a - right.a);
            return difference.magnitude;
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            Assert.That(ColorDistance(expected, actual), Is.LessThan(0.001f));
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.001f));
        }

        private sealed class GuidanceTestSession : IWorldGameSession
        {
            private readonly HashSet<ModuleId> completedModules = new HashSet<ModuleId>();

            public IReadOnlyList<PartId> InventoryParts => Array.Empty<PartId>();
            public bool AreAllModulesComplete => false;
            public bool IsLandingComplete => completedModules.Contains(ModuleId.Landing);
            public bool IsVehicleComplete => false;
            public CommissioningPhase CommissioningPhase => CommissioningPhase.Locked;
            public AssemblyFlowStatus FlowStatus => AssemblyFlowStatus.Pending;
            public SessionProgressSummary Progress => null;
            public bool IsTimingPaused => false;

            public WorldAnswerResult SubmitAnswer(string questionId, int selectedOptionIndex)
            {
                return new WorldAnswerResult(false, false, 0, null, string.Empty);
            }

            public WorldCollectionResult CollectPart(PartId partId)
            {
                return new WorldCollectionResult(false, false, partId, string.Empty);
            }

            public WorldPartInstallResult InstallPart(ModuleId moduleId, PartId partId)
            {
                var changed = completedModules.Add(moduleId);
                return new WorldPartInstallResult(
                    true,
                    changed,
                    moduleId,
                    partId,
                    true,
                    string.Empty);
            }

            public WorldModuleInstallResult InstallModule(
                ModuleId targetModuleId,
                ModuleId childModuleId)
            {
                var changed = completedModules.Add(targetModuleId);
                return new WorldModuleInstallResult(
                    true,
                    changed,
                    targetModuleId,
                    childModuleId,
                    true,
                    false,
                    string.Empty);
            }

            public WorldCommissioningResult RunCommissioning()
            {
                return new WorldCommissioningResult(
                    false,
                    false,
                    false,
                    CommissioningPhase.Locked,
                    string.Empty);
            }

            public WorldCommissioningResult PerformRetuning()
            {
                return RunCommissioning();
            }

            public WorldCommissioningResult PerformInspection()
            {
                return RunCommissioning();
            }

            public bool InventoryContains(PartId partId)
            {
                return false;
            }

            public bool IsPartInstalled(ModuleId moduleId, PartId partId)
            {
                return false;
            }

            public bool IsModuleComplete(ModuleId moduleId)
            {
                return completedModules.Contains(moduleId);
            }

            public bool IsModuleInstalled(ModuleId targetModuleId, ModuleId childModuleId)
            {
                return false;
            }

            public WhiteboxGameSessionSnapshot ExportSnapshot()
            {
                return new WhiteboxGameSessionSnapshot();
            }

            public void RestoreSnapshot(WhiteboxGameSessionSnapshot snapshot)
            {
                completedModules.Clear();
            }

            public void PauseTiming()
            {
            }

            public void ResumeTiming()
            {
            }

            public void Reset()
            {
                completedModules.Clear();
            }
        }
    }

    public sealed class GuidanceTestInteractable : MonoBehaviour, IPlayerInteractable
    {
        public string InteractionPrompt => "Interact";

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
        }
    }
}
