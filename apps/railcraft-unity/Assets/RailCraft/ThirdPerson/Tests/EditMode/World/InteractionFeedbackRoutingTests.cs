using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class InteractionFeedbackRoutingTests
    {
        // Some repository-wide EditMode tests leave the legacy Factory scene
        // active. Keep physics fixtures away from that scene so scanner tests
        // observe only the colliders they create themselves.
        private static readonly Vector3 TestOrigin = new Vector3(10000f, 10000f, 10000f);
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("InteractionFeedbackRoutingTests");
            root.transform.position = TestOrigin;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void HostRaisesAnswerEvaluatedForEverySubmission()
        {
            var worldSession = new DomainWorldGameSession();
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(worldSession, "测试答题事件");
            var question = worldSession.DomainSession.Catalog.Questions[0];
            var eventCount = 0;
            var observed = default(WhiteboxAnswerEvaluatedEvent);
            host.AnswerEvaluated += answerEvent =>
            {
                eventCount++;
                observed = answerEvent;
            };

            var result = host.SubmitAnswer(question.Id, question.CorrectOptionIndex);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(observed.QuestionId, Is.EqualTo(question.Id));
            Assert.That(observed.Result.IsCorrect, Is.EqualTo(result.IsCorrect));
            Assert.That(observed.Result.RewardPart, Is.EqualTo(result.RewardPart));

            host.SubmitAnswer("missing-question", -1);
            Assert.That(eventCount, Is.EqualTo(2));
            Assert.That(observed.QuestionId, Is.EqualTo("missing-question"));
            Assert.That(observed.Result.IsCorrect, Is.False);
        }

        [TestCase("回答正确，车轴已解锁", InteractionFeedbackOutcome.Success)]
        [TestCase("落车完成，车辆进入调试阶段", InteractionFeedbackOutcome.Success)]
        [TestCase("调试通过，车辆投入使用", InteractionFeedbackOutcome.Success)]
        [TestCase("首次调试未通过，进入重新调试流程", InteractionFeedbackOutcome.Failure)]
        [TestCase("库存中没有车轮", InteractionFeedbackOutcome.Failure)]
        [TestCase("请前往下一工位", InteractionFeedbackOutcome.None)]
        [TestCase("", InteractionFeedbackOutcome.None)]
        public void FeedbackMessageClassificationHandlesChineseOutcomeLanguage(
            string message,
            InteractionFeedbackOutcome expected)
        {
            Assert.That(
                WhiteboxInteractionFeedbackRouter.ClassifyFeedback(message),
                Is.EqualTo(expected));
        }

        [Test]
        public void FeedbackRequestedColorsCurrentInteractable()
        {
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            var scanner = root.AddComponent<PlayerInteractionScanner>();
            var target = CreateTarget(scanner, out var visual);
            var router = root.AddComponent<WhiteboxInteractionFeedbackRouter>();
            router.Configure(host, scanner);

            Assert.That(scanner.CurrentTarget, Is.SameAs(target));

            host.NotifyFeedback("已安装车轴");
            Assert.That(visual.State, Is.EqualTo(InteractionVisualState.Success));

            visual.ClearFeedback();
            host.NotifyFeedback("无法安装车轮（库存缺失）");
            Assert.That(visual.State, Is.EqualTo(InteractionVisualState.Failure));
        }

        [Test]
        public void AnswerResultUsesLastTargetWhileQuizLocksScanner()
        {
            var worldSession = new DomainWorldGameSession();
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(worldSession, "测试答题反馈");
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var scanner = root.AddComponent<PlayerInteractionScanner>();
            scanner.Configure(root.transform, inputLock);
            scanner.ConfigurePlayer(root);
            scanner.ConfigureScan(3f, 180f, ~0);
            CreateTarget(scanner, out var visual);
            scanner.Configure(root.transform, inputLock);
            scanner.ScanNow();
            var router = root.AddComponent<WhiteboxInteractionFeedbackRouter>();
            router.Configure(host, scanner);
            var question = worldSession.DomainSession.Catalog.Questions[0];

            inputLock.SetInputLocked(true);
            Assert.That(scanner.CurrentTarget, Is.Null);

            host.SubmitAnswer(question.Id, question.CorrectOptionIndex);

            Assert.That(visual.State, Is.EqualTo(InteractionVisualState.Success));
        }

        [Test]
        public void PublicOutcomeMethodsAllowQuizPresenterToRouteLocalFeedback()
        {
            var scanner = root.AddComponent<PlayerInteractionScanner>();
            var target = CreateTarget(scanner, out var visual);
            var router = root.AddComponent<WhiteboxInteractionFeedbackRouter>();
            router.Configure(null, scanner);

            Assert.That(scanner.CurrentTarget, Is.SameAs(target));
            Assert.That(router.ShowFailureForCurrentTarget(), Is.True);
            Assert.That(visual.State, Is.EqualTo(InteractionVisualState.Failure));

            Assert.That(router.ShowSuccessForCurrentTarget(), Is.True);
            Assert.That(visual.State, Is.EqualTo(InteractionVisualState.Success));
        }

        private TestInteractable CreateTarget(
            PlayerInteractionScanner scanner,
            out InteractableVisualFeedback visual)
        {
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "FeedbackTarget";
            targetObject.transform.SetParent(root.transform, false);
            targetObject.transform.localPosition = Vector3.forward;
            var target = targetObject.AddComponent<TestInteractable>();
            visual = targetObject.AddComponent<InteractableVisualFeedback>();

            scanner.Configure(root.transform, null);
            scanner.ConfigurePlayer(root);
            scanner.ConfigureScan(3f, 180f, ~0);
            visual.Configure(scanner, target, new[] { targetObject.GetComponent<Renderer>() });
            scanner.ScanNow();
            return target;
        }

        private sealed class TestInteractable : MonoBehaviour, IPlayerInteractable
        {
            public string InteractionPrompt => "测试交互";

            public bool CanInteract(InteractionContext context)
            {
                return true;
            }

            public void Interact(InteractionContext context)
            {
            }
        }
    }
}
