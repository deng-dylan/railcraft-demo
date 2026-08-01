using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RailCraft.Assets;
using RailCraft.Editor;
using RailCraft.Tests.EditMode.Fixtures;
using UnityEditor;
using UnityEngine;

namespace RailCraft.Tests.EditMode
{
    public sealed class ModelContractValidatorTests
    {
        private static readonly string[] ExpectedAssetKeys =
        {
            "module.frame",
            "module.wheelset_axlebox_a",
            "module.wheelset_axlebox_b",
            "module.primary_suspension",
            "module.brake",
            "module.traction_drive_a",
            "module.traction_drive_b",
            "module.central_traction",
            "module.secondary_suspension",
            "module.height_damping",
            "module.sensor",
            "vehicle.powered_intermediate_car",
            "process.commissioning_card",
            "process.inspection_card",
            "process.release_card"
        };

        [Test]
        public void CatalogResolvesEveryProductionAssetKey()
        {
            var bundle = ContentFixture.LoadProduction();
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(
                "Assets/RailCraft/Art/PartPrefabCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(bundle.Flow.steps.Select(step => step.assetKey),
                Is.EquivalentTo(ExpectedAssetKeys));
            foreach (var step in bundle.Flow.steps)
                Assert.That(catalog.Resolve(step.assetKey), Is.Not.Null, step.assetKey);
            Assert.That(catalog.Resolve("missing.asset"), Is.Null);
        }

        [Test]
        public void EveryDraggablePrefabHasRequiredContract()
        {
            var issues = ModelContractValidator.ValidateProductionCatalog();
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void CatalogContainsOneContractForEachExactKey()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(
                "Assets/RailCraft/Art/PartPrefabCatalog.asset");
            var found = new List<string>();

            foreach (var key in ExpectedAssetKeys)
            {
                var prefab = catalog.Resolve(key);
                var contract = prefab.GetComponent<ModelContract>();
                Assert.That(contract, Is.Not.Null, key);
                Assert.That(contract.AssetKey, Is.EqualTo(key));
                found.Add(contract.AssetKey);
            }

            Assert.That(found, Is.Unique);
            Assert.That(found, Is.EquivalentTo(ExpectedAssetKeys));
        }

        [Test]
        public void HeadDisplayIsStaticPresentationOnly()
        {
            var head = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/Vehicles/CR400AFHeadDisplay.prefab");

            Assert.That(head, Is.Not.Null);
            Assert.That(head.transform.Find("CR400AF 展示背景"), Is.Not.Null);
            Assert.That(head.GetComponentInChildren<Collider>(true), Is.Null);
            Assert.That(head.GetComponentInChildren<RailCraft.Interaction.DraggableModule>(true), Is.Null);
            Assert.That(head.GetComponentInChildren<ModelContract>(true), Is.Null);
            Assert.That(head.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        }
    }
}
