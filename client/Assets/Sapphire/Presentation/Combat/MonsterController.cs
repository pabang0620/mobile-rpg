using System.Collections;
using UnityEngine;
using Sapphire.Domain.Combat;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.World;

namespace Sapphire.Presentation.Combat
{
    public class MonsterController : MonoBehaviour
    {
        public CombatStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        
        public bool IsBoss { get; private set; }

        private SpriteRenderer spriteRenderer;
        private MonsterHpBar hpBar;
        private Color originalColor;
        private Vector3 originalScale;

        private float aiTimer = 0f;
        private float aiInterval = 1.5f;

        public void Initialize(int x, int y, int maxHp, int atk, int def, bool isBoss = false)
        {
            GridX = x; GridY = y;
            IsBoss = isBoss;
            aiInterval = isBoss ? 1.0f : 1.5f; // Boss is faster
            Stats = new CombatStats(maxHp, 0, atk, def);
            Health = new HealthComponent(maxHp);
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            originalScale = transform.localScale;
            Health.OnDied += HandleDeath;

            hpBar = MonsterHpBar.Create(transform);
            
            gameObject.AddComponent<Sapphire.Presentation.World.DynamicYSort>();
            
            if (IsBoss)
            {
                originalColor = new Color(0.8f, 0.4f, 1.0f); // Purple boss
                if (spriteRenderer != null) spriteRenderer.color = originalColor;
            }
        }

        public void OnHit(int damage, CombatStats attackerStats)
        {
            if (spriteRenderer == null) return;

            hpBar?.UpdateFill((float)Health.CurrentHp / Health.MaxHp);
            StartCoroutine(HitFlash());
            StartCoroutine(Knockback());
        }

        private IEnumerator HitFlash()
        {
            if (spriteRenderer == null) yield break;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null && !Health.IsDead)
                spriteRenderer.color = originalColor;
        }

        private IEnumerator Knockback()
        {
            Vector3 original = transform.position;
            Vector3 knocked = original + Vector3.up * 0.15f;
            float duration = 0.08f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(original, knocked, elapsed / duration);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(knocked, original, elapsed / duration);
                yield return null;
            }
            transform.position = original;
        }

        private void Update()
        {
            if (Health == null || Health.IsDead) return;

            aiTimer += Time.deltaTime;
            if (aiTimer >= aiInterval)
            {
                aiTimer -= aiInterval;
                ExecuteAI();
            }
        }

        private void ExecuteAI()
        {
            var player = FindObjectOfType<PlayerGridController>();
            if (player == null) return;

            var playerCoord = GridWorldConversion.WorldToGrid(new WorldPoint(player.transform.position.x, player.transform.position.y));
            int dx = playerCoord.X - GridX;
            int dy = playerCoord.Y - GridY;
            int dist = Mathf.Abs(dx) + Mathf.Abs(dy);

            if (dist > 5) return; // Player too far

            if (dist == 1)
            {
                // Attack Player
                var combatController = player.GetComponent<PlayerCombatController>();
                if (combatController != null && !combatController.Health.IsDead)
                {
                    bool isCrit = UnityEngine.Random.value < 0.15f;
                    float finalMultiplier = isCrit ? 1.5f : 1.0f;
                    int damage = CombatEngine.CalculateDamage(Stats, combatController.Stats, finalMultiplier);
                    CombatEngine.ProcessAttack(Stats, combatController.Stats, combatController.Health, finalMultiplier);
                    
                    combatController.OnHit(damage);
                    DamagePopup.Spawn(player.transform.position, damage, isCrit ? damage.ToString() + " CRIT!" : null);
                    HitEffectSpawner.Spawn(player.transform.position + new Vector3(0, 0.25f, -1f), isCrit);
                    var shake = FindObjectOfType<CameraShake>();
                    shake?.Shake(0.08f, 0.15f);

                    // Basic hop animation for attacking
                    StartCoroutine(Knockback());
                }
            }
            else
            {
                // Move towards player
                int nextX = GridX;
                int nextY = GridY;

                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                {
                    nextX += (int)Mathf.Sign(dx);
                }
                else
                {
                    nextY += (int)Mathf.Sign(dy);
                }

                TryMoveTo(nextX, nextY);
            }
        }

        private void TryMoveTo(int x, int y)
        {
            var gridBuilder = FindObjectOfType<TilemapGridMapBuilder>();
            if (gridBuilder == null) return;

            var collisionField = gridBuilder.GetType().GetField("collisionTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (collisionField == null) return;
            var collisionMap = (UnityEngine.Tilemaps.Tilemap)collisionField.GetValue(gridBuilder);

            if (collisionMap.HasTile(new Vector3Int(x, y, 0))) return; // Blocked

            // Move
            collisionMap.SetTile(new Vector3Int(GridX, GridY, 0), null);
            
            var blockerField = gridBuilder.GetType().GetField("blockerTile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            UnityEngine.Tilemaps.Tile blockerTile = null;
            if (blockerField != null) blockerTile = (UnityEngine.Tilemaps.Tile)blockerField.GetValue(gridBuilder);
            
            if (blockerTile == null) 
            {
                // fallback creating temp blocker if needed, but usually we just set the same tile it was
                // wait, if we don't have blocker tile, we can't properly block the new cell.
                // Let's just instantiate a dummy tile.
                blockerTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            }

            collisionMap.SetTile(new Vector3Int(x, y, 0), blockerTile);

            GridX = x;
            GridY = y;
            
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            StartCoroutine(SmoothMove(new Vector3(world.X, world.Y, 0f)));
        }

        private IEnumerator SmoothMove(Vector3 target)
        {
            Vector3 start = transform.position;
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }

        private void HandleDeath()
        {
            var gridBuilder = FindObjectOfType<TilemapGridMapBuilder>();
            if (gridBuilder != null)
            {
                var field = gridBuilder.GetType().GetField("collisionTilemap",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var collisionMap = (UnityEngine.Tilemaps.Tilemap)field.GetValue(gridBuilder);
                    collisionMap.SetTile(new Vector3Int(GridX, GridY, 0), null);
                }
            }

            StartCoroutine(DeathFade());
            ItemDrop.Spawn(transform.position);
        }

        private IEnumerator DeathFade()
        {
            float duration = 0.4f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

                transform.position = startPos + Vector3.down * (t * 0.3f);

                float scale = Mathf.Lerp(1f, 0.5f, t);
                transform.localScale = new Vector3(
                    originalScale.x * scale,
                    originalScale.y * scale,
                    1f);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

