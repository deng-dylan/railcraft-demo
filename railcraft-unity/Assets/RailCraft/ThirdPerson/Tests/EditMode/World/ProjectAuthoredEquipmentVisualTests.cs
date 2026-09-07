using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ProjectAuthoredEquipmentVisualTests
    {
        private GameObject root;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ProjectAuthoredEquipmentTests");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            material = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (material != null) Object.DestroyImmediate(material);
        }

        [Test]
        public void SecondaryAirSpringIsMovableTeachingGeometry()
        {
            Assert.That(ProjectAuthoredEquipmentVisualFactory.TryCreatePartVisual(
                root.transform, "AirSpring", PartId.SecondaryElasticElement, material, out var visual), Is.True);
            Assert.That(visual.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(12));
            Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(visual.GetComponentsInChildren<Transform>(true)
                .All(item => GameObjectUtility.GetStaticEditorFlags(item.gameObject) == 0), Is.True);
            Assert.That(visual.transform.Find("AirSpring_L_BellowsWaist"), Is.Not.Null);
            Assert.That(visual.transform.Find("AirSpring_R_AuxiliarySpring"), Is.Not.Null);
        }

        [Test]
        public void EnvironmentEquipmentBuildsFourStaticVisualGroups()
        {
            Assert.That(ProjectAuthoredEquipmentVisualFactory.BuildDefaultVisuals(
                root.transform, material, material, material, material, material), Is.EqualTo(4));
            var expansion = root.transform.Find(ProjectAuthoredEquipmentVisualFactory.RootName);
            Assert.That(expansion, Is.Not.Null);
            foreach (var name in new[]
            {
                ProjectAuthoredEquipmentVisualFactory.LiftTableName,
                ProjectAuthoredEquipmentVisualFactory.SpreaderName,
                ProjectAuthoredEquipmentVisualFactory.HmiCabinetName,
                ProjectAuthoredEquipmentVisualFactory.CableRunName
            }) Assert.That(expansion.Find(name), Is.Not.Null, name);
            Assert.That(expansion.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(expansion.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(20));
            Assert.That(expansion.GetComponentsInChildren<Transform>(true)
                .All(item => GameObjectUtility.GetStaticEditorFlags(item.gameObject) != 0), Is.True);
        }

        [Test]
        public void FactoryRejectsUnrelatedPart()
        {
            Assert.That(ProjectAuthoredEquipmentVisualFactory.TryCreatePartVisual(
                root.transform, "Wheel", PartId.Wheel, material, out var visual), Is.False);
            Assert.That(visual, Is.Null);
        }
    }
}
