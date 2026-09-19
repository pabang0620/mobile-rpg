using UnityEngine;
using Sapphire.Domain.Combat;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        public CombatStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        public ManaComponent Mana { get; private set; }

        private void Awake()
        {
            // Initial player stats
            Stats = new CombatStats(maxHp: 100, maxMp: 50, attack: 15, defense: 5);
            Health = new HealthComponent(100);
            Mana = new ManaComponent(50);
        }
        
        public void AttackTarget(int gridX, int gridY, float skillMultiplier = 1.0f)
        {
            var monsters = FindObjectsOfType<MonsterController>();
            foreach (var monster in monsters)
            {
                if (monster.GridX == gridX && monster.GridY == gridY)
                {
                    int damage = CombatEngine.CalculateDamage(Stats, monster.Stats, skillMultiplier);
                    CombatEngine.ProcessAttack(Stats, monster.Stats, monster.Health, skillMultiplier);
                    Debug.Log($"Attacked monster at {gridX}, {gridY} for {damage} damage! HP remaining: {monster.Health.CurrentHp}");
                    return; // Hit one monster per tile
                }
            }
        }
    }
}
