using System.Collections;
using UnityEngine;
using Sapphire.Domain.Combat;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.World;

namespace Sapphire.Presentation.Combat
{
    public class MonsterController : MonoBehaviour
    {
        public CombatStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        private SpriteRenderer spriteRenderer;
        private MonsterHpBar hpBar;
        private Color originalColor;
        private Vector3 originalScale;

        public void Initialize(int x, int y, int maxHp, int atk, int def)
        {
            GridX = x; GridY = y;
            Stats = new CombatStats(maxHp, 0, atk, def);
            Health = new HealthComponent(maxHp);
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            originalScale = transform.localScale;
            Health.OnDied += HandleDeath;

            hpBar = MonsterHpBar.Create(transform);
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
