using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sapphire.Presentation.Camera
{
    /// <summary>
    /// Follows a target transform on the XY plane, then clamps the resulting
    /// position so the camera's visible rectangle never shows area outside
    /// the map.
    ///
    /// 2026-09-15 (Phase 1, REMEDIATION_PLAN.md D1(b)): this used to delegate
    /// all pixel snapping/zoom to Unity's PixelPerfectCamera component and do
    /// no clamping at all - with that component gone (plain orthographic
    /// camera now, see SapphireSceneBuilder.BuildCamera), a free-following
    /// camera would show empty space past the map edge as soon as the player
    /// walked near a border. Map bounds are read at runtime from the Ground
    /// tilemap's cellBounds via <see cref="SetGroundTilemap"/> rather than
    /// hardcoded, so a resized map (the 24x18 map here was itself already
    /// resized once from 14x10, see SapphireSceneBuilder.MapWidth/Height)
    /// keeps working without touching this class.
    /// </summary>
    public class CameraFollowRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private Tilemap groundTilemap;

        private UnityEngine.Camera cachedCamera;
        private bool hasMapBounds;
        private float mapMinX;
        private float mapMaxX;
        private float mapMinY;
        private float mapMaxY;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Sets (or replaces) the tilemap the map's world-space bounds are
        /// read from. Bounds are (re)computed lazily on the next clamp, not
        /// here, so this is safe to call before the camera's own
        /// <see cref="UnityEngine.Camera"/> component exists yet.
        /// </summary>
        public void SetGroundTilemap(Tilemap tilemap)
        {
            groundTilemap = tilemap;
            hasMapBounds = false;
        }

        private void Awake()
        {
            cachedCamera = GetComponent<UnityEngine.Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            float smoothing = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            Vector3 next = Vector3.Lerp(transform.position, desired, smoothing);
            transform.position = ClampToMapBounds(next);
        }

        private Vector3 ClampToMapBounds(Vector3 position)
        {
            if (!EnsureMapBounds())
            {
                return position;
            }

            float halfHeight = cachedCamera != null ? cachedCamera.orthographicSize : 0f;
            float halfWidth = cachedCamera != null ? halfHeight * cachedCamera.aspect : 0f;

            float clampedX = ClampAxis(position.x, mapMinX, mapMaxX, halfWidth);
            float clampedY = ClampAxis(position.y, mapMinY, mapMaxY, halfHeight);
            return new Vector3(clampedX, clampedY, position.z);
        }

        /// <summary>
        /// Clamps a single axis so the camera's [value-halfExtent,
        /// value+halfExtent] visible span stays within [min, max]. If the
        /// visible span is wider/taller than the map on this axis
        /// (halfExtent >= map half-extent - e.g. an ultra-wide window with a
        /// small map), locking to the map's center is the only option that
        /// doesn't show empty space on one side while cutting off the other.
        /// </summary>
        private static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            float mapHalfExtent = (max - min) * 0.5f;
            if (halfExtent >= mapHalfExtent)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
        }

        private bool EnsureMapBounds()
        {
            if (hasMapBounds)
            {
                return true;
            }

            if (groundTilemap == null)
            {
                return false;
            }

            // cellBounds is in tilemap cell coordinates - CellToWorld converts
            // its min/max corners to world space. This reads the map's actual
            // populated extent (whatever VillageHubTerrainBuilder actually
            // painted), not a size constant duplicated from SapphireSceneBuilder.
            BoundsInt cellBounds = groundTilemap.cellBounds;
            Vector3 corner1 = groundTilemap.CellToWorld(cellBounds.min);
            Vector3 corner2 = groundTilemap.CellToWorld(cellBounds.max);

            mapMinX = Mathf.Min(corner1.x, corner2.x);
            mapMaxX = Mathf.Max(corner1.x, corner2.x);
            mapMinY = Mathf.Min(corner1.y, corner2.y);
            mapMaxY = Mathf.Max(corner1.y, corner2.y);
            hasMapBounds = true;
            return true;
        }
    }
}
