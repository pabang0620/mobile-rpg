using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Sapphire.Domain.Character;

namespace Sapphire.Infrastructure.Character
{
    /// <summary>
    /// Implements ICharacterRosterRepository with one JSON file per account
    /// under Application.persistentDataPath (docs/planning/01_PRODUCT.md's
    /// "로컬 슬롯" - no server, no online sync). File name is the account id
    /// with filesystem-unsafe characters stripped, since AccountIdValidator
    /// only constrains length, not character set.
    /// </summary>
    public sealed class CharacterRosterFileRepository : ICharacterRosterRepository
    {
        private const string FilePrefix = "characters_";
        private const string FileSuffix = ".json";

        public IReadOnlyList<CharacterSlot> Load(string accountId)
        {
            string path = ResolvePath(accountId);
            if (!File.Exists(path))
            {
                return Array.Empty<CharacterSlot>();
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            CharacterRosterDto dto = JsonUtility.FromJson<CharacterRosterDto>(json);
            if (dto == null || dto.slots == null)
            {
                return Array.Empty<CharacterSlot>();
            }

            var result = new List<CharacterSlot>(dto.slots.Length);
            foreach (CharacterSlotDto slotDto in dto.slots)
            {
                if (slotDto == null)
                {
                    continue;
                }

                if (!Enum.TryParse(slotDto.characterClass, out CharacterClass characterClass))
                {
                    Debug.LogWarning($"Unknown character class '{slotDto.characterClass}' in roster file, skipping slot '{slotDto.id}'.");
                    continue;
                }

                result.Add(new CharacterSlot(slotDto.id, slotDto.name, characterClass, Mathf.Max(1, slotDto.level)));
            }

            return result;
        }

        public void Save(string accountId, IReadOnlyList<CharacterSlot> slots)
        {
            var dto = new CharacterRosterDto
            {
                slots = new CharacterSlotDto[slots?.Count ?? 0]
            };

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    CharacterSlot slot = slots[i];
                    dto.slots[i] = new CharacterSlotDto
                    {
                        id = slot.Id,
                        name = slot.Name,
                        characterClass = slot.Class.ToString(),
                        level = slot.Level
                    };
                }
            }

            string path = ResolvePath(accountId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(dto, prettyPrint: true), Encoding.UTF8);
        }

        private static string ResolvePath(string accountId)
        {
            string safeAccountId = SanitizeForFileName(accountId ?? string.Empty);
            return Path.Combine(Application.persistentDataPath, FilePrefix + safeAccountId + FileSuffix);
        }

        private static string SanitizeForFileName(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                builder.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            }

            return builder.Length > 0 ? builder.ToString() : "default";
        }
    }
}
