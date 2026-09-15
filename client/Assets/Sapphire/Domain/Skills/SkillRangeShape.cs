namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// The shape a skill's tile range covers, expressed purely in grid
    /// terms - no world units, no target resolution.
    /// </summary>
    public enum SkillRangeShape
    {
        /// <summary>Self-cast or no spatial range (e.g. a buff).</summary>
        None,

        /// <summary>A straight line of tiles extending from the caster's facing direction.</summary>
        Line,

        /// <summary>A square area of tiles centered on the caster (Chebyshev radius).</summary>
        Radius,

        /// <summary>A widening fan of tiles directly ahead of the caster's facing direction (see SkillRangeCalculator.TilesInFrontCone).</summary>
        Cone
    }
}
