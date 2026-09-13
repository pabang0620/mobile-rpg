using UnityEngine;

namespace Sapphire
{
    /// <summary>
    /// Visual-only four-direction, two-frame SD character view. Feed it the facade's move vector
    /// and position; it never reads input or moves the simulated player itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SdCharacterAnimator : MonoBehaviour
    {
        [SerializeField] string sortingLayer = "Default";
        [SerializeField] int baseSortingOrder = 100;
        [SerializeField] float framesPerSecond = 8f;
        [SerializeField] Color tint = Color.white;
        SpriteRenderer renderer;
        ExplorationFacing facing = ExplorationFacing.Down;
        bool moving;
        float animationTime;

        public ExplorationFacing Facing { get { return facing; } }
        public bool IsMoving { get { return moving; } }
        public SpriteRenderer Renderer { get { EnsureRenderer(); return renderer; } }

        void Awake() { EnsureRenderer(); RefreshSprite(); }

        void Update()
        {
            if (moving) animationTime += Time.deltaTime;
            else animationTime = 0f;
            RefreshSprite();
        }

        /// <summary>Sets the directional visual state from a world-space movement vector.</summary>
        public void SetMoveState(Vector2 direction, bool isMoving)
        {
            if (direction.sqrMagnitude > .0001f) facing = FacingFor(direction);
            moving = isMoving;
            if (!moving) animationTime = 0f;
            RefreshSprite();
        }

        public void SetFacing(ExplorationFacing value)
        {
            facing = value;
            RefreshSprite();
        }

        /// <summary>Applies an interpolated render position without changing any gameplay state.</summary>
        public void SetWorldPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            UpdateSortingOrder();
        }

        public void SetTint(Color value)
        {
            tint = value;
            EnsureRenderer();
            renderer.color = tint;
        }

        public void SetSorting(string layerName, int order)
        {
            sortingLayer = layerName;
            baseSortingOrder = order;
            UpdateSortingOrder();
        }

        public static ExplorationFacing FacingFor(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x < 0f ? ExplorationFacing.Left : ExplorationFacing.Right;
            return direction.y < 0f ? ExplorationFacing.Down : ExplorationFacing.Up;
        }

        void EnsureRenderer()
        {
            if (renderer != null) return;
            renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.color = tint;
        }

        void RefreshSprite()
        {
            EnsureRenderer();
            int frame = moving ? Mathf.FloorToInt(animationTime * Mathf.Max(.01f, framesPerSecond)) & 1 : 0;
            renderer.sprite = PixelSpriteLibrary.Walker(facing, frame);
            renderer.color = tint;
            UpdateSortingOrder();
        }

        void UpdateSortingOrder()
        {
            if (renderer == null) return;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = baseSortingOrder - Mathf.RoundToInt(transform.position.y * 16f);
        }
    }
}
