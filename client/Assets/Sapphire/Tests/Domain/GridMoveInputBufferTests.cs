using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// 2026-09-16 (movement responsiveness fix, docs/HANDOFF.md): locks in
    /// the buffering rule that fixes the "quick direction taps get dropped
    /// while a move is in progress" bug (PlayerGridController used to just
    /// discard input read while mover.IsMoving was true).
    ///
    /// 2026-09-16 correction (same day): the original UpdateBuffer kept a
    /// stale direction alive across every frame the key was up, which caused
    /// a real bug - releasing a held key WHILE a move was still animating
    /// fired one extra move once the mover freed up (reported as "releases
    /// but keeps going ~0.5s longer"). UpdateBuffer_NotHeld_ClearsBufferImmediately
    /// below replaces the old (buggy) UpdateBuffer_NotHeld_KeepsPreviousBufferedValue
    /// assertion to lock in the corrected contract instead.
    /// </summary>
    public class GridMoveInputBufferTests
    {
        [Test]
        public void UpdateBuffer_DirectionHeld_ReturnsHeldDirectionRegardlessOfPreviousBuffer()
        {
            GridDirection? result = GridMoveInputBuffer.UpdateBuffer(GridDirection.Up, isDirectionHeld: true, heldDirection: GridDirection.Down);

            Assert.AreEqual(GridDirection.Down, result);
        }

        [Test]
        public void UpdateBuffer_NotHeld_ClearsBufferImmediately()
        {
            // Regression guard for the stop-on-release bug: once the key is
            // up, whatever was previously buffered must NOT survive into the
            // next frame, otherwise a release that happens mid-animation
            // fires one extra move once the mover frees up.
            GridDirection? result = GridMoveInputBuffer.UpdateBuffer(GridDirection.Left, isDirectionHeld: false, heldDirection: GridDirection.Up);

            Assert.IsNull(result);
        }

        [Test]
        public void UpdateBuffer_NotHeldAndNothingBufferedYet_StaysNull()
        {
            GridDirection? result = GridMoveInputBuffer.UpdateBuffer(null, isDirectionHeld: false, heldDirection: GridDirection.Right);

            Assert.IsNull(result);
        }

        [Test]
        public void ResolveMoveDirection_DirectionHeldRightNow_PrefersHeldOverBuffer()
        {
            // Regression guard: a live held key must win over a stale buffer
            // entry, so a player continuously holding one direction is never
            // overridden by whatever was queued right before.
            GridDirection? result = GridMoveInputBuffer.ResolveMoveDirection(isDirectionHeld: true, heldDirection: GridDirection.Right, bufferedDirection: GridDirection.Left);

            Assert.AreEqual(GridDirection.Right, result);
        }

        [Test]
        public void ResolveMoveDirection_NothingHeldButBuffered_FallsBackToBufferedDirection()
        {
            // This is the exact case the bug report described: the key that
            // was tapped during the blocked window is already released by
            // the time the mover frees up, but the queued tap must still fire.
            GridDirection? result = GridMoveInputBuffer.ResolveMoveDirection(isDirectionHeld: false, heldDirection: default, bufferedDirection: GridDirection.Down);

            Assert.AreEqual(GridDirection.Down, result);
        }

        [Test]
        public void ResolveMoveDirection_NothingHeldAndNothingBuffered_ReturnsNull()
        {
            GridDirection? result = GridMoveInputBuffer.ResolveMoveDirection(isDirectionHeld: false, heldDirection: default, bufferedDirection: null);

            Assert.IsNull(result);
        }

        [Test]
        public void PressedThenReleasedBeforeMoveEnds_NoExtraMoveWhenMoverFrees()
        {
            // Exact scenario from the bug report: player holds Up (buffer
            // tracks it every frame), releases Up while the current move is
            // still animating (mover.IsMoving == true, several frames before
            // the move actually completes), then the mover frees up on a
            // later frame with the key still up. The corrected UpdateBuffer
            // must have already dropped the stale direction on the release
            // frame, so ResolveMoveDirection sees nothing buffered and
            // naturally stops at the tile already in progress - no extra move.
            GridDirection? buffer = GridMoveInputBuffer.UpdateBuffer(null, isDirectionHeld: true, heldDirection: GridDirection.Up);
            Assert.AreEqual(GridDirection.Up, buffer, "sanity: buffer should track the held direction while pressed");

            // Key released mid-animation (mover.IsMoving still true here in
            // the real caller, but UpdateBuffer itself does not need that -
            // it only cares about this frame's held state).
            buffer = GridMoveInputBuffer.UpdateBuffer(buffer, isDirectionHeld: false, heldDirection: default);
            Assert.IsNull(buffer, "buffer must clear the instant the key comes up, even mid-animation");

            // A few more blocked frames tick by with the key still up - buffer
            // stays cleared throughout.
            buffer = GridMoveInputBuffer.UpdateBuffer(buffer, isDirectionHeld: false, heldDirection: default);
            Assert.IsNull(buffer);

            // Mover finally frees up on this frame; key is still not held.
            GridDirection? moveDirection = GridMoveInputBuffer.ResolveMoveDirection(isDirectionHeld: false, heldDirection: default, bufferedDirection: buffer);

            Assert.IsNull(moveDirection, "no extra move should start once the key was released before the current move finished");
        }
    }
}
