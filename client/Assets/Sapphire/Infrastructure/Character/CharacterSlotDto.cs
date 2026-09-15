using System;

namespace Sapphire.Infrastructure.Character
{
    /// <summary>
    /// JsonUtility-serializable mirror of Sapphire.Domain.Character.CharacterSlot.
    /// Kept separate from the Domain type (which has no [Serializable]/Unity
    /// dependency by design) so Domain never needs to know about JSON at all -
    /// CharacterRosterFileRepository converts to/from this on every load/save.
    /// </summary>
    [Serializable]
    public sealed class CharacterSlotDto
    {
        public string id;
        public string name;
        public string characterClass;
        public int level;
    }

    /// <summary>Top-level JSON document for one account's roster file (JsonUtility requires a single root object, not a bare array).</summary>
    [Serializable]
    public sealed class CharacterRosterDto
    {
        public CharacterSlotDto[] slots = Array.Empty<CharacterSlotDto>();
    }
}
