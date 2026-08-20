using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class AssemblyVariantGameplayTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("AssemblyVariantGameplayTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void SelectedPlanIsIncludedInHostSnapshotAndRestored()
        {
            var source = new GameObject("SourceHost");
            source.transform.SetParent(root.transform, false);
            var sourceHost = source.AddComponent<WhiteboxGameSessionHost>();
            sourceHost.Configure(new DomainWorldGameSession(), "测试目标");
            sourceHost.SelectAssemblyVariant(AssemblyVariantId.Y25Freight);
            var snapshot = sourceHost.ExportSnapshot();

            Assert.That(snapshot.AssemblyVariant, Is.EqualTo(AssemblyVariantId.Y25Freight));

            var target = new GameObject("TargetHost");
            target.transform.SetParent(root.transform, false);
            var targetHost = target.AddComponent<WhiteboxGameSessionHost>();
            targetHost.Configure(new DomainWorldGameSession(), "测试目标");
            targetHost.RestoreSession(snapshot);

            Assert.That(targetHost.SelectedAssemblyVariant, Is.EqualTo(AssemblyVariantId.Y25Freight));
        }

        [Test]
        public void VisualRouterActivatesOnlyTheSelectedPlanRoot()
        {
            var hostObject = new GameObject("Host");
            hostObject.transform.SetParent(root.transform, false);
            var host = hostObject.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(new DomainWorldGameSession(), "测试目标");

            var roots = new GameObject[AssemblyVariantCatalog.Definitions.Count];
            for (var index = 0; index < roots.Length; index++)
            {
                roots[index] = new GameObject($"Variant_{index}");
                roots[index].transform.SetParent(root.transform, false);
            }

            var router = root.AddComponent<AssemblyVariantVisualRouter>();
            router.Configure(host, roots);
            host.SelectAssemblyVariant(AssemblyVariantId.MetroSimplified);

            Assert.That(router.ActiveVariant, Is.EqualTo(AssemblyVariantId.MetroSimplified));
            Assert.That(router.ActiveVariantRoot, Is.SameAs(roots[(int)AssemblyVariantId.MetroSimplified]));
            for (var index = 0; index < roots.Length; index++)
                Assert.That(roots[index].activeSelf, Is.EqualTo(index == (int)AssemblyVariantId.MetroSimplified));
        }

        [Test]
        public void SmokeArgumentParsesEveryPlayablePlanKey()
        {
            foreach (var definition in AssemblyVariantCatalog.Definitions)
            {
                var arguments = new[]
                {
                    WhiteboxAutomatedSmokeRunner.SmokeArgument,
                    WhiteboxAutomatedSmokeRunner.VariantArgumentPrefix + definition.Key
                };

                Assert.That(
                    WhiteboxAutomatedSmokeRunner.TryGetRequestedVariant(arguments, out var parsed),
                    Is.True,
                    definition.Key);
                Assert.That(parsed, Is.EqualTo(definition.Id), definition.Key);
            }
        }
    }
}
