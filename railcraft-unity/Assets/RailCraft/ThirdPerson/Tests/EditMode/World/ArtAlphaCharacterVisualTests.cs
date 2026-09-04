using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using RailCraft.ThirdPerson.Player;
using UnityEditor.Animations;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ArtAlphaCharacterVisualTests
    {
        [Test]
        public void CuratedWorkerCreatesHumanoidVisualWithLocomotionController()
        {
            var parent = new GameObject("ArtAlphaWorkerTestRoot");
            try
            {
                Assert.That(ArtAlphaCharacterVisualFactory.IsCharacterAvailable, Is.True);
                Assert.That(
                    ArtAlphaCharacterVisualFactory.TryCreate(
                        parent.transform,
                        out var visual,
                        out var animator),
                    Is.True);

                Assert.That(visual.name, Is.EqualTo(ArtAlphaCharacterVisualFactory.VisualRootName));
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                Assert.That(visual.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
                Assert.That(visual.GetComponentsInChildren<LODGroup>(true), Is.Not.Empty);
                Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Light>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<AudioSource>(true), Is.Empty);

                var bounds = BoundsOf(visual);
                Assert.That(bounds.size.y, Is.EqualTo(1.76f).Within(0.03f));
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.03f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ProjectOwnedControllerHasThreeClipBlendAndRuntimeParameters()
        {
            var controller = ArtAlphaCharacterVisualFactory.EnsureLocomotionController();
            Assert.That(controller, Is.Not.Null);
            Assert.That(
                controller.parameters.Any(parameter =>
                    parameter.name == ThirdPersonLocomotionAnimator.SpeedParameterName &&
                    parameter.type == AnimatorControllerParameterType.Float),
                Is.True);
            Assert.That(
                controller.parameters.Any(parameter =>
                    parameter.name == ThirdPersonLocomotionAnimator.GroundedParameterName &&
                    parameter.type == AnimatorControllerParameterType.Bool),
                Is.True);

            var locomotionState = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .Single(state => state.name == "Locomotion");
            Assert.That(locomotionState.motion, Is.TypeOf<BlendTree>());
            var tree = (BlendTree)locomotionState.motion;
            Assert.That(tree.blendParameter, Is.EqualTo(ThirdPersonLocomotionAnimator.SpeedParameterName));
            Assert.That(tree.children, Has.Length.EqualTo(3));
            Assert.That(tree.children.Select(child => child.threshold),
                Is.EqualTo(new[] { 0f, 0.58f, 1f }).Within(0.001f));
            Assert.That(tree.children.All(child => child.motion != null), Is.True);
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}
