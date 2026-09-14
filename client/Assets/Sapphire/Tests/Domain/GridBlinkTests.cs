using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Tests.Domain
{
    public class GridBlinkTests
    {
        [Test] public void BlinkMovesExactlyTwoCells()
        {
            var mover = new GridMover(new GridCoord(1, 1), GridDirection.Right);
            Assert.AreEqual(MoveResult.Started, mover.TryBlink(2, new GridMap(10, 10)));
            Assert.AreEqual(new GridCoord(3, 1), mover.Position);
            Assert.IsFalse(mover.IsMoving);
        }
        [Test] public void BlinkStopsBeforeObstacleAndNeverSkipsIt()
        {
            var map = new GridMap(10, 10); map.SetBlocked(new GridCoord(3, 1), true);
            var mover = new GridMover(new GridCoord(1, 1), GridDirection.Right);
            Assert.AreEqual(MoveResult.Started, mover.TryBlink(5, map));
            Assert.AreEqual(new GridCoord(2, 1), mover.Position);
        }
        [Test] public void BusyAndBlockedBlinksLeavePositionUnchanged()
        {
            var map = new GridMap(3, 3);
            var mover = new GridMover(new GridCoord(0, 0), GridDirection.Left);
            Assert.AreEqual(MoveResult.BlockedByObstacle, mover.TryBlink(2, map));
            Assert.AreEqual(new GridCoord(0, 0), mover.Position);
            mover.TryBeginMove(GridDirection.Right, map);
            Assert.AreEqual(MoveResult.AlreadyMoving, mover.TryBlink(2, map));
            Assert.AreEqual(new GridCoord(0, 0), mover.Position);
        }
    }
}
