namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Pure input-buffering rule for grid movement, extracted out of
    /// Presentation.Movement.PlayerGridController.Update so it is
    /// unit-testable without a MonoBehaviour/Keyboard dependency
    /// (2026-09-16, movement responsiveness fix, docs/HANDOFF.md).
    ///
    /// Bug this fixes: while GridMover.IsMoving is true, PlayerGridController
    /// still polls input every frame but previously discarded it entirely -
    /// a direction key tapped and released wholly inside the current move's
    /// blocked window (~move duration + step pause) was silently lost,
    /// because by the time Update finally acted on input again (the frame
    /// IsMoving turns false), the key was already back up. Rapid alternating
    /// taps ("옆 아래 옆 아래") felt unresponsive as a result - the player had
    /// to keep a key held down until that exact frame for the next step to
    /// register.
    ///
    /// Fix: remember the most recently held direction every frame
    /// (<see cref="UpdateBuffer"/>), and once the mover is free again, prefer
    /// whatever is held RIGHT NOW but fall back to that remembered value
    /// (<see cref="ResolveMoveDirection"/>) instead of requiring the key to
    /// still be down at that exact frame - the standard input-buffer pattern
    /// grid-snap movement games (Pokemon-style) use.
    /// </summary>
    public static class GridMoveInputBuffer
    {
        /// <summary>
        /// Call once per frame, every frame (including frames where the mover
        /// is still busy), with this frame's held state. Returns the value the
        /// buffer should hold going into next frame: the freshly held
        /// direction while a key is down, otherwise whatever was already
        /// buffered (unchanged - a buffered press survives frames with no key
        /// held until it gets consumed by <see cref="ResolveMoveDirection"/>).
        /// </summary>
        public static GridDirection? UpdateBuffer(GridDirection? currentBuffer, bool isDirectionHeld, GridDirection heldDirection)
        {
            return isDirectionHeld ? heldDirection : currentBuffer;
        }

        /// <summary>
        /// The direction to actually start moving in, called only once the
        /// mover is free (mover.IsMoving == false). A direction held RIGHT
        /// NOW always takes priority over the buffer (so a player who is
        /// simply holding one key continues seamlessly); otherwise falls back
        /// to the buffered direction (a step queued during the last move's
        /// blocked window); otherwise null (nothing to do). Callers must
        /// clear the buffer after consuming a non-null result here so a
        /// single queued tap fires exactly once.
        /// </summary>
        public static GridDirection? ResolveMoveDirection(bool isDirectionHeld, GridDirection heldDirection, GridDirection? bufferedDirection)
        {
            return isDirectionHeld ? heldDirection : bufferedDirection;
        }
    }
}
