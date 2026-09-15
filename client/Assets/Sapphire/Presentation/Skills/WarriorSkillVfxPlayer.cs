using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Warrior skill VFX, mirroring SkillVfxPlayer's role, call convention
    /// (ISkillVfxPlayer) and Create/Animate helper pattern - previously this
    /// class drew procedural primitive markers (no VFX atlas art existed for
    /// the warrior kit yet); it now plays back real frames from
    /// WarriorSkillVfxLibrary the same way mage's SkillVfxPlayer does. Row
    /// order for the ISkillVfxPlayer.Play(row,...) parameter still matches
    /// SkillCatalog.ForClass(CharacterClass.Warrior): 0=dash, 1=whirlwind,
    /// 2=shieldblock, 3=warcry, 4=groundslam - this is a DIFFERENT order from
    /// WarriorSkillVfxLibrary.Frames' own row layout (see that class's doc),
    /// so each Play* method below references its library row by name
    /// (WarriorSkillVfxLibrary.XRow), never by the incoming `row` parameter.
    /// Basic attack has no SkillCatalog entry (see SkillCatalog's class doc)
    /// so it isn't reachable via Play(row,...) at all - RadialSkillMenu calls
    /// the public <see cref="PlayBasicAttack"/> method directly.
    /// </summary>
    public sealed class WarriorSkillVfxPlayer : MonoBehaviour, ISkillVfxPlayer
    {
        private WarriorSkillVfxLibrary library;
        private GameObject shieldObject;
        private Coroutine shieldRoutine;
        private readonly List<GameObject> activeEffects = new List<GameObject>();

        public int DamageReductionPoints { get; private set; }
        public bool IsShielded => DamageReductionPoints > 0;

        private void Awake()
        {
            library = Resources.Load<WarriorSkillVfxLibrary>("WarriorSkillVfxLibrary");
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
                    PlayDash(actorCenter, origin, destination, facing);
                    break;
                case 1:
                    PlayWhirlwind(actorCenter);
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

        /// <summary>
        /// Cosmetic-only basic attack ("대검베기") - no SkillCatalog entry
        /// (RadialSkillMenu's basic-attack button is separate from the 5-slot
        /// skill fan, see SkillCatalog's class doc), so this is a standalone
        /// public method rather than an ISkillVfxPlayer.Play(row,...) case.
        /// Range is WarriorCombatConstants.BasicAttackRangeTiles (2) tiles
        /// straight ahead, same left-edge-pivot elongation technique mage's
        /// LightningSpear row uses (SkillVfxPlayer.Play, row 4).
        /// </summary>
        public void PlayBasicAttack(GridDirection facing)
        {
            Vector3 actorCenter = ActorCenter();
            GridCoord direction = facing.ToOffset();
            float rotation = Mathf.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;

            GameObject slash = Create("WarriorVfx_BasicAttack", actorCenter, 1f, rotation);
            slash.transform.localScale = new Vector3(WarriorCombatConstants.BasicAttackRangeTiles, 1f, 1f);
            StartCoroutine(Animate(slash, WarriorSkillVfxLibrary.BasicAttackRow, 0.35f, false, Vector3.zero));
        }

        // Player.TryBlink already moved the actor before this plays (same
        // situation SkillVfxPlayer's row==1 teleport handles) - reconstruct
        // the departure point from the already-updated actor center. Scale
        // uses the ACTUAL tiles moved (ChebyshevDistance), not a hardcoded 3,
        // since TryBlink can stop short of the full dash range at an
        // obstacle.
        private void PlayDash(Vector3 actorCenter, GridCoord origin, GridCoord destination, GridDirection facing)
        {
            Vector3 originCenter = actorCenter + Point(origin) - Point(destination);
            GridCoord direction = facing.ToOffset();
            float rotation = Mathf.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;
            int tiles = Mathf.Max(1, SkillRangeCalculator.ChebyshevDistance(origin, destination));

            GameObject streak = Create("WarriorVfx_Dash", originCenter, 1f, rotation);
            streak.transform.localScale = new Vector3(tiles, 1f, 1f);
            StartCoroutine(Animate(streak, WarriorSkillVfxLibrary.DashRow, 0.3f, false, Vector3.zero));
        }

        // 3x3 (radius 1) area centered on the caster - uniform scale 3, per
        // spec, since WhirlwindSlash is a direction-less area effect (unlike
        // dash/basic-attack, no per-tile marker loop is needed anymore now
        // that a real atlas animation exists).
        private void PlayWhirlwind(Vector3 actorCenter)
        {
            GameObject vfx = Create("WarriorVfx_Whirlwind", actorCenter, 3f, 0f);
            StartCoroutine(Animate(vfx, WarriorSkillVfxLibrary.WhirlwindRow, 0.4f, false, Vector3.zero));
        }

        // Self-buff - scale 1.65 per mage's ManaShield reference (SkillVfxPlayer
        // row 0), loop=true for the shield's whole 4s duration. Animate's
        // "go == shieldObject" cleanup (shared with mage's pattern) resets
        // DamageReductionPoints when the coroutine ends, so no separate pulse
        // coroutine is needed anymore.
        private void PlayShieldBlock(Vector3 actorCenter)
        {
            ClearShield();
            DamageReductionPoints = 40;
            shieldObject = Create("WarriorVfx_ShieldBlock", actorCenter, 1.65f, 0f);
            shieldRoutine = StartCoroutine(Animate(shieldObject, WarriorSkillVfxLibrary.ShieldBlockRow, 4f, true, Vector3.zero));
        }

        // Self-buff, expanding shockwave ring - keeps the exact same
        // expand+fade tween this class used before real VFX art existed
        // (only the sprite-per-frame assignment is new, see
        // ExpandFadeAnimated), per spec "ExpandFade 애니메이션 기존 로직
        // 유지하며 스프라이트만 교체".
        private void PlayWarCry(Vector3 actorCenter)
        {
            GameObject wave = Create("WarriorVfx_WarCry", actorCenter, 0.6f, 0f);
            StartCoroutine(ExpandFadeAnimated(wave, WarriorSkillVfxLibrary.WarCryRow, 2.4f, 0.5f));
        }

        // Frontal 3-wide x 1-deep slam directly ahead of the caster, matching
        // SkillRangeCalculator.TilesInFrontCone(origin, facing, 1)'s
        // footprint. WarriorGroundSlamPadded.png is imported with
        // spritePixelsPerUnit = frame width (see WarriorSkillVfxImporter), so
        // a frame's own unscaled size is already 1 local unit wide (canvas
        // width -> 1 tile depth) x ~2.9 local units tall (canvas height -> 3
        // tiles wide) - center pivot, so rotating local +X to face `facing`
        // and positioning at the forward tile's center covers exactly that
        // tile's own footprint (front-to-back) plus its left/right neighbors
        // at the same forward distance, with no further scale needed.
        // Atan2-based facing rotation is new - the old primitive version drew
        // one unrotated marker per tile instead.
        private void PlayGroundSlam(Vector3 actorCenter, GridCoord origin, GridDirection facing)
        {
            GridCoord direction = facing.ToOffset();
            Vector3 position = actorCenter + Point(origin + direction) - Point(origin);
            float rotation = Mathf.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;

            GameObject slam = Create("WarriorVfx_GroundSlam", position, 1f, rotation);
            StartCoroutine(Animate(slam, WarriorSkillVfxLibrary.GroundSlamRow, 0.45f, false, Vector3.zero));
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

        private GameObject Create(string name, Vector3 position, float size, float rotation)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            activeEffects.Add(go);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            go.transform.localScale = new Vector3(size, size, 1f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 200;
            return go;
        }

        // Mirrors SkillVfxPlayer.Animate exactly (frame-cycle over `duration`,
        // optional `travel`, no fade) - see that method for the frame-index
        // math this shares.
        private IEnumerator Animate(GameObject go, int row, float duration, bool loop, Vector3 travel)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            Vector3 start = go.transform.position;
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                int frame = loop ? (int)(elapsed * 12) % 8 : Mathf.Min(7, (int)(elapsed / duration * 8));
                if (library != null && library.Frames != null && library.Frames.Length > row * 8 + frame)
                {
                    renderer.sprite = library.Frames[row * 8 + frame];
                }

                if (travel != Vector3.zero)
                {
                    go.transform.position = start + travel * Mathf.Clamp01(elapsed / duration);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (go == shieldObject)
            {
                DamageReductionPoints = 0;
                shieldObject = null;
                shieldRoutine = null;
            }

            DestroyEffect(go);
        }

        // WarCry's expand+fade tween, unchanged from the pre-atlas version,
        // plus frame-per-tick sprite assignment (the only new part).
        private IEnumerator ExpandFadeAnimated(GameObject go, int row, float endScale, float duration)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            Color startColor = renderer.color;
            float startScale = go.transform.localScale.x;
            float elapsed = 0f;
            while (go != null && elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(startScale, endScale, t);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));

                int frame = Mathf.Min(7, (int)(t * 8));
                if (library != null && library.Frames != null && library.Frames.Length > row * 8 + frame)
                {
                    renderer.sprite = library.Frames[row * 8 + frame];
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

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
