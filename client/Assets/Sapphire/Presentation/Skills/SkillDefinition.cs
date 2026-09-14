using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Static content for one skill bar slot: display/icon data plus the
    /// tile range shape used for the visual range indicator. No MP/cooldown
    /// resource fields here - this slice has no resource system yet
    /// (see docs/planning/02_SYSTEM_CONTRACTS.md for the full future stats).
    /// </summary>
    public readonly struct SkillDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string IconSpriteName;
        public readonly SkillRangeShape RangeShape;
        public readonly int RangeTiles;

        public SkillDefinition(string id, string displayName, string iconSpriteName, SkillRangeShape rangeShape, int rangeTiles)
        {
            Id = id;
            DisplayName = displayName;
            IconSpriteName = iconSpriteName;
            RangeShape = rangeShape;
            RangeTiles = rangeTiles;
        }
    }
}
