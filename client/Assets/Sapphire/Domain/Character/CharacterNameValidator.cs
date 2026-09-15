using System.Collections.Generic;

namespace Sapphire.Domain.Character
{
    /// <summary>
    /// Pure name validation for the character-creation screen: 2-8 characters,
    /// Hangul/Latin letters/digits only, no duplicate within the account's
    /// existing roster. No engine or I/O dependency - CharacterRoster calls
    /// this, and CharacterCreate's Presentation controller surfaces the
    /// result as on-screen text.
    /// </summary>
    public static class CharacterNameValidator
    {
        public const int MinLength = 2;
        public const int MaxLength = 8;

        public static CharacterNameValidationResult Validate(string name, IEnumerable<string> existingNames)
        {
            string trimmed = name ?? string.Empty;

            if (trimmed.Length < MinLength)
            {
                return CharacterNameValidationResult.TooShort;
            }

            if (trimmed.Length > MaxLength)
            {
                return CharacterNameValidationResult.TooLong;
            }

            foreach (char c in trimmed)
            {
                if (!IsAllowedCharacter(c))
                {
                    return CharacterNameValidationResult.InvalidCharacters;
                }
            }

            if (existingNames != null)
            {
                foreach (string existing in existingNames)
                {
                    if (string.Equals(existing, trimmed, System.StringComparison.Ordinal))
                    {
                        return CharacterNameValidationResult.Duplicate;
                    }
                }
            }

            return CharacterNameValidationResult.Valid;
        }

        // Hangul syllables (가-힣) + standalone Hangul jamo (ㄱ-ㅣ) + ASCII
        // letters + digits. Standalone jamo is included so a name typed with
        // an IME mid-composition doesn't look "invalid" character-by-character;
        // full syllables are the common case.
        private static bool IsAllowedCharacter(char c)
        {
            bool isDigit = c >= '0' && c <= '9';
            bool isLatin = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            bool isHangulSyllable = c >= '가' && c <= '힣';
            bool isHangulJamo = c >= 'ㄱ' && c <= 'ㆎ';
            return isDigit || isLatin || isHangulSyllable || isHangulJamo;
        }
    }
}
