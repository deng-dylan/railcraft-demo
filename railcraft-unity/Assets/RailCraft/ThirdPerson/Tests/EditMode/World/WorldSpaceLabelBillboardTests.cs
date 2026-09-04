using NUnit.Framework;
using RailCraft.ThirdPerson.UI;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WorldSpaceLabelBillboardTests
    {
        [Test]
        public void UprightRotationFacesTheCameraWithoutPitch()
        {
            var label = new Vector3(2f, 4f, 8f);
            var camera = new Vector3(-4f, 1f, -6f);
            var rotation = WorldSpaceLabelBillboard.CalculateFacingRotation(
                label,
                camera,
                true);
            var expected = Vector3.ProjectOnPlane(label - camera, Vector3.up).normalized;

            Assert.That(Vector3.Dot(rotation * Vector3.forward, expected),
                Is.GreaterThan(0.9999f));
            Assert.That(Mathf.Abs((rotation * Vector3.forward).y), Is.LessThan(0.0001f));
        }
    }
}
