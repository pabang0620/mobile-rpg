using System.Collections.Generic;
using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// 2026-09-16, 3rd revision: locks in the priority-stack rule that
    /// replaced PlayerInputReader's old fixed if/else priority chain
    /// (always Up > Down > Left > Right). Bug report: holding Down and
    /// tapping Right got ignored almost every time, only occasionally
    /// registering. Root cause was the fixed chain always checking Down
    /// before Right, regardless of which key was actually pressed more
    /// recently - see GridMoveInputBuffer's doc for the full writeup.
    /// </summary>
    public class GridMoveInputBufferTests
    {
        private static List<GridDirection> Stack(params GridDirection[] directions) => new List<GridDirection>(directions);

        [Test]
        public void UpdatePriorityStack_FreshPressWhileAnotherHeld_NewPressGoesToFront()
        {
            // Exact bug report scenario: Down already held (tracked from a
            // previous frame), Right newly pressed this frame. Right must
            // immediately outrank Down, not be ignored.
            List<GridDirection> previous = Stack(GridDirection.Down);
            HeldDirections held = new HeldDirections(up: false, down: true, left: false, right: true);

            List<GridDirection> result = GridMoveInputBuffer.UpdatePriorityStack(previous, held);

            Assert.AreEqual(GridDirection.Right, GridMoveInputBuffer.TopDirection(result));
            CollectionAssert.AreEqual(new[] { GridDirection.Right, GridDirection.Down }, result);
        }

        [Test]
        public void UpdatePriorityStack_NewlyReleasedDirectionFallsBackToStillHeldDirection()
        {
            // Continuation of the scenario above: Right (the tap) is released
            // one frame later while Down is still held - Down must become top
            // priority again immediately.
            List<GridDirection> previous = Stack(GridDirection.Right, GridDirection.Down);
            HeldDirections held = new HeldDirections(up: false, down: true, left: false, right: false);

            List<GridDirection> result = GridMoveInputBuffer.UpdatePriorityStack(previous, held);

            Assert.AreEqual(GridDirection.Down, GridMoveInputBuffer.TopDirection(result));
            CollectionAssert.AreEqual(new[] { GridDirection.Down }, result);
        }

        [Test]
        public void UpdatePriorityStack_NothingHeld_ResultIsEmptyAndTopIsNull()
        {
            List<GridDirection> previous = Stack(GridDirection.Down);
            HeldDirections held = HeldDirections.None;

            List<GridDirection> result = GridMoveInputBuffer.UpdatePriorityStack(previous, held);

            CollectionAssert.IsEmpty(result);
            Assert.IsNull(GridMoveInputBuffer.TopDirection(result));
        }

        [Test]
        public void UpdatePriorityStack_NullPreviousStackTreatedAsEmpty()
        {
            HeldDirections held = new HeldDirections(up: true, down: false, left: false, right: false);

            List<GridDirection> result = GridMoveInputBuffer.UpdatePriorityStack(null, held);

            CollectionAssert.AreEqual(new[] { GridDirection.Up }, result);
        }

        [Test]
        public void UpdatePriorityStack_SameDirectionHeldAcrossFrames_StaysAtSameRank()
        {
            // A direction that was already tracked and is still held on this
            // frame must not be re-inserted at the front - it keeps its
            // existing rank (this is what makes "still held" distinguishable
            // from "freshly pressed" for the isContinuousHold decision).
            List<GridDirection> previous = Stack(GridDirection.Right, GridDirection.Down);
            HeldDirections held = new HeldDirections(up: false, down: true, left: false, right: true);

            List<GridDirection> result = GridMoveInputBuffer.UpdatePriorityStack(previous, held);

            CollectionAssert.AreEqual(new[] { GridDirection.Right, GridDirection.Down }, result);
        }

        [Test]
        public void TopDirection_EmptyStack_ReturnsNull()
        {
            Assert.IsNull(GridMoveInputBuffer.TopDirection(new List<GridDirection>()));
            Assert.IsNull(GridMoveInputBuffer.TopDirection(null));
        }

        [Test]
        public void TopDirection_NonEmptyStack_ReturnsFrontElement()
        {
            Assert.AreEqual(GridDirection.Left, GridMoveInputBuffer.TopDirection(Stack(GridDirection.Left, GridDirection.Up)));
        }

        [Test]
        public void HeldDirections_IsHeld_ReflectsEachAxisIndependently()
        {
            HeldDirections held = new HeldDirections(up: true, down: false, left: true, right: false);

            Assert.IsTrue(held.IsHeld(GridDirection.Up));
            Assert.IsFalse(held.IsHeld(GridDirection.Down));
            Assert.IsTrue(held.IsHeld(GridDirection.Left));
            Assert.IsFalse(held.IsHeld(GridDirection.Right));
        }

        [Test]
        public void HeldDirections_Only_ReportsExactlyThatSingleDirection()
        {
            HeldDirections held = HeldDirections.Only(GridDirection.Down);

            Assert.IsFalse(held.IsHeld(GridDirection.Up));
            Assert.IsTrue(held.IsHeld(GridDirection.Down));
            Assert.IsFalse(held.IsHeld(GridDirection.Left));
            Assert.IsFalse(held.IsHeld(GridDirection.Right));
        }

        [Test]
        public void HeldDirections_Only_NullDirection_HoldsNothing()
        {
            HeldDirections held = HeldDirections.Only(null);

            Assert.IsFalse(held.IsHeld(GridDirection.Up));
            Assert.IsFalse(held.IsHeld(GridDirection.Down));
            Assert.IsFalse(held.IsHeld(GridDirection.Left));
            Assert.IsFalse(held.IsHeld(GridDirection.Right));
        }

        [Test]
        public void FullScenario_HoldDownTapRightReleaseRight_MatchesBugReportExpectation()
        {
            // (a) Down held for a few frames.
            List<GridDirection> stack = GridMoveInputBuffer.UpdatePriorityStack(null, new HeldDirections(up: false, down: true, left: false, right: false));
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, new HeldDirections(up: false, down: true, left: false, right: false));
            Assert.AreEqual(GridDirection.Down, GridMoveInputBuffer.TopDirection(stack));

            // (a) Right tapped while Down still held - Right must win immediately.
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, new HeldDirections(up: false, down: true, left: false, right: true));
            Assert.AreEqual(GridDirection.Right, GridMoveInputBuffer.TopDirection(stack), "a fresh tap must immediately outrank an already-held direction");

            // (b) Right released, Down still held - must fall back to Down.
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, new HeldDirections(up: false, down: true, left: false, right: false));
            Assert.AreEqual(GridDirection.Down, GridMoveInputBuffer.TopDirection(stack), "releasing the newer key must fall back to the direction still held");

            // (c) Down released too - nothing held, must stop.
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, HeldDirections.None);
            Assert.IsNull(GridMoveInputBuffer.TopDirection(stack), "nothing held must resolve to no movement");
        }

        [Test]
        public void PressedThenReleasedBeforeMoveEnds_NoExtraMoveWhenMoverFrees()
        {
            // Regression guard carried over from the previous revision's fix
            // for the "releases but keeps going ~0.5s longer" bug: holding a
            // direction, releasing it while a move is still animating
            // (several blocked frames tick by with the key up), must not
            // leave anything behind for the mover to consume once it frees.
            List<GridDirection> stack = GridMoveInputBuffer.UpdatePriorityStack(null, new HeldDirections(up: true, down: false, left: false, right: false));
            Assert.AreEqual(GridDirection.Up, GridMoveInputBuffer.TopDirection(stack), "sanity: stack should track the held direction while pressed");

            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, HeldDirections.None);
            Assert.IsNull(GridMoveInputBuffer.TopDirection(stack), "must clear the instant the key comes up, even mid-animation");

            // A few more blocked frames tick by with the key still up.
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, HeldDirections.None);
            stack = GridMoveInputBuffer.UpdatePriorityStack(stack, HeldDirections.None);

            Assert.IsNull(GridMoveInputBuffer.TopDirection(stack), "no extra move should start once the key was released before the current move finished");
        }
    }
}
