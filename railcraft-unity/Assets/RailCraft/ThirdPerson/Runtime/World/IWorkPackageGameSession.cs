using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Optional extension of the world session for the current compact,
    /// package-level training flow. Legacy test sessions can continue to
    /// implement only IWorldGameSession and use the per-part API.
    /// </summary>
    public interface IWorkPackageGameSession
    {
        bool IsWorkPackageKnowledgeComplete(WorkPackageId workPackageId);
        bool IsWorkPackageUnlocked(WorkPackageId workPackageId);
        bool IsWorkPackageCollected(WorkPackageId workPackageId);
        int GetWorkPackageAnsweredQuestionCount(WorkPackageId workPackageId);
        WorkPackageAnswerResult SubmitWorkPackageAnswer(
            WorkPackageId workPackageId,
            string questionId,
            int selectedOptionIndex);
        WorkPackageCollectionResult CollectWorkPackage(WorkPackageId workPackageId);
        WorkPackageInstallationResult InstallWorkPackage(
            ModuleId moduleId,
            WorkPackageId workPackageId);
    }
}
