using UnityEngine;

namespace Sapphire
{
    /// <summary>
    /// Orthographic exploration camera helper with optional map bounds and pixel-grid snapping.
    /// Attach to the camera and set a target, or call <see cref="Configure"/> from a presenter.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PixelCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset;
        [SerializeField] bool constrainToBounds = true;
        [SerializeField] Rect worldBounds = new Rect(0, 0, 20, 11);
        [SerializeField] bool pixelSnap = true;
        [SerializeField] float pixelsPerUnit = 16f;
        [SerializeField] float smoothTime;
        Camera cameraComponent;
        Vector3 velocity;

        public Transform Target { get { return target; } set { target = value; } }
        public Rect WorldBounds { get { return worldBounds; } }

        void Awake() { cameraComponent = GetComponent<Camera>(); }
        void LateUpdate() { Follow(); }

        public void Configure(Transform followTarget, Rect bounds, bool useBounds = true)
        {
            target = followTarget;
            worldBounds = bounds;
            constrainToBounds = useBounds;
            Follow(true);
        }

        public void SetBounds(Rect bounds)
        {
            worldBounds = bounds;
            constrainToBounds = true;
        }

        public void ClearBounds() { constrainToBounds = false; }

        public void SnapNow() { Follow(true); }

        void Follow(bool immediate = false)
        {
            if (target == null) return;
            if (cameraComponent == null) cameraComponent = GetComponent<Camera>();
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (constrainToBounds) desired = ClampToBounds(desired);
            if (immediate || smoothTime <= 0f) transform.position = desired;
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (pixelSnap) transform.position = Snap(transform.position);
        }

        Vector3 ClampToBounds(Vector3 position)
        {
            if (cameraComponent == null || !cameraComponent.orthographic) return position;
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;
            float centerX = worldBounds.center.x;
            float centerY = worldBounds.center.y;
            position.x = worldBounds.width <= halfWidth * 2f ? centerX : Mathf.Clamp(position.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            position.y = worldBounds.height <= halfHeight * 2f ? centerY : Mathf.Clamp(position.y, worldBounds.yMin + halfHeight, worldBounds.yMax - halfHeight);
            return position;
        }

        Vector3 Snap(Vector3 value)
        {
            float units = Mathf.Max(1f, pixelsPerUnit);
            value.x = Mathf.Round(value.x * units) / units;
            value.y = Mathf.Round(value.y * units) / units;
            return value;
        }
    }
}
