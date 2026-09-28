using Sapphire.Domain.Combat;

namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Validates and commits a skill cast against mana and cooldown state.
    /// `Check` is non-mutating (safe to call before a side effect like a
    /// blink move); `TryCommit` re-validates and, only on Accepted, consumes
    /// mana and starts the cooldown - a rejected command never changes
    /// either (see AGENTS.md's Accepted/rejection-code contract).
    /// </summary>
    public static class SkillCastRules
    {
        public static SkillCastResult Check(string skillId, ManaComponent mana, SkillCooldownTracker cooldowns, double nowSeconds, out SkillCombatSpec spec)
        {
            if (!SkillCombatCatalog.TryGet(skillId, out spec))
            {
                return SkillCastResult.RejectedUnknownSkill;
            }

            if (cooldowns != null && !cooldowns.IsReady(skillId, nowSeconds))
            {
                return SkillCastResult.RejectedOnCooldown;
            }

            if (mana != null && mana.CurrentMp < spec.ManaCost)
            {
                return SkillCastResult.RejectedInsufficientMana;
            }

            return SkillCastResult.Accepted;
        }

        public static SkillCastResult TryCommit(string skillId, ManaComponent mana, SkillCooldownTracker cooldowns, double nowSeconds, out SkillCombatSpec spec)
        {
            SkillCastResult result = Check(skillId, mana, cooldowns, nowSeconds, out spec);
            if (result != SkillCastResult.Accepted)
            {
                return result;
            }

            if (mana != null && spec.ManaCost > 0)
            {
                if (!mana.TryConsume(spec.ManaCost))
                {
                    // Should be unreachable (Check already confirmed enough
                    // mana) but guard anyway rather than start a cooldown
                    // for an attack that did not actually consume mana.
                    return SkillCastResult.RejectedInsufficientMana;
                }
            }

            if (cooldowns != null)
            {
                cooldowns.Start(skillId, spec.CooldownSeconds, nowSeconds);
            }

            return SkillCastResult.Accepted;
        }
    }
}
