using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Combat;
using Sapphire.Domain.Grid;
using UnityEngine.SceneManagement;

namespace Sapphire.Presentation.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        public CombatStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        public ManaComponent Mana { get; private set; }
        public ExpComponent Exp { get; private set; }

        private float mpRegenTimer = 0f;
        private bool isDead = false;

        private void Awake()
        {
            Stats = new CombatStats(maxHp: 100, maxMp: 50, attack: 15, defense: 5);
            Health = new HealthComponent(Stats.MaxHp);
            Mana = new ManaComponent(Stats.MaxMp);
            Exp = new ExpComponent();

            Exp.OnLevelUp += HandleLevelUp;
            Health.OnDied += HandleDeath;
        }

        private void Update()
        {
            if (isDead) return;

            // MP Regen: 1 MP per second
            mpRegenTimer += Time.deltaTime;
            if (mpRegenTimer >= 1f)
            {
                mpRegenTimer -= 1f;
                Mana.Restore(1);
            }
        }

        public void OnHit(int damage)
        {
            if (isDead) return;
            StartCoroutine(HitFlash());
        }

        private IEnumerator HitFlash()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) yield break;

            Color original = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (!isDead) renderer.color = original;
        }

        private void HandleLevelUp(int newLevel)
        {
            if (isDead) return;
            Health.OnDied -= HandleDeath;

            // Increase stats
            int newHp = 100 + (newLevel - 1) * 20;
            int newMp = 50 + (newLevel - 1) * 10;
            int newAtk = 15 + (newLevel - 1) * 5;
            int newDef = 5 + (newLevel - 1) * 2;
            
            Stats = new CombatStats(maxHp: newHp, maxMp: newMp, attack: newAtk, defense: newDef);
            
            // Full heal
            Health = new HealthComponent(Stats.MaxHp);
            Mana = new ManaComponent(Stats.MaxMp);
            
            Health.OnDied += HandleDeath;

            // Level Up Visual Feedback
            DamagePopup.Spawn(transform.position + Vector3.up * 0.5f, 0, "LEVEL UP!");
            
            var shake = FindObjectOfType<CameraShake>();
            shake?.Shake(0.15f, 0.4f);
        }

        private void HandleDeath()
        {
            if (isDead) return;
            isDead = true;

            DamagePopup.Spawn(transform.position + Vector3.up * 0.5f, 0, "YOU DIED");
            
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = Color.red;

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            SceneManager.LoadScene("VillageHub");
        }

        public void AttackArea(IReadOnlyList<GridCoord> tiles, float skillMultiplier = 1.0f, float delay = 0f)
        {
            if (isDead) return;

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
            if (!isDead) ExecuteAttack(tiles, skillMultiplier);
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
                        if (monster.Health.IsDead) continue; 

                        int damage = CombatEngine.CalculateDamage(Stats, monster.Stats, skillMultiplier);
                        CombatEngine.ProcessAttack(Stats, monster.Stats, monster.Health, skillMultiplier);

                        monster.OnHit(damage, Stats);
                        DamagePopup.Spawn(monster.transform.position, damage);
                        hitAny = true;
                        
                        if (monster.Health.IsDead)
                        {
                            Exp.AddExp(monster.IsBoss ? 100 : 30);
                        }
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
