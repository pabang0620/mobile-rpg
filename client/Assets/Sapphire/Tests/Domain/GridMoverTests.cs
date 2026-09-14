using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    public class GridMoverTests
    {
        private static GridMap CreateOpenMap(int width = 5, int height = 5)
        {
            return new GridMap(width, height);
        }

        [Test]
        public void TryBeginMove_WalkableDirection_ReturnsStartedAndSetsIsMoving()
        {
            var map = CreateOpenMap();
            var mover = new GridMover(new GridCoord(2, 2));

            MoveResult result = mover.TryBeginMove(GridDirection.Up, map);

            Assert.AreEqual(MoveResult.Started, result);
            Assert.IsTrue(mover.IsMoving);
            Assert.AreEqual(GridDirection.Up, mover.Facing);
            Assert.AreEqual(new GridCoord(2, 2), mover.Position);
        }

        [Test]
        public void CompleteMove_AfterStartedMove_UpdatesPositionAndClearsIsMoving()
        {
            var map = CreateOpenMap();
            var mover = new GridMover(new GridCoord(2, 2));
            mover.TryBeginMove(GridDirection.Right, map);

            mover.CompleteMove();

            Assert.AreEqual(new GridCoord(3, 2), mover.Position);
            Assert.IsFalse(mover.IsMoving);
        }

        [Test]
        public void TryBeginMove_TowardBlockedCoord_ReturnsBlockedByObstacleAndPositionUnchanged()
        {
            var map = CreateOpenMap();
            var blocked = new GridCoord(2, 3);
            map.SetBlocked(blocked, true);
            var mover = new GridMover(new GridCoord(2, 2));

            MoveResult result = mover.TryBeginMove(GridDirection.Up, map);

            Assert.AreEqual(MoveResult.BlockedByObstacle, result);
            Assert.AreEqual(new GridCoord(2, 2), mover.Position);
            Assert.IsFalse(mover.IsMoving);
        }

        [Test]
        public void TryBeginMove_TowardOutOfBoundsCoord_ReturnsBlockedByObstacle()
        {
            var map = CreateOpenMap(3, 3);
            var mover = new GridMover(new GridCoord(0, 0));

            MoveResult result = mover.TryBeginMove(GridDirection.Left, map);

            Assert.AreEqual(MoveResult.BlockedByObstacle, result);
            Assert.AreEqual(new GridCoord(0, 0), mover.Position);
        }

        [Test]
        public void TryBeginMove_WhileAlreadyMoving_ReturnsAlreadyMovingAndKeepsOriginalTarget()
        {
            var map = CreateOpenMap();
            var mover = new GridMover(new GridCoord(2, 2));
            mover.TryBeginMove(GridDirection.Up, map);

            MoveResult result = mover.TryBeginMove(GridDirection.Right, map);

            Assert.AreEqual(MoveResult.AlreadyMoving, result);
            Assert.IsTrue(mover.IsMoving);

            mover.CompleteMove();

            Assert.AreEqual(new GridCoord(2, 3), mover.Position);
        }

        [Test]
        public void CompleteMove_WhenNotMoving_IsNoOp()
        {
            var mover = new GridMover(new GridCoord(1, 1));

            mover.CompleteMove();

            Assert.AreEqual(new GridCoord(1, 1), mover.Position);
            Assert.IsFalse(mover.IsMoving);
        }
    }
}
