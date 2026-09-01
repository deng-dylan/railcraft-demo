using NUnit.Framework;
using RailCraft.ThirdPerson.FinalShowcase;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.FinalShowcase
{
    public sealed class FinalShowcasePresentationStateTests
    {
        [Test]
        public void ConfigureCountsSelectsFirstAvailableCameraAndCar()
        {
            var state = new FinalShowcasePresentationState();

            state.ConfigureCounts(4, 8);

            Assert.That(state.CameraPreset, Is.EqualTo(FinalShowcaseCameraPreset.Overview));
            Assert.That(state.CameraIndex, Is.Zero);
            Assert.That(state.SelectedCarIndex, Is.Zero);
            Assert.That(state.CarCount, Is.EqualTo(8));
        }

        [Test]
        public void CameraAndCarStepsWrapInBothDirections()
        {
            var state = new FinalShowcasePresentationState();
            state.ConfigureCounts(4, 8);

            state.StepCamera(-1);
            state.StepCar(-1);

            Assert.That(state.CameraPreset, Is.EqualTo(FinalShowcaseCameraPreset.Departure));
            Assert.That(state.SelectedCarIndex, Is.EqualTo(7));

            state.StepCamera(1);
            state.StepCar(1);

            Assert.That(state.CameraPreset, Is.EqualTo(FinalShowcaseCameraPreset.Overview));
            Assert.That(state.SelectedCarIndex, Is.Zero);
        }

        [Test]
        public void InvalidDirectSelectionsPreserveCurrentState()
        {
            var state = new FinalShowcasePresentationState();
            state.ConfigureCounts(4, 8);

            Assert.That(state.SelectCamera(4), Is.False);
            Assert.That(state.SelectCar(8), Is.False);
            Assert.That(state.CameraIndex, Is.Zero);
            Assert.That(state.SelectedCarIndex, Is.Zero);
        }

        [Test]
        public void ExplosionProgressAdvancesAndReversesWithoutOvershoot()
        {
            var state = new FinalShowcasePresentationState();
            state.ConfigureCounts(4, 8);
            state.SetExploded(true);

            state.AdvanceExplosion(0.5f, 2f);
            Assert.That(state.ExplodeProgress, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(state.IsExplosionAnimating, Is.True);

            state.AdvanceExplosion(10f, 2f);
            Assert.That(state.ExplodeProgress, Is.EqualTo(1f));
            Assert.That(state.IsExplosionAnimating, Is.False);

            state.SetExploded(false);
            state.AdvanceExplosion(0.5f, 2f);
            Assert.That(state.ExplodeProgress, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void EightCarExplodedOffsetsAreLongitudinallySymmetric()
        {
            var first = FinalShowcasePresentationState.CalculateExplodedOffset(
                0, 8, 3.2f, 0.55f, 0.18f);
            var last = FinalShowcasePresentationState.CalculateExplodedOffset(
                7, 8, 3.2f, 0.55f, 0.18f);
            var second = FinalShowcasePresentationState.CalculateExplodedOffset(
                1, 8, 3.2f, 0.55f, 0.18f);

            Assert.That(first.z, Is.EqualTo(-last.z).Within(0.0001f));
            Assert.That(first.z - second.z, Is.EqualTo(3.2f).Within(0.0001f));
            Assert.That(first.x, Is.EqualTo(-last.x).Within(0.0001f));
            Assert.That(first.y, Is.EqualTo(0.18f).Within(0.0001f));
        }

        [Test]
        public void ImmediateResetRestoresInitialPresentation()
        {
            var state = new FinalShowcasePresentationState();
            state.ConfigureCounts(4, 8);
            state.SelectCamera(FinalShowcaseCameraPreset.Side);
            state.SelectCar(5);
            state.SetExploded(true);
            state.AdvanceExplosion(5f, 1f);

            state.Reset(true);

            Assert.That(state.CameraPreset, Is.EqualTo(FinalShowcaseCameraPreset.Overview));
            Assert.That(state.SelectedCarIndex, Is.Zero);
            Assert.That(state.ExplodedTarget, Is.False);
            Assert.That(state.ExplodeProgress, Is.Zero);
        }

        [Test]
        public void HudSnapshotProvidesStableLabelsForPresenter()
        {
            var snapshot = new FinalShowcaseHudState(
                FinalShowcaseCameraPreset.Side,
                2,
                8,
                8,
                true,
                1f,
                true,
                "展示控制已就绪");

            Assert.That(snapshot.CameraLabel, Is.EqualTo("侧面机位"));
            Assert.That(snapshot.SelectedCarLabel, Is.EqualTo("第 03/08 节"));
            Assert.That(snapshot.StatusText, Does.Contain("分解视图"));
            Assert.That(FinalShowcaseHudState.ShortcutHelp, Does.Contain("F1-F4"));
        }

        [Test]
        public void TransitionBlendIsFrameRateIndependentAndBounded()
        {
            var blend = FinalShowcasePresentationState.CalculateExponentialBlend(5f, 0.02f);

            Assert.That(blend, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(
                FinalShowcasePresentationState.CalculateExponentialBlend(0f, 0.02f),
                Is.Zero);
            Assert.That(FinalShowcasePresentationState.SmoothProgress(-1f), Is.Zero);
            Assert.That(FinalShowcasePresentationState.SmoothProgress(2f), Is.EqualTo(1f));
        }

        [Test]
        public void SelectedCarFocusFollowsItsVisualRootThroughExplodedView()
        {
            var root = new GameObject("FinalShowcaseRuntimeTest");
            try
            {
                var trainDisplay = Child(root.transform, "TrainDisplay");
                var segments = Child(trainDisplay.transform, "CarSegments");
                Transform firstVisualRoot = null;
                for (var index = 0; index < FinalShowcaseRuntimeController.ExpectedCarCount; index++)
                {
                    var segment = Child(
                        segments.transform,
                        $"CarSegment_{index + 1:00}_Test");
                    segment.transform.localPosition = new Vector3(0f, 0f, 80f - index * 22f);
                    var visualRoot = Child(segment.transform, "VisualRoot_LOD0_HighDetail");
                    visualRoot.AddComponent<MeshRenderer>();
                    if (index == 0)
                        firstVisualRoot = visualRoot.transform;
                }

                var composition = Child(root.transform, "CameraComposition");
                var camera = Child(composition.transform, "HeroCamera")
                    .AddComponent<Camera>();
                var controller = root.AddComponent<FinalShowcaseRuntimeController>();
                controller.Configure(camera, trainDisplay.transform, composition.transform);

                Assert.That(controller.SelectCarNumber(1), Is.True);
                Assert.That(controller.SetExploded(true, true), Is.True);
                Assert.That(controller.SelectedCarTransform, Is.SameAs(firstVisualRoot));
                Assert.That(firstVisualRoot.localPosition.sqrMagnitude, Is.GreaterThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
