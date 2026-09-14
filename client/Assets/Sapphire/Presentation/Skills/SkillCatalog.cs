using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>Five grid-mage spells. Order also defines the VFX atlas row.</summary>
    public static class SkillCatalog
    {
        public const string ShieldSkillId = "skill.shield";
        public const string BlinkSkillId = "skill.blink";
        public const string ThunderSkillId = "skill.thunder_grid";
        public const string IceSpikeSkillId = "skill.ice_spike";
        public const string LightningSpearSkillId = "skill.lightning_spear";
        // Legacy identifier retained for code that still references the old speed boost.
        public const string HasteSkillId = "skill.haste";
        public static readonly SkillDefinition[] All =
        {
            new SkillDefinition(ShieldSkillId, "마력쉴드", "SkillIcons_Shield", SkillRangeShape.None, 0),
            new SkillDefinition(BlinkSkillId, "텔레포트", "SkillIcons_Blink", SkillRangeShape.Line, 2),
            new SkillDefinition(ThunderSkillId, "낙뢰", "SkillIcons_FrostWave", SkillRangeShape.Radius, 1),
            new SkillDefinition(IceSpikeSkillId, "고드름", "SkillIcons_ArcaneBolt", SkillRangeShape.Line, 5),
            new SkillDefinition(LightningSpearSkillId, "번개창", "SkillIcons_Haste", SkillRangeShape.Line, 4),
        };
    }
}
