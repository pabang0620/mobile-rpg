using System.Collections;
using System.Collections.Generic;
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
            Stats = new CombatStats(maxHp: 100, maxMp: 50, attack: 15, defense: 5);
            Health = new HealthComponent(100);
            Mana = new ManaComponent(50);
        }

        public void AttackArea(IReadOnlyList<GridCoord> tiles, float skillMultiplier = 1.0f, float delay = 0f)
        {
            if (delay > 0f)
            {
                StartCoroutine(AttackAreaDelayed(tiles, skillMultiplier, delay));
            }
            else
            {
                ExecuteAttack(tiles, skillMultiplier);
            }
        }

        private IEnumerator AttackAreaDelayed(IReadOnlyList<GridCoord> tiles, float skillMultiplier, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteAttack(tiles, skillMultiplier);
        }

        private void ExecuteAttack(IReadOnlyList<GridCoord> tiles, float skillMultiplier)
        {
            var monsters = FindObjectsOfType<MonsterController>();
            bool hitAny = false;

            foreach (var tile in tiles)
            {
                foreach (var monster in monsters)
                {
                    if (monster == null) continue;
                    if (monster.GridX == tile.X && monster.GridY == tile.Y)
                    {
                        if (monster.Health.IsDead) continue; // Don't hit dead monsters

                        int damage = CombatEngine.CalculateDamage(Stats, monster.Stats, skillMultiplier);
                        CombatEngine.ProcessAttack(Stats, monster.Stats, monster.Health, skillMultiplier);

                        monster.OnHit(damage, Stats);
                        DamagePopup.Spawn(monster.transform.position, damage);
                        hitAny = true;
                    }
                }
            }

            if (hitAny)
            {
                var shake = FindObjectOfType<CameraShake>();
                shake?.Shake(0.08f, 0.15f);
            }
        }
    }
}
