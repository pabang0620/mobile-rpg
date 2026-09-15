using System.Collections.Generic;
using System.Linq;

namespace Sapphire.Domain.Character
{
    /// <summary>
    /// One account's character slots (max 4, per docs/planning/01_PRODUCT.md's
    /// "로컬 슬롯" scope and the task's explicit 4-slot cap). Owns slot-count
    /// and name-uniqueness rules; does not know about save files - the
    /// Presentation/Infrastructure repository loads a roster's initial slots
    /// and persists it back after a mutation. A rejected add/remove never
    /// mutates the roster (see AGENTS.md "명령 결과는 Accepted 또는 명시적
    /// 거절 코드로 통일" principle, applied here even though this isn't
    /// combat).
    /// </summary>
    public sealed class CharacterRoster
    {
        public const int MaxSlots = 4;

        private readonly List<CharacterSlot> slots = new List<CharacterSlot>();

        public CharacterRoster(IEnumerable<CharacterSlot> initialSlots = null)
        {
            if (initialSlots != null)
            {
                slots.AddRange(initialSlots);
            }
        }

        public IReadOnlyList<CharacterSlot> Slots => slots;

        public CharacterRosterResult TryAddCharacter(string id, string name, CharacterClass characterClass, out CharacterSlot created)
        {
            created = null;

            if (slots.Count >= MaxSlots)
            {
                return CharacterRosterResult.RosterFull;
            }

            IEnumerable<string> existingNames = slots.Select(s => s.Name);
            CharacterNameValidationResult nameResult = CharacterNameValidator.Validate(name, existingNames);
            switch (nameResult)
            {
                case CharacterNameValidationResult.TooShort:
                    return CharacterRosterResult.NameTooShort;
                case CharacterNameValidationResult.TooLong:
                    return CharacterRosterResult.NameTooLong;
                case CharacterNameValidationResult.InvalidCharacters:
                    return CharacterRosterResult.NameInvalidCharacters;
                case CharacterNameValidationResult.Duplicate:
                    return CharacterRosterResult.NameDuplicate;
            }

            created = new CharacterSlot(id, name, characterClass, 1);
            slots.Add(created);
            return CharacterRosterResult.Added;
        }

        public bool TryRemoveCharacter(string id)
        {
            int index = slots.FindIndex(s => s.Id == id);
            if (index < 0)
            {
                return false;
            }

            slots.RemoveAt(index);
            return true;
        }
    }
}
