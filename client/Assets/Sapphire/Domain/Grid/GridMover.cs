using System;

namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Owns a single actor's grid position/facing/move-in-progress state.
    /// Presentation adapters drive CompleteMove once the visual tween finishes.
    /// </summary>
    public class GridMover
    {
        private GridCoord targetPosition;

        public GridMover(GridCoord startPosition, GridDirection startFacing = GridDirection.Down)
        {
            Position = startPosition;
            Facing = startFacing;
            IsMoving = false;
            targetPosition = startPosition;
        }

        public GridCoord Position { get; private set; }
        public GridDirection Facing { get; private set; }
        public bool IsMoving { get; private set; }

        public MoveResult TryBeginMove(GridDirection direction, GridMap map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (IsMoving)
            {
                return MoveResult.AlreadyMoving;
            }

            Facing = direction;

            GridCoord destination = Position + direction.ToOffset();
            if (!map.IsWalkable(destination))
            {
                return MoveResult.BlockedByObstacle;
            }

            targetPosition = destination;
            IsMoving = true;
            return MoveResult.Started;
        }

        /// <summary>Instant step-by-step blink; obstacles and bounds stop the path.</summary>
        public MoveResult TryBlink(int rangeTiles, GridMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (IsMoving) return MoveResult.AlreadyMoving;
            GridCoord destination = Position;
            for (int i = 0; i < Math.Max(0, rangeTiles); i++)
            {
                GridCoord next = destination + Facing.ToOffset();
                if (!map.IsWalkable(next)) break;
                destination = next;
            }
            if (destination == Position) return MoveResult.BlockedByObstacle;
            Position = targetPosition = destination;
            return MoveResult.Started;
        }

        public void CompleteMove()
        {
            if (!IsMoving)
            {
                return;
            }

            Position = targetPosition;
            IsMoving = false;
        }
    }
}
