using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class AssemblyVariantCatalogTests
    {
        [Test]
        public void CatalogContainsFourPlayablePlansWithStableKeys()
        {
            var definitions = AssemblyVariantCatalog.Definitions;
            Assert.That(definitions.Count, Is.EqualTo(4));

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < definitions.Count; index++)
            {
                Assert.That(definitions[index].Key, Is.Not.Null.And.Not.Empty);
                Assert.That(keys.Add(definitions[index].Key), Is.True);
                Assert.That(definitions[index].DisplayName, Is.Not.Null.And.Not.Empty);
                Assert.That(definitions[index].MenuLabel, Does.Contain(definitions[index].DisplayName));
            }
        }

        [Test]
        public void UnknownKeyFallsBackToFuxingPlan()
        {
            Assert.That(
                AssemblyVariantCatalog.TryParse("unknown-plan", out var parsed),
                Is.False);
            Assert.That(parsed, Is.EqualTo(AssemblyVariantId.FuxingDemo));
            Assert.That(
                AssemblyVariantCatalog.Clamp((AssemblyVariantId)999),
                Is.EqualTo(AssemblyVariantId.FuxingDemo));
        }

        [Test]
        public void TeachingPlanIsExplicitlyMarkedAsTeachingOnly()
        {
            var definition = AssemblyVariantCatalog.Get(AssemblyVariantId.TeachingConcept);
            Assert.That(definition.TeachingOnly, Is.True);
            Assert.That(definition.Description, Does.Contain("现实无对应"));
        }
    }
}
