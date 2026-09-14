using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Skills
{
    /// <summary>Generated atlas animation, independent of skill hit/damage rules.</summary>
    public sealed class SkillVfxPlayer : MonoBehaviour
    {
        private SkillVfxLibrary library;
        private GameObject shieldObject;
        private Coroutine shieldRoutine;
        private readonly List<GameObject> activeEffects = new List<GameObject>();
        public int ShieldPoints { get; private set; }
        public bool IsShielded => ShieldPoints > 0;
        private void Awake() { library = Resources.Load<SkillVfxLibrary>("MageSkillVfxLibrary"); }

        /// <summary>Future damage resolver calls this before reducing HP; no HP system exists in this slice.</summary>
        public int AbsorbDamage(int incoming)
        {
            int absorbed = Mathf.Min(Mathf.Max(0, incoming), ShieldPoints);
            ShieldPoints -= absorbed;
            if (ShieldPoints == 0) ClearShield();
            return Mathf.Max(0, incoming) - absorbed;
        }

        public void Play(int row, GridCoord origin, GridCoord destination, GridDirection facing)
        {
            if (row < 0 || row > 4) return;
            Vector3 actorCenter = ActorCenter();
            // Blink moves the player before VFX playback, so reconstruct its
            // departure center from the already-updated renderer center.
            Vector3 originCenter = row == 1
                ? actorCenter + Point(origin) - Point(destination)
                : actorCenter;
            Vector3 destinationCenter = row == 1 ? actorCenter : actorCenter + Point(destination) - Point(origin);
            if (row == 0)
            {
                ClearShield(); ShieldPoints = 40;
                Vector3 shieldOffset = Vector3.down * 0.3f + Vector3.right * 0.1f;
                shieldObject = Create("ManaShield", originCenter + shieldOffset, 1.65f, 0);
                shieldObject.transform.SetParent(transform, true);
                shieldRoutine = StartCoroutine(Animate(shieldObject, row, 4f, true, Vector3.zero));
                return;
            }
            if (row == 1)
            {
                StartCoroutine(Animate(Create("TeleportDeparture", originCenter, 1.4f, 0), 1, .55f, false, Vector3.zero));
                StartCoroutine(Animate(Create("TeleportArrival", destinationCenter, 1.4f, 0), 1, .55f, false, Vector3.zero));
                return;
            }
            GridCoord direction = facing.ToOffset();
            float rotation = Mathf.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;
            if (row == 2)
            {
                float pixelSize = library != null && library.Frames != null && library.Frames.Length > 16 && library.Frames[16] != null
                    ? 1f / library.Frames[16].pixelsPerUnit
                    : 1f / 198f;
                Vector3 thunderOffset = Vector3.right * (0.2f + 0.1f * pixelSize);
                StartCoroutine(Animate(Create("Thunder3x3", originCenter + thunderOffset, 3f, 0), 2, .8f, false, Vector3.zero));
            }
            else if (row == 3)
                StartCoroutine(Animate(Create("IceSpikeSingle", originCenter, 1.25f, rotation), 3, .65f, false, new Vector3(direction.X * 5f, direction.Y * 5f, 0)));
            else
            {
                // A single connected atlas animation grows from the actor origin
                // to the full four-tile tip. The spear-row sprites use a left-edge
                // pivot, so this non-uniform scale never expands behind the caster.
                GameObject spear = Create("LightningSpear4Tiles", originCenter, 1f, rotation);
                spear.transform.localScale = new Vector3(4f, 1f, 1f);
                StartCoroutine(Animate(spear, 4, .7f, false, Vector3.zero));
            }
        }
        private static Vector3 Point(GridCoord cell)
        { WorldPoint p = GridWorldConversion.GridToWorld(cell); return new Vector3(p.X, p.Y, 0); }
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
            go.transform.rotation = Quaternion.Euler(0, 0, rotation);
            go.transform.localScale = new Vector3(size, size, 1);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 200;
            return go;
        }
        private IEnumerator Animate(GameObject go, int row, float duration, bool loop, Vector3 travel)
        {
            var sr = go.GetComponent<SpriteRenderer>(); Vector3 start = go.transform.position;
            float elapsed = 0;
            while (go != null && elapsed < duration)
            {
                int frame = loop ? (int)(elapsed * 12) % 8 : Mathf.Min(7, (int)(elapsed / duration * 8));
                if (library != null && library.Frames != null && library.Frames.Length > row * 8 + frame)
                    sr.sprite = library.Frames[row * 8 + frame];
                if (travel != Vector3.zero) go.transform.position = start + travel * Mathf.Clamp01(elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go == shieldObject) { ShieldPoints = 0; shieldObject = null; shieldRoutine = null; }
            activeEffects.Remove(go);
            if (go != null) Destroy(go);
        }
        private void ClearShield()
        {
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = null;
            if (shieldObject != null) { activeEffects.Remove(shieldObject); Destroy(shieldObject); }
            shieldObject = null; ShieldPoints = 0;
        }
        private void OnDisable()
        {
            ClearShield();
            StopAllCoroutines();
            foreach (var effect in activeEffects) if (effect != null) Destroy(effect);
            activeEffects.Clear();
        }
    }
}
