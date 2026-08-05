using System;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class PartDefinition
    {
        public PartDefinition(PartId id, string key, string displayName)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A part key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A part display name is required.", nameof(displayName));

            Id = id;
            Key = key;
            DisplayName = displayName;
        }

        public PartId Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
    }
}
