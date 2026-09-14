using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// The 5 skill-menu slots shown in RadialSkillMenu (alongside a separate,
    /// not-catalog-driven basic attack button). The first 4 follow the skill
    /// table in docs/planning/02_SYSTEM_CONTRACTS.md (arcane bolt / frost wave
    /// / blink / shield); range values are that table's world-unit ranges
    /// reinterpreted as tiles (CellSize == 1, so 1u == 1 tile - see
    /// SkillRangeCalculator). The 5th slot ("질주"/Haste) was added 2026-09-14
    /// as a self-cast utility skill and has no entry in that planning table.
    /// All 6 icon sprite names referenced here (plus the basic attack button's)
    /// live in the single SkillIconsSet.png sheet - see
    /// ArtImportConfigurator.ConfigureSkillIconsSet (2026-09-14 full UI asset
    /// replacement, superseded the old 2-sheet SkillIcons.png/
    /// SkillIconsExtra.png setup). Haste is handled specially in
    /// RadialSkillMenu (triggers GridMoveAnimator's speed boost).
    /// </summary>
    public static class SkillCatalog
    {
        /// <summary>Id checked by RadialSkillMenu to trigger the speed-boost effect.</summary>
        public const string HasteSkillId = "skill.haste";

        public static readonly SkillDefinition[] All =
        {
            new SkillDefinition("skill.arcane_bolt", "비전탄", "SkillIcons_ArcaneBolt", SkillRangeShape.Line, 5),
            new SkillDefinition("skill.frost_wave", "서리 파동", "SkillIcons_FrostWave", SkillRangeShape.Radius, 2),
            new SkillDefinition("skill.blink", "점멸", "SkillIcons_Blink", SkillRangeShape.Line, 2),
            new SkillDefinition("skill.shield", "보호막", "SkillIcons_Shield", SkillRangeShape.None, 0),
            new SkillDefinition(HasteSkillId, "질주", "SkillIcons_Haste", SkillRangeShape.None, 0),
        };
    }
}
