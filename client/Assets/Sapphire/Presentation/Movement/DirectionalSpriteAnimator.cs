using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Swaps a SpriteRenderer's frame based on facing/moving state. Idle
    /// state is a static pose - only frame 0 of the idle set is ever shown
    /// (2026-09-14: grid movement made a looping idle animation unnecessary,
    /// so the idle breathing frame is no longer cycled). Moving state still
    /// loops the 3-frame walk cycle. Frame arrays are assigned in the
    /// inspector by the scene builder - no Domain dependency.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 8f;

        [Header("Idle (MageIdleDirectional.png, 2 frames per direction - only frame 0 used)")]
        [SerializeField] private Sprite[] idleUp = new Sprite[0];
        [SerializeField] private Sprite[] idleDown = new Sprite[0];
        [SerializeField] private Sprite[] idleLeft = new Sprite[0];
        [SerializeField] private Sprite[] idleRight = new Sprite[0];

        [Header("Walk (MageWalk4x3-v2.png, 3 frames per direction)")]
        [SerializeField] private Sprite[] walkUp = new Sprite[0];
        [SerializeField] private Sprite[] walkDown = new Sprite[0];
        [SerializeField] private Sprite[] walkLeft = new Sprite[0];
        [SerializeField] private Sprite[] walkRight = new Sprite[0];

        private SpriteRenderer spriteRenderer;
        private GridDirection facing = GridDirection.Down;
        private bool isMoving;
        private float frameTimer;
        private int frameIndex;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyFrame();
        }

        public void SetFacing(GridDirection direction)
        {
            if (facing == direction)
            {
                return;
            }

            facing = direction;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        public void SetMoving(bool moving)
        {
            if (isMoving == moving)
            {
                return;
            }

            isMoving = moving;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        private void Update()
        {
            // Idle has no animation loop - it stays locked on frame 0 (set by
            // ApplyFrame on the SetFacing/SetMoving transition). Only the
            // walk cycle needs to keep advancing frames every tick.
            if (!isMoving)
            {
                return;
            }

            Sprite[] frames = CurrentFrameSet();
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.01f);
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Sprite[] frames = CurrentFrameSet();
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = frames[frameIndex % frames.Length];
        }

        private Sprite[] CurrentFrameSet()
        {
            switch (facing)
            {
                case GridDirection.Up:
                    return isMoving ? walkUp : idleUp;
                case GridDirection.Down:
                    return isMoving ? walkDown : idleDown;
                case GridDirection.Left:
                    return isMoving ? walkLeft : idleLeft;
                case GridDirection.Right:
                    return isMoving ? walkRight : idleRight;
                default:
                    return idleDown;
            }
        }
    }
}
