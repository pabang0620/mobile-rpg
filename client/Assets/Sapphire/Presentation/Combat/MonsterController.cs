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
        [SerializeField] private bool spawnConfigured;
        [SerializeField] private int spawnX, spawnY;
        [SerializeField] private int spawnMaxHp, spawnAttack, spawnDefense;
        [SerializeField] private bool spawnIsBoss;
        [SerializeField] private Vector3 visualOffset;
        private bool runtimeInitialized;
        private UnityEngine.Tilemaps.Tile runtimeBlocker;
        private UnityEngine.Tilemaps.TileBase occupancyTile;
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
            if (runtimeInitialized) return;
            spawnConfigured = true;
            spawnX = x; spawnY = y;
            spawnMaxHp = maxHp; spawnAttack = atk; spawnDefense = def;
            spawnIsBoss = isBoss;
            WorldPoint spawnWorld = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            visualOffset = transform.position - new Vector3(spawnWorld.X, spawnWorld.Y, 0f);
            if (UnityEngine.Application.isPlaying) InitializeRuntime();
        }

        private void Awake()
        {
            InitializeRuntime();
        }

        private void InitializeRuntime()
        {
            if (!spawnConfigured || runtimeInitialized) return;
            runtimeInitialized = true;
            GridX = spawnX; GridY = spawnY;
            var gridBuilder = FindObjectOfType<TilemapGridMapBuilder>();
            if (gridBuilder != null)
                occupancyTile = gridBuilder.GetCollisionTile(new GridCoord(GridX, GridY));
            IsBoss = spawnIsBoss;
            aiInterval = IsBoss ? 1.0f : 1.5f;
            Stats = new CombatStats(spawnMaxHp, 0, spawnAttack, spawnDefense);
            Health = new HealthComponent(spawnMaxHp);
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            originalScale = transform.localScale;
            Health.OnDied += HandleDeath;

            // Old generated scenes may contain editor-created bars whose runtime
            // references were never serialized. Replace those once on load.
            foreach (var oldBar in GetComponentsInChildren<MonsterHpBar>(true))
            {
                oldBar.gameObject.SetActive(false);
                Destroy(oldBar.gameObject);
            }
            hpBar = MonsterHpBar.Create(transform);

            var ySort = GetComponent<DynamicYSort>();
            if (ySort == null) ySort = gameObject.AddComponent<DynamicYSort>();
            ySort.OrderOffset = Mathf.RoundToInt(visualOffset.y * 100f);

            if (IsBoss)
            {
                originalColor = new Color(0.8f, 0.4f, 1.0f); // Purple boss
                if (spriteRenderer != null) spriteRenderer.color = originalColor;
            }
        }

        private void OnDestroy()
        {
            if (Health != null) Health.OnDied -= HandleDeath;
            if (runtimeBlocker != null) Destroy(runtimeBlocker);
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
            PlayerGridController player = null;
            foreach (var candidate in FindObjectsOfType<PlayerGridController>())
            {
                if (!candidate.isActiveAndEnabled) continue;
                player = candidate;
                break;
            }
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
                if (combatController != null && combatController.Health != null && !combatController.Health.IsDead)
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

            // Validate bounds, Ground coverage and collision before any mutation.
            var nextCoord = new GridCoord(x, y);
            if (!gridBuilder.Build().IsWalkable(nextCoord)) return;
            var oldCoord = new GridCoord(GridX, GridY);
            if (gridBuilder.GetCollisionTile(oldCoord) != occupancyTile) return;
            var blockerTile = occupancyTile;
            if (blockerTile == null)
            {
                if (runtimeBlocker == null)
                    runtimeBlocker = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                blockerTile = runtimeBlocker;
            }
            if (!gridBuilder.SetCollisionBlocked(nextCoord, true, blockerTile)) return;
            if (occupancyTile != null && !gridBuilder.SetCollisionBlocked(oldCoord, false, occupancyTile))
            {
                gridBuilder.SetCollisionBlocked(nextCoord, false, blockerTile);
                return;
            }
            occupancyTile = blockerTile;

            GridX = x;
            GridY = y;

            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            StartCoroutine(SmoothMove(new Vector3(world.X, world.Y, 0f) + visualOffset));
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
            if (gridBuilder != null && occupancyTile != null)
                gridBuilder.SetCollisionBlocked(new GridCoord(GridX, GridY), false, occupancyTile);

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

