using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FinalShowcaseLayoutTests
    {
        [Test]
        public void FullScalePlacementMatchesInspectedSourceLength()
        {
            var scale = FinalShowcaseSceneBuilder.CalculateUniformScale(
                2.301f,
                FinalShowcaseSceneBuilder.TargetTrainLengthMetres);

            Assert.That(scale, Is.EqualTo(86.91873f).Within(0.0001f));
        }

        [TestCase(0f, 200f)]
        [TestCase(-1f, 200f)]
        [TestCase(2.3f, 0f)]
        public void ScaleCalculationRejectsInvalidDimensions(float sourceLength, float targetLength)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FinalShowcaseSceneBuilder.CalculateUniformScale(sourceLength, targetLength));
        }

        [Test]
        public void SourceXAxisIsRotatedIntoUnityTrackDirection()
        {
            Assert.That(
                FinalShowcaseSceneBuilder.ShouldRotateLengthFromX(new Vector3(2.301f, 0.04f, 0.0355f)),
                Is.True);
            Assert.That(
                FinalShowcaseSceneBuilder.ShouldRotateLengthFromX(new Vector3(3.36f, 4.05f, 200f)),
                Is.False);
        }

        [Test]
        public void RepeatedTrackElementsStayCenteredAndWithinExtent()
        {
            const float halfExtent = 9f;
            var values = FinalShowcaseSceneBuilder.CalculateSymmetricPositions(halfExtent, 2f).ToArray();

            Assert.That(values, Is.EqualTo(new[] { -8f, -6f, -4f, -2f, 0f, 2f, 4f, 6f, 8f }));
            Assert.That(values.First(), Is.EqualTo(-values.Last()));
            Assert.That(values.All(value => Mathf.Abs(value) <= halfExtent), Is.True);
        }

        [Test]
        public void InspectedInterfacesProduceEightLogicalCarSegments()
        {
            var boundaries = FinalShowcaseSceneBuilder.CalculateTargetCarBoundaryPositions().ToArray();

            Assert.That(
                boundaries,
                Has.Length.EqualTo(FinalShowcaseSceneBuilder.CarSegmentCount + 1));
            Assert.That(boundaries[0], Is.EqualTo(100f).Within(0.001f));
            Assert.That(boundaries[8], Is.EqualTo(-100f).Within(0.001f));
            Assert.That(boundaries.Zip(boundaries.Skip(1), (left, right) => left > right).All(value => value),
                Is.True);
        }

        [Test]
        public void InterfaceCentersStayWithTheSourceMinusXCar()
        {
            var boundaries = FinalShowcaseSceneBuilder.CalculateTargetCarBoundaryPositions();

            Assert.That(
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(boundaries[1], boundaries),
                Is.EqualTo(0));
            Assert.That(
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(boundaries[1] - 0.10f, boundaries),
                Is.EqualTo(1));
            Assert.That(
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(boundaries[7], boundaries),
                Is.EqualTo(6));
            Assert.That(
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(boundaries[8] - 5f, boundaries),
                Is.EqualTo(7));
        }

        [Test]
        public void InspectedRendererCentersReproduceAllNinetyNineUniqueAssignments()
        {
            var sourceCentersX = new[]
            {
                -0.8916447958405f,
                -0.610528295841f,
                -0.315419295841f,
                -0.020309795841f,
                0.274799704159f,
                0.569909204159f,
                0.865018704159f,
                1.1432254541595f
            };
            var inspectedCounts = new[] { 20, 10, 10, 10, 10, 10, 10, 19 };
            var rendererCenters = sourceCentersX
                .SelectMany((center, index) => Enumerable.Repeat(
                    FinalShowcaseSceneBuilder.ConvertInspectedSourceXToTargetZ(center),
                    inspectedCounts[index]))
                .ToArray();

            var result = FinalShowcaseSceneBuilder.CountCarSegmentAssignments(
                rendererCenters,
                FinalShowcaseSceneBuilder.CalculateTargetCarBoundaryPositions());

            Assert.That(
                rendererCenters,
                Has.Length.EqualTo(FinalShowcaseSceneBuilder.InspectedHighDetailRendererCount));
            Assert.That(result, Is.EqualTo(inspectedCounts));
            Assert.That(result.Sum(), Is.EqualTo(rendererCenters.Length));
        }

        [Test]
        public void PerCarLodContractHasThreeStrictlyDescendingLevels()
        {
            var thresholds = FinalShowcaseSceneBuilder
                .GetPerCarLodTransitionHeights()
                .ToArray();

            Assert.That(thresholds, Has.Length.EqualTo(3));
            Assert.That(thresholds.All(value => value > 0f && value < 1f), Is.True);
            Assert.That(
                thresholds.Zip(thresholds.Skip(1), (near, far) => near > far).All(value => value),
                Is.True);
            Assert.That(
                FinalShowcaseSceneBuilder.Lod1ProxyRenderersPerCar *
                FinalShowcaseSceneBuilder.CarSegmentCount,
                Is.EqualTo(40));
            Assert.That(
                FinalShowcaseSceneBuilder.Lod2ProxyRenderersPerCar *
                FinalShowcaseSceneBuilder.CarSegmentCount,
                Is.EqualTo(8));
        }

        [Test]
        public void SegmentAssignmentRejectsMalformedBoundaryContracts()
        {
            Assert.Throws<ArgumentException>(() =>
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(
                    0f,
                    new[] { 1f, 0f, -1f }));
            Assert.Throws<ArgumentException>(() =>
                FinalShowcaseSceneBuilder.ResolveCarSegmentIndex(
                    0f,
                    new[] { 4f, 3f, 2f, 1f, 0f, -1f, -2f, -2f, -4f }));
        }

        [Test]
        public void MissingModelGuidanceNamesTheStableAssetPathAndRebuildMenu()
        {
            var message = FinalShowcaseSceneBuilder.GetMissingModelMessage();

            Assert.That(message, Does.Contain(FinalShowcaseSceneBuilder.ModelAssetPath));
            Assert.That(message, Does.Contain("RailCraft > Final Showcase > Rebuild Scene"));
        }

        [Test]
        public void ShowcaseBuilderDoesNotClaimDefaultBuildSettingsEntry()
        {
            Assert.That(
                EditorBuildSettings.scenes.Select(scene => scene.path),
                Has.None.EqualTo(FinalShowcaseSceneBuilder.ScenePath));
        }
    }
}
