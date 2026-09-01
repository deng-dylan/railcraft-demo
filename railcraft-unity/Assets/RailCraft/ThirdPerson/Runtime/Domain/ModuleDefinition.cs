using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class ModuleDefinition
    {
        private readonly ReadOnlyCollection<PartId> requiredParts;
        private readonly ReadOnlyCollection<ModuleId> requiredModules;

        public ModuleDefinition(
            ModuleId id,
            string key,
            string displayName,
            IEnumerable<PartId> requiredParts)
            : this(id, key, displayName, requiredParts, Array.Empty<ModuleId>())
        {
        }

        public ModuleDefinition(
            ModuleId id,
            string key,
            string displayName,
            IEnumerable<PartId> requiredParts,
            IEnumerable<ModuleId> requiredModules)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A module key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A module display name is required.", nameof(displayName));
            if (requiredParts == null)
                throw new ArgumentNullException(nameof(requiredParts));
            if (requiredModules == null)
                throw new ArgumentNullException(nameof(requiredModules));

            var copiedParts = new List<PartId>(requiredParts);
            var copiedModules = new List<ModuleId>(requiredModules);
            if (copiedParts.Count + copiedModules.Count == 0)
                throw new ArgumentException("An assembly recipe needs at least one part or child assembly.");
            if (new HashSet<PartId>(copiedParts).Count != copiedParts.Count)
                throw new ArgumentException("A module recipe cannot contain duplicate parts.", nameof(requiredParts));
            if (new HashSet<ModuleId>(copiedModules).Count != copiedModules.Count)
                throw new ArgumentException("A module recipe cannot contain duplicate child assemblies.", nameof(requiredModules));
            if (copiedModules.Contains(id))
                throw new ArgumentException("An assembly cannot require itself.", nameof(requiredModules));

            Id = id;
            Key = key;
            DisplayName = displayName;
            this.requiredParts = copiedParts.AsReadOnly();
            this.requiredModules = copiedModules.AsReadOnly();
        }

        public ModuleId Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public IReadOnlyList<PartId> RequiredParts => requiredParts;
        public IReadOnlyList<ModuleId> RequiredModules => requiredModules;
        public int RequiredInputCount => requiredParts.Count + requiredModules.Count;

        public bool Requires(PartId partId)
        {
            return requiredParts.Contains(partId);
        }

        public bool Requires(ModuleId moduleId)
        {
            return requiredModules.Contains(moduleId);
        }
    }
}
