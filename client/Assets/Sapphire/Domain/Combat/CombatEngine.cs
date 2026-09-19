using System;

namespace Sapphire.Domain.Combat
{
    public static class CombatEngine
    {
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
    }
}
