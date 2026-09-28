using System;

namespace Sapphire.Domain.Combat
{
    public static class CombatEngine
    {
        /// <summary>Chance for a hit resolved via ResolveHit to be a critical.</summary>
        public const double CritChance = 0.25;

        /// <summary>Multiplier applied to skillMultiplier on a critical hit.</summary>
        public const float CritMultiplier = 1.5f;

        /// <summary>Crit chance for monster attacks on the player (lower than the player's).</summary>
        public const double MonsterCritChance = 0.15;

        public static int CalculateDamage(CombatStats attacker, CombatStats defender, float skillMultiplier = 1.0f)
        {
            // Simple damage formula: (Attack * SkillMultiplier) - Defense
            // Minimum damage is 1 on a successful hit.
            int baseDamage = (int)(attacker.Attack * skillMultiplier);
            int finalDamage = baseDamage - defender.Defense;
            return Math.Max(1, finalDamage);
        }

        public static bool ProcessAttack(CombatStats attacker, CombatStats defender, HealthComponent defenderHealth, float skillMultiplier = 1.0f)
        {
            if (defenderHealth.IsDead) return false;

            int damage = CalculateDamage(attacker, defender, skillMultiplier);
            defenderHealth.TakeDamage(damage);

            return true;
        }

        /// <summary>
        /// Single source of truth for "did this attack land, was it a crit,
        /// how much damage, did it kill" - rolls the crit, applies damage to
        /// defenderHealth and returns the outcome. No-ops (Damage=0,
        /// IsCrit=false, Killed=false) when defenderHealth is already dead,
        /// same guard ProcessAttack uses.
        /// </summary>
        public static HitResult ResolveHit(CombatStats attacker, CombatStats defender, HealthComponent defenderHealth, float skillMultiplier, ICombatRandom rng)
        {
            return ResolveHit(attacker, defender, defenderHealth, skillMultiplier, rng, CritChance);
        }

        /// <summary>Same as the overload above with an explicit crit chance (e.g. MonsterCritChance).</summary>
        public static HitResult ResolveHit(CombatStats attacker, CombatStats defender, HealthComponent defenderHealth, float skillMultiplier, ICombatRandom rng, double critChance)
        {
            if (defenderHealth == null || defenderHealth.IsDead)
            {
                return new HitResult(0, false, false);
            }

            bool isCrit = rng != null && rng.NextDouble() < critChance;
            float finalMultiplier = isCrit ? skillMultiplier * CritMultiplier : skillMultiplier;

            int damage = CalculateDamage(attacker, defender, finalMultiplier);
            defenderHealth.TakeDamage(damage);

            return new HitResult(damage, isCrit, defenderHealth.IsDead);
        }
    }
}
