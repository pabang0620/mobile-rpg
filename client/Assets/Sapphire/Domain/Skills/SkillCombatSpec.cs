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

        public SkillCombatSpec(string skillId, int manaCost, float cooldownSeconds, float damageMultiplier, float hitDelaySeconds)
        {
            SkillId = skillId;
            ManaCost = manaCost;
            CooldownSeconds = cooldownSeconds;
            DamageMultiplier = damageMultiplier;
            HitDelaySeconds = hitDelaySeconds;
        }
    }
}
