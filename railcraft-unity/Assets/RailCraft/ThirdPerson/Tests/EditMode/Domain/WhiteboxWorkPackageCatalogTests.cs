using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class WhiteboxWorkPackageCatalogTests
    {
        [Test]
        public void DefinesFiveMaterialPackagesAndAssignsEveryDomainPartOnce()
        {
            var packages = WhiteboxWorkPackageCatalog.Definitions;
            var assignedParts = new HashSet<PartId>();
            var materialPackageCount = 0;

            Assert.That(packages.Count, Is.EqualTo(6));

            foreach (var package in packages)
            {
                if (package.IsMaterialPackage)
                    materialPackageCount++;
                foreach (var partId in package.RequiredParts)
                    Assert.That(assignedParts.Add(partId), Is.True, $"Duplicate package part: {partId}");
            }

            Assert.That(materialPackageCount, Is.EqualTo(5));
            Assert.That(assignedParts, Is.EquivalentTo((PartId[])Enum.GetValues(typeof(PartId))));

            var coreQuestionCount = 0;
            foreach (var package in packages)
            {
                if (!package.IsMaterialPackage)
                    continue;
                Assert.That(package.RequiredQuestionCount, Is.EqualTo(2), package.Key);
                coreQuestionCount += package.RequiredQuestionCount;
                Assert.That(package.AssemblyModule.HasValue, Is.True, package.Key);
            }
            Assert.That(coreQuestionCount, Is.EqualTo(10));
        }

        [Test]
        public void PackageLookupIsIndependentFromQuestionCount()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();

            Assert.That(catalog.Questions.Count, Is.GreaterThan(WhiteboxWorkPackageCatalog.Definitions.Count));
            foreach (var partId in (PartId[])Enum.GetValues(typeof(PartId)))
            {
                Assert.That(WhiteboxWorkPackageCatalog.TryGetForPart(partId, out var package), Is.True);
                Assert.That(package.RequiredParts, Does.Contain(partId));
            }

            foreach (var package in WhiteboxWorkPackageCatalog.Definitions)
            {
                foreach (var questionId in package.CoreQuestionIds)
                {
                    Assert.That(catalog.TryGetQuestion(questionId, out var question), Is.True, questionId);
                    Assert.That(question.IsCore, Is.True, questionId);
                    Assert.That(question.RelatedWorkPackage, Is.EqualTo(package.Id), questionId);
                    Assert.That(package.RequiredParts, Does.Contain(question.RewardPart), questionId);
                }
            }
        }
    }
}
