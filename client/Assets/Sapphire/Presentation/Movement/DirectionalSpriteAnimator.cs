using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Swaps a SpriteRenderer's frame based on facing/moving state, sourced
    /// from the single MageTopdownGridSheet.png sheet (3 columns: idle/walkA/
    /// walkB, 4 rows: Down/Left/Right/Up - see ArtImportConfigurator). Idle
    /// state is a static pose - only the idle column frame is ever shown
    /// while not moving (2026-09-14: grid movement made a looping idle
    /// animation unnecessary). Moving state cycles idle -> walkA -> idle ->
    /// walkB so the walk animation returns to the neutral pose between steps
    /// instead of jumping directly from one step pose to the other. Frame
    /// sprites are assigned in the inspector by the scene builder - no
    /// Domain dependency.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 8f;

        [Header("Idle column (MageTopdownGridSheet.png)")]
        [SerializeField] private Sprite idleUp;
        [SerializeField] private Sprite idleDown;
        [SerializeField] private Sprite idleLeft;
        [SerializeField] private Sprite idleRight;

        [Header("WalkA column (MageTopdownGridSheet.png)")]
        [SerializeField] private Sprite walkAUp;
        [SerializeField] private Sprite walkADown;
        [SerializeField] private Sprite walkALeft;
        [SerializeField] private Sprite walkARight;

        [Header("WalkB column (MageTopdownGridSheet.png)")]
        [SerializeField] private Sprite walkBUp;
        [SerializeField] private Sprite walkBDown;
        [SerializeField] private Sprite walkBLeft;
        [SerializeField] private Sprite walkBRight;

        private SpriteRenderer spriteRenderer;
        private GridDirection facing = GridDirection.Down;
        private bool isMoving;
        private float frameTimer;
        private int frameIndex;

        // Precomputed per-direction frame sets, built once in Awake from the
        // serialized single sprites above so Update()/ApplyFrame() never
        // allocate. Idle sets are a single frame; moving sets are the
        // idle -> walkA -> idle -> walkB cycle.
        private Sprite[] idleFramesUp;
        private Sprite[] idleFramesDown;
        private Sprite[] idleFramesLeft;
        private Sprite[] idleFramesRight;
        private Sprite[] movingFramesUp;
        private Sprite[] movingFramesDown;
        private Sprite[] movingFramesLeft;
        private Sprite[] movingFramesRight;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            idleFramesUp = new[] { idleUp };
            idleFramesDown = new[] { idleDown };
            idleFramesLeft = new[] { idleLeft };
            idleFramesRight = new[] { idleRight };

            movingFramesUp = new[] { idleUp, walkAUp, idleUp, walkBUp };
            movingFramesDown = new[] { idleDown, walkADown, idleDown, walkBDown };
            movingFramesLeft = new[] { idleLeft, walkALeft, idleLeft, walkBLeft };
            movingFramesRight = new[] { idleRight, walkARight, idleRight, walkBRight };

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
            // Idle has no animation loop - it stays locked on the idle column
            // frame (set by ApplyFrame on the SetFacing/SetMoving transition).
            // Only the moving state needs to keep advancing frames every tick.
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
                    return isMoving ? movingFramesUp : idleFramesUp;
                case GridDirection.Down:
                    return isMoving ? movingFramesDown : idleFramesDown;
                case GridDirection.Left:
                    return isMoving ? movingFramesLeft : idleFramesLeft;
                case GridDirection.Right:
                    return isMoving ? movingFramesRight : idleFramesRight;
                default:
                    return idleFramesDown;
            }
        }
    }
}
