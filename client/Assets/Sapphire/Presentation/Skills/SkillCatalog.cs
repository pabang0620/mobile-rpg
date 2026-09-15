using System;
using Sapphire.Domain.Character;
using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Per-class skill bar content (5 slots, matching keys 1-5). Mage's array
    /// and every Mage id constant/value below are unchanged from before this
    /// class became class-aware (see docs/HANDOFF.md F4 fix comment on the
    /// Mage entries) - ForClass(CharacterClass.Mage) returns exactly what the
    /// old `All` field used to. Warrior's 5 entries are new (see
    /// docs/HANDOFF.md's character-flow entry for the kit design). Order also
    /// defines the VFX row index each class's ISkillVfxPlayer.Play(row, ...)
    /// receives - SkillVfxPlayer for Mage, WarriorSkillVfxPlayer for Warrior.
    /// </summary>
    public static class SkillCatalog
    {
        // --- Mage ids (unchanged) ---
        public const string ShieldSkillId = "skill.shield";
        public const string BlinkSkillId = "skill.blink";
        public const string ThunderSkillId = "skill.thunder_grid";
        public const string IceSpikeSkillId = "skill.ice_spike";
        public const string LightningSpearSkillId = "skill.lightning_spear";

        // --- Warrior ids (new) ---
        public const string DashSkillId = "skill.dash";
        public const string WhirlwindSkillId = "skill.whirlwind";
        public const string ShieldBlockSkillId = "skill.shield_block";
        public const string WarCrySkillId = "skill.war_cry";
        public const string GroundSlamSkillId = "skill.ground_slam";

        private static readonly SkillDefinition[] MageSkills =
        {
            // 2026-09-16 (F4 fix): icon<->name mapping was wrong for these 3
            // (낙뢰 showed a snowflake, 고드름 showed a crystal). Corrected to
            // match each icon's actual artwork: 낙뢰(thunder)->Haste's winged
            // lightning-bolt icon, 고드름(ice spike)->FrostWave's snowflake,
            // 번개창(lightning spear)->ArcaneBolt's crystal spear-tip. Sprite
            // asset names are unchanged (still SkillIcons_Haste/FrostWave/
            // ArcaneBolt) - only which SkillDefinition references which name
            // moved.
            new SkillDefinition(ShieldSkillId, "마력쉴드", "SkillIcons_Shield", SkillRangeShape.None, 0),
            new SkillDefinition(BlinkSkillId, "텔레포트", "SkillIcons_Blink", SkillRangeShape.Line, 2),
            new SkillDefinition(ThunderSkillId, "낙뢰", "SkillIcons_Haste", SkillRangeShape.Radius, 1),
            new SkillDefinition(IceSpikeSkillId, "고드름", "SkillIcons_FrostWave", SkillRangeShape.Line, 5),
            new SkillDefinition(LightningSpearSkillId, "번개창", "SkillIcons_ArcaneBolt", SkillRangeShape.Line, 4),
        };

        // Icon sprite names come from WarriorSkillIconsSetGold.png (see
        // WarriorArtImportConfigurator) - 6-cell sheet, reading order
        // BasicAttack/Dash/Whirlwind (top row), ShieldBlock/WarCry/GroundSlam
        // (bottom row); BasicAttack is used by RadialSkillMenu's separate
        // basic-attack button, not this array.
        private static readonly SkillDefinition[] WarriorSkills =
        {
            new SkillDefinition(DashSkillId, "돌진", "SkillIcons_Dash", SkillRangeShape.Line, 3),
            new SkillDefinition(WhirlwindSkillId, "회오리베기", "SkillIcons_Whirlwind", SkillRangeShape.Radius, 1),
            new SkillDefinition(ShieldBlockSkillId, "방패막기", "SkillIcons_ShieldBlock", SkillRangeShape.None, 0),
            new SkillDefinition(WarCrySkillId, "전쟁함성", "SkillIcons_WarCry", SkillRangeShape.None, 0),
            new SkillDefinition(GroundSlamSkillId, "대지강타", "SkillIcons_GroundSlam", SkillRangeShape.Cone, 1),
        };

        public static SkillDefinition[] ForClass(CharacterClass characterClass)
        {
            switch (characterClass)
            {
                case CharacterClass.Mage:
                    return MageSkills;
                case CharacterClass.Warrior:
                    return WarriorSkills;
                default:
                    throw new ArgumentOutOfRangeException(nameof(characterClass), characterClass, "Unknown character class.");
            }
        }
    }
}
