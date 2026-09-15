using System;

namespace Sapphire.Domain.Character
{
    /// <summary>
    /// One saved character: identity, chosen class and level. Plain
    /// immutable data - no engine/serialization attributes here (Infrastructure
    /// maps this to/from its own JSON-serializable DTO instead of this type
    /// growing Unity/JsonUtility concerns).
    /// </summary>
    public sealed class CharacterSlot
    {
        public string Id { get; }
        public string Name { get; }
        public CharacterClass Class { get; }
        public int Level { get; }

        public CharacterSlot(string id, string name, CharacterClass characterClass, int level)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Character id must not be empty.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Character name must not be empty.", nameof(name));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Character level must be at least 1.");
            }

            Id = id;
            Name = name;
            Class = characterClass;
            Level = level;
        }
    }
}
