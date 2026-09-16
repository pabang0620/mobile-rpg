using System.Collections.Generic;

namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Raw per-frame snapshot of which of the 4 grid directions are physically
    /// held, independent of any priority between them. Presentation builds
    /// this from Keyboard.current / the virtual pad; Domain only ever sees
    /// this plain struct, never UnityEngine input types.
    /// </summary>
    public readonly struct HeldDirections
    {
        public readonly bool Up;
        public readonly bool Down;
        public readonly bool Left;
        public readonly bool Right;

        public static readonly HeldDirections None = default;

        public HeldDirections(bool up, bool down, bool left, bool right)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }

        /// <summary>A snapshot where at most one direction (or none) is held - what a
        /// single-axis source like the virtual movement pad always reports.</summary>
        public static HeldDirections Only(GridDirection? direction) => new HeldDirections(
            up: direction == GridDirection.Up,
            down: direction == GridDirection.Down,
            left: direction == GridDirection.Left,
            right: direction == GridDirection.Right);

        public bool IsHeld(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up: return Up;
                case GridDirection.Down: return Down;
                case GridDirection.Left: return Left;
                case GridDirection.Right: return Right;
                default: return false;
            }
        }
    }

    /// <summary>
    /// Pure input-priority rule for grid movement, extracted out of
    /// Presentation.Movement.PlayerGridController.Update so it is
    /// unit-testable without a MonoBehaviour/Keyboard dependency.
    ///
    /// 2026-09-16, 3rd revision (bug report: holding Down and tapping Right
    /// gets ignored most of the time, only occasionally registers): the
    /// previous two revisions of this file only ever resolved "the" held
    /// direction one level up, in PlayerInputReader.TryGetHeldDirection,
    /// using a FIXED if/else priority chain (always Up, then Down, then
    /// Left, then Right - see the removed method's old diff). That meant
    /// holding Down while tapping Right could never win, because the chain
    /// checked Down before Right on every single frame regardless of which
    /// key was pressed more recently - Right only "occasionally" got through
    /// on frames where Down had not yet been read as held for that exact
    /// poll (input-timing luck), never because the game correctly recognized
    /// Right as the newer press.
    ///
    /// Fix: track a priority STACK of currently-held directions ordered by
    /// recency (index 0 = highest priority = the direction that started
    /// being held most recently and is still down). Every frame:
    /// - any tracked direction that is no longer held is dropped, wherever
    ///   it was in the stack (this is what makes a release take effect the
    ///   instant the key comes up - same guarantee the previous revision's
    ///   UpdateBuffer had, now expressed as "not in the held set anymore"
    ///   instead of "buffer cleared to null").
    /// - any direction that is held this frame but was NOT tracked last
    ///   frame (a fresh press) is inserted at the front, ahead of every
    ///   direction that has been held for longer - so a brand new press
    ///   always outranks an older hold, no matter which physical key it is.
    /// The direction actually used to move is always stack[0] (see
    /// <see cref="TopDirection"/>); nothing is queued past release, so a tap
    /// that starts and ends entirely while GridMover.IsMoving blocks input is
    /// still dropped (same deliberate tradeoff as the previous revision,
    /// which fixed a worse bug: a stale direction firing an extra move after
    /// release - see docs/HANDOFF.md 2026-09-16 entries).
    /// </summary>
    public static class GridMoveInputBuffer
    {
        private static readonly GridDirection[] AllDirections =
        {
            GridDirection.Up, GridDirection.Down, GridDirection.Left, GridDirection.Right
        };

        /// <summary>
        /// Call once per frame, every frame (including frames where the mover
        /// is still busy), with this frame's raw held state. Returns a NEW
        /// list for the next frame's priority stack - <paramref
        /// name="previousStack"/> is never mutated (immutable-style, so a
        /// caller that stored the old reference elsewhere is unaffected).
        /// Index 0 of the result is the current top-priority direction.
        /// </summary>
        public static List<GridDirection> UpdatePriorityStack(IReadOnlyList<GridDirection> previousStack, HeldDirections held)
        {
            var next = new List<GridDirection>();

            if (previousStack != null)
            {
                foreach (GridDirection direction in previousStack)
                {
                    if (held.IsHeld(direction))
                    {
                        next.Add(direction);
                    }
                }
            }

            foreach (GridDirection direction in AllDirections)
            {
                if (held.IsHeld(direction) && !next.Contains(direction))
                {
                    next.Insert(0, direction);
                }
            }

            return next;
        }

        /// <summary>The direction to actually move in, or null if nothing is held.</summary>
        public static GridDirection? TopDirection(IReadOnlyList<GridDirection> stack)
            => stack != null && stack.Count > 0 ? stack[0] : (GridDirection?)null;
    }
}
