namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Pure input-buffering rule for grid movement, extracted out of
    /// Presentation.Movement.PlayerGridController.Update so it is
    /// unit-testable without a MonoBehaviour/Keyboard dependency
    /// (2026-09-16, movement responsiveness fix, docs/HANDOFF.md).
    ///
    /// Bug this originally fixed: while GridMover.IsMoving is true,
    /// PlayerGridController still polls input every frame but previously
    /// discarded it entirely - a direction key tapped and released wholly
    /// inside the current move's blocked window (~move duration + step
    /// pause) was silently lost. Rapid alternating taps ("옆 아래 옆 아래")
    /// felt unresponsive as a result.
    ///
    /// 2026-09-16 correction: the first version of <see cref="UpdateBuffer"/>
    /// kept the last held direction alive across every frame where no key was
    /// held, instead of clearing it the instant the key came up. That meant a
    /// player who simply held a direction and released it WHILE the current
    /// move's animation was still playing (the completely normal "release to
    /// stop" case, not a deliberate queued tap) had that stale direction sit
    /// in the buffer and fire one extra move the moment the mover became free
    /// - visible as ~0.5s / 1-2 extra tiles of movement after release. Because
    /// nothing in the per-frame state can distinguish "a fresh tap queued
    /// during the blocked window" from "was held since before, then released
    /// mid-window", the buffer no longer remembers anything past release: it
    /// only ever reflects the direction held on the current frame (see
    /// <see cref="UpdateBuffer"/>), so releasing a key stops the next move
    /// from starting immediately, at the cost of no longer catching taps that
    /// happen to end inside the blocked window.
    /// </summary>
    public static class GridMoveInputBuffer
    {
        /// <summary>
        /// Call once per frame, every frame (including frames where the mover
        /// is still busy), with this frame's held state. Returns the value the
        /// buffer should hold going into next frame: the currently held
        /// direction while a key is down, otherwise null - the buffer never
        /// outlives the frame a key was released on, so <paramref
        /// name="currentBuffer"/> is intentionally ignored once the key is up.
        /// </summary>
        public static GridDirection? UpdateBuffer(GridDirection? currentBuffer, bool isDirectionHeld, GridDirection heldDirection)
        {
            return isDirectionHeld ? heldDirection : null;
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
