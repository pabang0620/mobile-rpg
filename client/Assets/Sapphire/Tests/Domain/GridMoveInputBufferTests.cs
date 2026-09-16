using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// 2026-09-16 (movement responsiveness fix, docs/HANDOFF.md): locks in
    /// the buffering rule that fixes the "quick direction taps get dropped
    /// while a move is in progress" bug (PlayerGridController used to just
    /// discard input read while mover.IsMoving was true).
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
        public void UpdateBuffer_NotHeld_KeepsPreviousBufferedValue()
        {
            GridDirection? result = GridMoveInputBuffer.UpdateBuffer(GridDirection.Left, isDirectionHeld: false, heldDirection: GridDirection.Up);

            Assert.AreEqual(GridDirection.Left, result);
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
    }
}
