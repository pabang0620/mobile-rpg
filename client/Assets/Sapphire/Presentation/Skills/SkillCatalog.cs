using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// The 4 skill bar slots, in the same order as the 2x2 SkillIcons.png
    /// sheet (arcane bolt / frost wave / blink / shield) and as the skill
    /// table in docs/planning/02_SYSTEM_CONTRACTS.md. Range values are that
    /// table's world-unit ranges reinterpreted as tiles (CellSize == 1, so
    /// 1u == 1 tile - see SkillRangeCalculator).
    /// </summary>
    public static class SkillCatalog
    {
        public static readonly SkillDefinition[] All =
        {
            new SkillDefinition("skill.arcane_bolt", "비전탄", "SkillIcons_ArcaneBolt", SkillRangeShape.Line, 5),
            new SkillDefinition("skill.frost_wave", "서리 파동", "SkillIcons_FrostWave", SkillRangeShape.Radius, 2),
            new SkillDefinition("skill.blink", "점멸", "SkillIcons_Blink", SkillRangeShape.Line, 2),
            new SkillDefinition("skill.shield", "보호막", "SkillIcons_Shield", SkillRangeShape.None, 0),
        };
    }
}
