namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Warrior combat values that don't belong in SkillCatalog
    /// (Presentation-side, see that class's doc) because they're pure
    /// Domain values Domain.Tests needs to reference directly (Domain.Tests
    /// cannot see Presentation). Basic attack has no SkillCatalog/
    /// SkillDefinition entry - RadialSkillMenu.CastBasicAttack is a separate
    /// button, not part of SkillCatalog.ForClass's 5-slot array - so its
    /// range lives here instead.
    /// </summary>
    public static class WarriorCombatConstants
    {
        /// <summary>
        /// Melee basic attack ("대검베기") forward reach in tiles - a 2-tile
        /// wide-sword sweep straight ahead of the caster's facing direction
        /// (SkillRangeShape.Line semantics, see SkillRangeCalculator.TilesInLine).
        /// Referenced by WarriorSkillVfxPlayer.PlayBasicAttack for the VFX
        /// footprint and by WarriorBasicAttackRangeTests (Domain-only, mirrors
        /// WarriorSkillRangeTests' convention of documenting a warrior range
        /// value with a dedicated test).
        /// </summary>
        public const int BasicAttackRangeTiles = 2;
    }
}
