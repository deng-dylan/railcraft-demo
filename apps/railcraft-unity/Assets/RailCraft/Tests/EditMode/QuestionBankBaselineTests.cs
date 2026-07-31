using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RailCraft.Tests.EditMode
{
    public sealed class QuestionBankBaselineTests
    {
        [Test]
        public void FrozenQuestionBankContainsFortyChoiceAndEightTrueFalse()
        {
            var path = Path.Combine(
                Application.dataPath,
                "RailCraft/Content/V1/questions.v1.json");
            var json = File.ReadAllText(path);
            var bank = JsonUtility.FromJson<QuestionBankFile>(json);

            Assert.That(bank.schemaVersion, Is.EqualTo(1));
            Assert.That(bank.contentVersion, Is.EqualTo("questions-2026-07-v1"));
            Assert.That(bank.questions, Has.Length.EqualTo(48));
            Assert.That(bank.questions, Has.Exactly(40)
                .Matches<QuestionRecord>(q => q.type == "single_choice"));
            Assert.That(bank.questions, Has.Exactly(8)
                .Matches<QuestionRecord>(q => q.type == "true_false"));
            Assert.That(bank.questions.Select(q => q.id), Is.EqualTo(
                Enumerable.Range(1, 48).Select(index => $"q{index:000}")));
            Assert.That(bank.questions.All(q => !string.IsNullOrWhiteSpace(q.prompt)), Is.True);
            Assert.That(bank.questions.All(q => q.options != null && q.options.Length >= 2), Is.True);
            Assert.That(bank.questions.All(q =>
                q.correctOptionIndex >= 0 && q.correctOptionIndex < q.options.Length), Is.True);
        }

        [System.Serializable]
        private sealed class QuestionBankFile
        {
            public int schemaVersion;
            public string contentVersion;
            public QuestionRecord[] questions;
        }

        [System.Serializable]
        private sealed class QuestionRecord
        {
            public string id;
            public string type;
            public string prompt;
            public string[] options;
            public int correctOptionIndex;
        }
    }
}
