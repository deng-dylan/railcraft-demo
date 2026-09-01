using System;
using System.Collections.Generic;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class PartInventory
    {
        private static readonly PartId[] OrderedPartIds =
            (PartId[])Enum.GetValues(typeof(PartId));

        private readonly HashSet<PartId> heldParts = new HashSet<PartId>();

        public int Count => heldParts.Count;

        public IReadOnlyList<PartId> Parts
        {
            get
            {
                var snapshot = new List<PartId>();
                foreach (var partId in OrderedPartIds)
                {
                    if (heldParts.Contains(partId))
                        snapshot.Add(partId);
                }
                return snapshot.AsReadOnly();
            }
        }

        public bool Contains(PartId partId)
        {
            return heldParts.Contains(partId);
        }

        internal bool Grant(PartId partId)
        {
            return heldParts.Add(partId);
        }

        internal bool Consume(PartId partId)
        {
            return heldParts.Remove(partId);
        }

        internal void Reset()
        {
            heldParts.Clear();
        }
    }
}
