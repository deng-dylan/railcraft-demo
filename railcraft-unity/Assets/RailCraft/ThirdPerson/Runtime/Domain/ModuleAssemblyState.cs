using System.Collections.Generic;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class ModuleAssemblyState
    {
        private readonly HashSet<PartId> installedParts = new HashSet<PartId>();
        private readonly HashSet<ModuleId> installedModules = new HashSet<ModuleId>();

        internal ModuleAssemblyState(ModuleDefinition definition)
        {
            Definition = definition;
        }

        public ModuleDefinition Definition { get; }
        public int InstalledPartCount => installedParts.Count;
        public int RequiredPartCount => Definition.RequiredParts.Count;
        public int InstalledModuleCount => installedModules.Count;
        public int RequiredModuleCount => Definition.RequiredModules.Count;
        public int InstalledInputCount => InstalledPartCount + InstalledModuleCount;
        public int RequiredInputCount => Definition.RequiredInputCount;
        public bool IsComplete =>
            installedParts.Count == Definition.RequiredParts.Count &&
            installedModules.Count == Definition.RequiredModules.Count;

        public IReadOnlyList<PartId> InstalledParts
        {
            get
            {
                var snapshot = new List<PartId>();
                foreach (var partId in Definition.RequiredParts)
                {
                    if (installedParts.Contains(partId))
                        snapshot.Add(partId);
                }
                return snapshot.AsReadOnly();
            }
        }

        public IReadOnlyList<ModuleId> InstalledModules
        {
            get
            {
                var snapshot = new List<ModuleId>();
                foreach (var moduleId in Definition.RequiredModules)
                {
                    if (installedModules.Contains(moduleId))
                        snapshot.Add(moduleId);
                }
                return snapshot.AsReadOnly();
            }
        }

        public bool HasInstalled(PartId partId)
        {
            return installedParts.Contains(partId);
        }

        public bool HasInstalled(ModuleId moduleId)
        {
            return installedModules.Contains(moduleId);
        }

        internal bool Install(PartId partId)
        {
            return installedParts.Add(partId);
        }

        /// <summary>
        /// Installs a set of part inputs only when every input is valid and
        /// currently absent. Validation completes before any set mutation so
        /// callers can use this for package-level atomic assembly.
        /// </summary>
        internal bool InstallAll(IEnumerable<PartId> partIds)
        {
            if (partIds == null)
                return false;

            var pending = new List<PartId>(partIds);
            if (pending.Count == 0)
                return false;

            var unique = new HashSet<PartId>();
            foreach (var partId in pending)
            {
                if (!unique.Add(partId)
                    || !Definition.Requires(partId)
                    || installedParts.Contains(partId))
                    return false;
            }

            foreach (var partId in pending)
                installedParts.Add(partId);
            return true;
        }

        internal bool Install(ModuleId moduleId)
        {
            return installedModules.Add(moduleId);
        }

        internal void Reset()
        {
            installedParts.Clear();
            installedModules.Clear();
        }
    }
}
