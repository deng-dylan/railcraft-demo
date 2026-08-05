using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    public enum WhiteboxMilestoneKind
    {
        PartInstalled,
        ModuleInstalled,
        Commissioning
    }

    public readonly struct WhiteboxMilestoneEvent
    {
        private WhiteboxMilestoneEvent(
            WhiteboxMilestoneKind kind,
            PartId partId,
            ModuleId moduleId,
            CommissioningPhase commissioningPhase)
        {
            Kind = kind;
            PartId = partId;
            ModuleId = moduleId;
            CommissioningPhase = commissioningPhase;
        }

        public WhiteboxMilestoneKind Kind { get; }
        public PartId PartId { get; }
        public ModuleId ModuleId { get; }
        public CommissioningPhase CommissioningPhase { get; }

        public static WhiteboxMilestoneEvent ForPart(PartId partId, ModuleId moduleId)
        {
            return new WhiteboxMilestoneEvent(
                WhiteboxMilestoneKind.PartInstalled,
                partId,
                moduleId,
                CommissioningPhase.Locked);
        }

        public static WhiteboxMilestoneEvent ForModule(ModuleId moduleId)
        {
            return new WhiteboxMilestoneEvent(
                WhiteboxMilestoneKind.ModuleInstalled,
                default,
                moduleId,
                CommissioningPhase.Locked);
        }

        public static WhiteboxMilestoneEvent ForCommissioning(CommissioningPhase phase)
        {
            return new WhiteboxMilestoneEvent(
                WhiteboxMilestoneKind.Commissioning,
                default,
                default,
                phase);
        }
    }
}
