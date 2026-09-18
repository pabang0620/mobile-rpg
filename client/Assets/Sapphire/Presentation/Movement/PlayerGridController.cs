using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Owns a Domain GridMover for the player, forwards held input into
    /// TryBeginMove and drives the move/sprite animators from the result.
    /// New input is naturally ignored while moving because GridMover itself
    /// returns AlreadyMoving.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerGridController : MonoBehaviour
    {
        [SerializeField] private GridMoveAnimator moveAnimator;
        [SerializeField] private DirectionalSpriteAnimator spriteAnimator;

        private PlayerInputReader inputReader;
        private GridMover mover;
        private GridMap map;

        // Priority stack of currently-held directions, most-recently-pressed first
        // (index 0). Updated every frame, even while a move is in progress and
        // Update returns early below, so the stack accurately reflects press order
        // the instant the mover frees up. See GridMoveInputBuffer's doc (2026-09-16,
        // 3rd revision) for why a stack replaced the single-direction buffer: a
        // fixed if/else priority chain (the previous PlayerInputReader) meant a
        // direction held first always won, so tapping a new direction while another
        // was already held could be silently ignored.
        private List<GridDirection> directionStack = new List<GridDirection>();

        public GridMover Mover => mover;

        /// <summary>Exposed so the skill bar can trigger the "질주" speed-boost skill directly.</summary>
        public GridMoveAnimator MoveAnimator => moveAnimator;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        /// <summary>
        /// Places the mover at spawnCoord and snaps the transform to match.
        /// Must be called once by the composition root before Update runs.
        /// </summary>
        public void Initialize(GridMap gridMap, GridCoord spawnCoord)
        {
            map = gridMap;
            mover = new GridMover(spawnCoord);

            WorldPoint spawnWorld = GridWorldConversion.GridToWorld(spawnCoord);
            transform.position = new Vector3(spawnWorld.X, spawnWorld.Y, transform.position.z);

            spriteAnimator?.SetFacing(mover.Facing);
        }

        public bool TryBlink(int rangeTiles)
        {
            if (mover == null || map == null || mover.TryBlink(rangeTiles, map) != MoveResult.Started) return false;
            WorldPoint destination = GridWorldConversion.GridToWorld(mover.Position);
            transform.position = new Vector3(destination.X, destination.Y, transform.position.z);
            directionStack = new List<GridDirection>();
            spriteAnimator?.SetMoving(false);
            return true;
        }

        private void Update()
        {
            HeldDirections held = inputReader.GetHeldDirections();

            // Snapshot the stack as it was BEFORE this frame's update, so we can
            // tell below whether the resolved direction is a brand new press or a
            // continuation of a hold that already existed last frame.
            List<GridDirection> stackBeforeThisFrame = directionStack;

            // Refresh the priority stack every frame, even while a move is in
            // progress and everything below returns early - this is what lets a
            // fresh tap on a different direction immediately outrank an
            // already-held one the instant the mover frees up (see
            // GridMoveInputBuffer's doc for the priority bug this fixes).
            directionStack = GridMoveInputBuffer.UpdatePriorityStack(directionStack, held);

            if (mover == null || map == null || mover.IsMoving)
            {
                return;
            }

            GridDirection? moveDirection = GridMoveInputBuffer.TopDirection(directionStack);
            if (!moveDirection.HasValue)
            {
                return;
            }

            GridDirection direction = moveDirection.Value;

            // Was this direction already the (or a) held direction last frame? If
            // so this move continues an existing hold; if not, it is a freshly
            // pressed tap (even if some OTHER direction was held before) and gets
            // the step-pause cadence below instead of chaining seamlessly.
            bool wasAlreadyHeldLastFrame = stackBeforeThisFrame.Contains(direction);

            GridCoord previousPosition = mover.Position;
            MoveResult result = mover.TryBeginMove(direction, map);

            spriteAnimator?.SetFacing(mover.Facing);

            if (result != MoveResult.Started)
            {
                return;
            }

            GridCoord destination = previousPosition + direction.ToOffset();
            WorldPoint from = GridWorldConversion.GridToWorld(previousPosition);
            WorldPoint to = GridWorldConversion.GridToWorld(destination);

            spriteAnimator?.SetMoving(true);

            // isContinuousHold is re-evaluated at move-COMPLETION time (see
            // GridMoveAnimator), not captured once here at move-start - this closes
            // the race where a key released mid-animation still had its start-time
            // "continuous" flag baked in, skipping the settle pause and letting the
            // mover free up with no cushion right as an occasional extra tile could
            // slip in (docs/HANDOFF.md 2026-09-16 entry).
            moveAnimator.PlayMove(
                transform, from, to,
                isContinuousHold: () => wasAlreadyHeldLastFrame && inputReader.GetHeldDirections().IsHeld(direction),
                onComplete: OnMoveAnimationComplete);
        }

        private void OnMoveAnimationComplete()
        {
            mover.CompleteMove();
            spriteAnimator?.SetMoving(false);
        }
    }
}
