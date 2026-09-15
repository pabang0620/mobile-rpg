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
        public static readonly SkillDefinition[] All =
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
    }
}
