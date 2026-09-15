using NUnit.Framework;
using Sapphire.Domain.Character;

namespace Sapphire.Domain.Tests
{
    public class CharacterRosterTests
    {
        [Test]
        public void TryAddCharacter_ValidName_AddsSlotAtLevelOne()
        {
            var roster = new CharacterRoster();

            CharacterRosterResult result = roster.TryAddCharacter("id-1", "달빛전사", CharacterClass.Warrior, out CharacterSlot created);

            Assert.AreEqual(CharacterRosterResult.Added, result);
            Assert.AreEqual(1, roster.Slots.Count);
            Assert.AreEqual("달빛전사", created.Name);
            Assert.AreEqual(CharacterClass.Warrior, created.Class);
            Assert.AreEqual(1, created.Level);
        }

        [Test]
        public void TryAddCharacter_DuplicateName_ReturnsNameDuplicateAndDoesNotMutate()
        {
            var roster = new CharacterRoster();
            roster.TryAddCharacter("id-1", "달빛전사", CharacterClass.Warrior, out _);

            CharacterRosterResult result = roster.TryAddCharacter("id-2", "달빛전사", CharacterClass.Mage, out CharacterSlot created);

            Assert.AreEqual(CharacterRosterResult.NameDuplicate, result);
            Assert.IsNull(created);
            Assert.AreEqual(1, roster.Slots.Count);
        }

        [Test]
        public void TryAddCharacter_InvalidName_ReturnsSpecificRejectionAndDoesNotMutate()
        {
            var roster = new CharacterRoster();

            CharacterRosterResult result = roster.TryAddCharacter("id-1", "a", CharacterClass.Mage, out CharacterSlot created);

            Assert.AreEqual(CharacterRosterResult.NameTooShort, result);
            Assert.IsNull(created);
            Assert.AreEqual(0, roster.Slots.Count);
        }

        [Test]
        public void TryAddCharacter_AtMaxSlots_ReturnsRosterFullAndDoesNotMutate()
        {
            var roster = new CharacterRoster();
            roster.TryAddCharacter("id-1", "이름하나", CharacterClass.Mage, out _);
            roster.TryAddCharacter("id-2", "이름둘", CharacterClass.Warrior, out _);
            roster.TryAddCharacter("id-3", "이름셋", CharacterClass.Mage, out _);
            roster.TryAddCharacter("id-4", "이름넷", CharacterClass.Warrior, out _);

            CharacterRosterResult result = roster.TryAddCharacter("id-5", "이름다섯", CharacterClass.Mage, out CharacterSlot created);

            Assert.AreEqual(CharacterRosterResult.RosterFull, result);
            Assert.IsNull(created);
            Assert.AreEqual(CharacterRoster.MaxSlots, roster.Slots.Count);
        }

        [Test]
        public void TryRemoveCharacter_ExistingId_RemovesAndReturnsTrue()
        {
            var roster = new CharacterRoster();
            roster.TryAddCharacter("id-1", "달빛전사", CharacterClass.Warrior, out _);

            bool removed = roster.TryRemoveCharacter("id-1");

            Assert.IsTrue(removed);
            Assert.AreEqual(0, roster.Slots.Count);
        }

        [Test]
        public void TryRemoveCharacter_UnknownId_ReturnsFalseAndDoesNotMutate()
        {
            var roster = new CharacterRoster();
            roster.TryAddCharacter("id-1", "달빛전사", CharacterClass.Warrior, out _);

            bool removed = roster.TryRemoveCharacter("does-not-exist");

            Assert.IsFalse(removed);
            Assert.AreEqual(1, roster.Slots.Count);
        }

        [Test]
        public void Constructor_WithInitialSlots_PopulatesSlots()
        {
            var seed = new[] { new CharacterSlot("id-1", "기존캐릭터", CharacterClass.Mage, 3) };

            var roster = new CharacterRoster(seed);

            Assert.AreEqual(1, roster.Slots.Count);
            Assert.AreEqual("기존캐릭터", roster.Slots[0].Name);
            Assert.AreEqual(3, roster.Slots[0].Level);
        }
    }
}
