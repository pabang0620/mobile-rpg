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

        // Tracks the held direction from the previous frame, updated every frame
        // (even while a move is in progress and Update returns early below) so that
        // continuity survives the frames where mover.IsMoving blocks input handling.
        // null means "no direction was held last frame".
        private GridDirection? previousHeldDirection;

        // 2026-09-16 (movement responsiveness fix, docs/HANDOFF.md): remembers
        // the last direction held while mover.IsMoving was blocking input, so
        // a quick tap-then-release that happens entirely inside the current
        // move's animation window still fires the instant that window clears
        // instead of being silently dropped. See GridMoveInputBuffer's doc.
        private GridDirection? bufferedDirection;

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
            previousHeldDirection = null;
            bufferedDirection = null;
            spriteAnimator?.SetMoving(false);
            return true;
        }

        private void Update()
        {
            bool isDirectionHeld = inputReader.TryGetHeldDirection(out GridDirection direction);

            // Refresh the input buffer every frame, even while a move is in
            // progress and everything below returns early - this is what
            // lets a tap-then-release that happens entirely inside the
            // current move's blocked window still register once that window
            // clears (see GridMoveInputBuffer's doc for the full bug this fixes).
            bufferedDirection = GridMoveInputBuffer.UpdateBuffer(bufferedDirection, isDirectionHeld, direction);

            // Same direction key held on both this frame and the previous one (tracked
            // unconditionally, so it still counts across the frames a move blocks input
            // handling below) means this is a continuous hold, not a fresh key press.
            bool isContinuousHold = isDirectionHeld
                && previousHeldDirection.HasValue
                && previousHeldDirection.Value == direction;

            previousHeldDirection = isDirectionHeld ? direction : (GridDirection?)null;

            if (mover == null || map == null || mover.IsMoving)
            {
                return;
            }

            GridDirection? moveDirection = GridMoveInputBuffer.ResolveMoveDirection(isDirectionHeld, direction, bufferedDirection);
            if (!moveDirection.HasValue)
            {
                return;
            }

            // Consume the buffer now - a queued tap fires exactly once, and a
            // released key won't keep re-firing the same stale press forever.
            bufferedDirection = null;

            GridCoord previousPosition = mover.Position;
            MoveResult result = mover.TryBeginMove(moveDirection.Value, map);

            spriteAnimator?.SetFacing(mover.Facing);

            if (result != MoveResult.Started)
            {
                return;
            }

            GridCoord destination = previousPosition + moveDirection.Value.ToOffset();
            WorldPoint from = GridWorldConversion.GridToWorld(previousPosition);
            WorldPoint to = GridWorldConversion.GridToWorld(destination);

            spriteAnimator?.SetMoving(true);
            moveAnimator.PlayMove(transform, from, to, isContinuousHold, OnMoveAnimationComplete);
        }

        private void OnMoveAnimationComplete()
        {
            mover.CompleteMove();
            spriteAnimator?.SetMoving(false);
        }
    }
}
