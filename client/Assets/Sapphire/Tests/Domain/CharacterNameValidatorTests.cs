using System.Collections.Generic;
using NUnit.Framework;
using Sapphire.Domain.Character;

namespace Sapphire.Domain.Tests
{
    public class CharacterNameValidatorTests
    {
        [Test]
        public void Validate_TwoCharacters_ReturnsValid()
        {
            Assert.AreEqual(CharacterNameValidationResult.Valid, CharacterNameValidator.Validate("가나", null));
        }

        [Test]
        public void Validate_EightCharacters_ReturnsValid()
        {
            Assert.AreEqual(CharacterNameValidationResult.Valid, CharacterNameValidator.Validate("Warrior1", null));
        }

        [Test]
        public void Validate_OneCharacter_ReturnsTooShort()
        {
            Assert.AreEqual(CharacterNameValidationResult.TooShort, CharacterNameValidator.Validate("가", null));
        }

        [Test]
        public void Validate_NullName_ReturnsTooShort()
        {
            Assert.AreEqual(CharacterNameValidationResult.TooShort, CharacterNameValidator.Validate(null, null));
        }

        [Test]
        public void Validate_NineCharacters_ReturnsTooLong()
        {
            Assert.AreEqual(CharacterNameValidationResult.TooLong, CharacterNameValidator.Validate("123456789", null));
        }

        [Test]
        public void Validate_ContainsSpace_ReturnsInvalidCharacters()
        {
            Assert.AreEqual(CharacterNameValidationResult.InvalidCharacters, CharacterNameValidator.Validate("전사 왕", null));
        }

        [Test]
        public void Validate_ContainsSymbol_ReturnsInvalidCharacters()
        {
            Assert.AreEqual(CharacterNameValidationResult.InvalidCharacters, CharacterNameValidator.Validate("전사!", null));
        }

        [Test]
        public void Validate_DuplicateAgainstExisting_ReturnsDuplicate()
        {
            var existing = new List<string> { "달빛전사" };

            Assert.AreEqual(CharacterNameValidationResult.Duplicate, CharacterNameValidator.Validate("달빛전사", existing));
        }

        [Test]
        public void Validate_NotInExistingList_ReturnsValid()
        {
            var existing = new List<string> { "달빛전사" };

            Assert.AreEqual(CharacterNameValidationResult.Valid, CharacterNameValidator.Validate("달빛법사", existing));
        }
    }
}
