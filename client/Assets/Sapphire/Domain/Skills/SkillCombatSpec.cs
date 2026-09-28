namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Immutable combat tuning for one skill: mana cost, cooldown, damage
    /// multiplier and the delay before its hit is resolved (matches the
    /// existing `AttackArea(tiles, multiplier, delay)` shape Presentation
    /// already calls). Damage multiplier 0 marks a non-damaging skill
    /// (movement/buff) - callers gate `AttackArea` on multiplier &gt; 0.
    /// </summary>
    public readonly struct SkillCombatSpec
    {
        public readonly string SkillId;
        public readonly int ManaCost;
        public readonly float CooldownSeconds;
        public readonly float DamageMultiplier;
        public readonly float HitDelaySeconds;

        /// <summary>
        /// Extra delay per tile along the range list for travelling effects
        /// (projectiles): tile i is hit at HitDelaySeconds + i * this. 0 hits
        /// every tile at once (area effects).
        /// </summary>
        public readonly float HitStaggerPerTileSeconds;

        public SkillCombatSpec(string skillId, int manaCost, float cooldownSeconds, float damageMultiplier, float hitDelaySeconds, float hitStaggerPerTileSeconds = 0f)
        {
            SkillId = skillId;
            ManaCost = manaCost;
            CooldownSeconds = cooldownSeconds;
            DamageMultiplier = damageMultiplier;
            HitDelaySeconds = hitDelaySeconds;
            HitStaggerPerTileSeconds = hitStaggerPerTileSeconds;
        }
    }
}
