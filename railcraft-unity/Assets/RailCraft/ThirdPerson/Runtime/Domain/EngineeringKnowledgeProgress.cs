using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class EngineeringKnowledgeProgress
    {
        private readonly HashSet<string> unlockedEntryIds = new HashSet<string>(
            StringComparer.Ordinal);

        public EngineeringKnowledgeProgress(EngineeringKnowledgeCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public EngineeringKnowledgeCatalog Catalog { get; }
        public bool IsCatalogEntranceUnlocked { get; private set; }
        public int UnlockedCount => unlockedEntryIds.Count;

        public IReadOnlyList<KnowledgeEntry> UnlockedEntries
        {
            get
            {
                var result = new List<KnowledgeEntry>();
                foreach (var entry in Catalog.Entries)
                {
                    if (unlockedEntryIds.Contains(entry.Id))
                        result.Add(entry);
                }
                return result.AsReadOnly();
            }
        }

        public bool IsUnlocked(string entryId)
        {
            return entryId != null && unlockedEntryIds.Contains(entryId);
        }

        public KnowledgeUnlockResult RecordCorrectAnswer(string questionId)
        {
            return Unlock(Catalog.GetEntriesForCorrectAnswer(questionId), false);
        }

        public KnowledgeUnlockResult RecordCompletedModule(ModuleId moduleId)
        {
            return Unlock(Catalog.GetEntriesForCompletedModule(moduleId), false);
        }

        public KnowledgeUnlockResult RecordCommissioningPhase(CommissioningPhase phase)
        {
            return Unlock(Catalog.GetEntriesForCommissioningPhase(phase), false);
        }

        public KnowledgeUnlockResult RecordVehicleCompleted()
        {
            var entranceChanged = !IsCatalogEntranceUnlocked;
            IsCatalogEntranceUnlocked = true;
            return Unlock(Catalog.GetVehicleCompletionEntries(), entranceChanged);
        }

        public void Reset()
        {
            unlockedEntryIds.Clear();
            IsCatalogEntranceUnlocked = false;
        }

        private KnowledgeUnlockResult Unlock(
            IReadOnlyList<KnowledgeEntry> candidates,
            bool additionalChange)
        {
            var newlyUnlocked = new List<KnowledgeEntry>();
            foreach (var entry in candidates)
            {
                if (unlockedEntryIds.Add(entry.Id))
                    newlyUnlocked.Add(entry);
            }
            newlyUnlocked.Sort((left, right) => left.UnlockOrder.CompareTo(right.UnlockOrder));
            return new KnowledgeUnlockResult(
                newlyUnlocked,
                additionalChange || newlyUnlocked.Count > 0,
                IsCatalogEntranceUnlocked);
        }
    }

    public sealed class KnowledgeUnlockResult
    {
        private readonly ReadOnlyCollection<KnowledgeEntry> newlyUnlockedEntries;

        internal KnowledgeUnlockResult(
            IEnumerable<KnowledgeEntry> newlyUnlockedEntries,
            bool changed,
            bool catalogEntranceUnlocked)
        {
            if (newlyUnlockedEntries == null)
                throw new ArgumentNullException(nameof(newlyUnlockedEntries));
            this.newlyUnlockedEntries =
                new List<KnowledgeEntry>(newlyUnlockedEntries).AsReadOnly();
            Changed = changed;
            CatalogEntranceUnlocked = catalogEntranceUnlocked;
        }

        public IReadOnlyList<KnowledgeEntry> NewlyUnlockedEntries => newlyUnlockedEntries;
        public int NewlyUnlockedCount => newlyUnlockedEntries.Count;
        public bool Changed { get; }
        public bool CatalogEntranceUnlocked { get; }
    }
}
