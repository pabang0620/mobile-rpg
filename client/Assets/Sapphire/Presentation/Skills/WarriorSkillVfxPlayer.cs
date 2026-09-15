using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Cosmetic-only warrior skill VFX, mirroring SkillVfxPlayer's role and
    /// call convention (ISkillVfxPlayer) but drawing simple procedural
    /// primitive markers instead of atlas frames - no VFX atlas source art
    /// was commissioned for the warrior kit (only a body sheet and a skill
    /// icon sheet, see AGENTS.md task notes), so this generates its own 1x1
    /// white texture at runtime (same trick SkillRangeIndicator already
    /// uses) and tints/scales/fades it per skill instead of playing back
    /// hand-authored frames. Row order matches
    /// SkillCatalog.ForClass(CharacterClass.Warrior): 0=dash, 1=whirlwind,
    /// 2=shieldblock, 3=warcry, 4=groundslam. This slice has no damage
    /// system, so <see cref="ReduceDamage"/> mirrors SkillVfxPlayer.AbsorbDamage
    /// as a hook for a future resolver, not something anything calls yet.
    /// </summary>
    public sealed class WarriorSkillVfxPlayer : MonoBehaviour, ISkillVfxPlayer
    {
        private Sprite markerSprite;
        private Texture2D markerTexture;
        private GameObject shieldObject;
        private Coroutine shieldRoutine;
        private readonly List<GameObject> activeEffects = new List<GameObject>();

        public int DamageReductionPoints { get; private set; }
        public bool IsShielded => DamageReductionPoints > 0;

        private void Awake()
        {
            markerTexture = new Texture2D(1, 1);
            markerTexture.SetPixel(0, 0, Color.white);
            markerTexture.Apply();
            markerSprite = Sprite.Create(markerTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnDestroy()
        {
            if (markerTexture != null)
            {
                Destroy(markerTexture);
                markerTexture = null;
            }
        }

        /// <summary>Future damage resolver calls this before reducing HP; no HP system exists in this slice.</summary>
        public int ReduceDamage(int incoming)
        {
            int reduced = Mathf.Min(Mathf.Max(0, incoming), DamageReductionPoints);
            DamageReductionPoints -= reduced;
            if (DamageReductionPoints == 0)
            {
                ClearShield();
            }

            return Mathf.Max(0, incoming) - reduced;
        }

        public void Play(int row, GridCoord origin, GridCoord destination, GridDirection facing)
        {
            if (row < 0 || row > 4)
            {
                return;
            }

            Vector3 actorCenter = ActorCenter();

            switch (row)
            {
                case 0:
                    PlayDash(actorCenter, origin, destination);
                    break;
                case 1:
                    PlayWhirlwind(actorCenter, origin);
                    break;
                case 2:
                    PlayShieldBlock(actorCenter);
                    break;
                case 3:
                    PlayWarCry(actorCenter);
                    break;
                default:
                    PlayGroundSlam(actorCenter, origin, facing);
                    break;
            }
        }

        // Player.TryBlink already moved the actor before this plays (same
        // situation SkillVfxPlayer's row==1 teleport handles) - reconstruct
        // the departure point from the already-updated actor center.
        private void PlayDash(Vector3 actorCenter, GridCoord origin, GridCoord destination)
        {
            Vector3 originCenter = actorCenter + Point(origin) - Point(destination);
            GameObject streak = Create(new Color(0.85f, 0.85f, 0.9f, 0.8f), originCenter, 0.6f);
            StartCoroutine(MoveScaleFade(streak, originCenter, actorCenter, 0.18f));
        }

        private void PlayWhirlwind(Vector3 actorCenter, GridCoord origin)
        {
            var brown = new Color(0.85f, 0.55f, 0.2f, 0.75f);
            foreach (GridCoord tile in SkillRangeCalculator.TilesInRing(origin, 1))
            {
                Vector3 tileCenter = actorCenter + Point(tile) - Point(origin);
                GameObject marker = Create(brown, tileCenter, 0.75f);
                StartCoroutine(ScaleFade(marker, 0.35f));
            }
        }

        private void PlayShieldBlock(Vector3 actorCenter)
        {
            ClearShield();
            DamageReductionPoints = 40;
            shieldObject = Create(new Color(0.6f, 0.65f, 0.8f, 0.4f), actorCenter, 1.5f);
            shieldRoutine = StartCoroutine(PulseWhileShielded(shieldObject, 4f));
        }

        private void PlayWarCry(Vector3 actorCenter)
        {
            var orange = new Color(0.95f, 0.45f, 0.15f, 0.7f);
            GameObject wave = Create(orange, actorCenter, 0.6f);
            StartCoroutine(ExpandFade(wave, 2.4f, 0.5f));
        }

        private void PlayGroundSlam(Vector3 actorCenter, GridCoord origin, GridDirection facing)
        {
            var impactColor = new Color(0.6f, 0.4f, 0.15f, 0.8f);
            foreach (GridCoord tile in SkillRangeCalculator.TilesInFrontCone(origin, facing, 1))
            {
                Vector3 tileCenter = actorCenter + Point(tile) - Point(origin);
                GameObject marker = Create(impactColor, tileCenter, 0.85f);
                StartCoroutine(ScaleFade(marker, 0.4f));
            }
        }

        private static Vector3 Point(GridCoord cell)
        {
            WorldPoint p = GridWorldConversion.GridToWorld(cell);
            return new Vector3(p.X, p.Y, 0f);
        }

        private Vector3 ActorCenter()
        {
            SpriteRenderer actor = GetComponent<SpriteRenderer>();
            return actor != null ? actor.bounds.center : transform.position;
        }

        private GameObject Create(Color color, Vector3 position, float size)
        {
            var go = new GameObject("WarriorSkillVfx", typeof(SpriteRenderer));
            activeEffects.Add(go);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size, size, 1f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            renderer.color = color;
            renderer.sortingOrder = 200;
            return go;
        }

        private IEnumerator ScaleFade(GameObject go, float duration)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            Color startColor = renderer.color;
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }

            DestroyEffect(go);
        }

        private IEnumerator ExpandFade(GameObject go, float endScale, float duration)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            Color startColor = renderer.color;
            float startScale = go.transform.localScale.x;
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(startScale, endScale, t);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }

            DestroyEffect(go);
        }

        private IEnumerator MoveScaleFade(GameObject go, Vector3 from, Vector3 to, float duration)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            Color startColor = renderer.color;
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                go.transform.position = Vector3.Lerp(from, to, t);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }

            DestroyEffect(go);
        }

        private IEnumerator PulseWhileShielded(GameObject go, float duration)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                float pulse = 1.4f + Mathf.Sin(elapsed * 4f) * 0.1f;
                go.transform.localScale = new Vector3(pulse, pulse, 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (go == shieldObject)
            {
                DamageReductionPoints = 0;
                shieldObject = null;
                shieldRoutine = null;
            }

            _ = renderer;
            DestroyEffect(go);
        }

        private void DestroyEffect(GameObject go)
        {
            activeEffects.Remove(go);
            if (go != null)
            {
                Destroy(go);
            }
        }

        private void ClearShield()
        {
            if (shieldRoutine != null)
            {
                StopCoroutine(shieldRoutine);
            }

            shieldRoutine = null;
            if (shieldObject != null)
            {
                activeEffects.Remove(shieldObject);
                Destroy(shieldObject);
            }

            shieldObject = null;
            DamageReductionPoints = 0;
        }

        private void OnDisable()
        {
            ClearShield();
            StopAllCoroutines();
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }

            activeEffects.Clear();
        }
    }
}
