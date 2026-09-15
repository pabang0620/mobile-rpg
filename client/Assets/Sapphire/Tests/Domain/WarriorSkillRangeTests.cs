using System.Collections.Generic;
using NUnit.Framework;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// Pure range-math coverage for the warrior kit's non-mage shapes
    /// (whirlwind's ring, ground slam's frontal cone) plus dash's reuse of
    /// GridMover.TryBlink (already covered generically by GridBlinkTests -
    /// this file adds the warrior-specific range value, 3 tiles, so the
    /// "돌진 경로 막힘 처리" requirement has a test that documents which
    /// number belongs to the warrior's dash). These are Domain-only; the
    /// Presentation-side WarriorSkillCatalog range/id values are not
    /// referenced here (Domain.Tests only references Sapphire.Domain).
    /// </summary>
    public class WarriorSkillRangeTests
    {
        [Test]
        public void TilesInRing_RadiusOne_ReturnsExactlyEightTilesExcludingOrigin()
        {
            var origin = new GridCoord(5, 5);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInRing(origin, 1);

            Assert.AreEqual(8, tiles.Count);
            CollectionAssert.DoesNotContain(tiles, origin);
            CollectionAssert.Contains(tiles, new GridCoord(4, 4));
            CollectionAssert.Contains(tiles, new GridCoord(6, 6));
            CollectionAssert.Contains(tiles, new GridCoord(5, 6));
        }

        [Test]
        public void TilesInRing_RadiusZero_ReturnsEmpty()
        {
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInRing(new GridCoord(0, 0), 0);

            Assert.AreEqual(0, tiles.Count);
        }

        [Test]
        public void TilesInFrontCone_RangeOne_ReturnsThreeTilesCenterAndBothSides()
        {
            var origin = new GridCoord(2, 2);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInFrontCone(origin, GridDirection.Right, 1);

            Assert.AreEqual(3, tiles.Count);
            CollectionAssert.Contains(tiles, new GridCoord(3, 2));
            CollectionAssert.Contains(tiles, new GridCoord(3, 3));
            CollectionAssert.Contains(tiles, new GridCoord(3, 1));
            CollectionAssert.DoesNotContain(tiles, origin);
        }

        [Test]
        public void TilesInFrontCone_FacingUp_WidensAlongXAxis()
        {
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInFrontCone(new GridCoord(0, 0), GridDirection.Up, 1);

            CollectionAssert.Contains(tiles, new GridCoord(0, 1));
            CollectionAssert.Contains(tiles, new GridCoord(1, 1));
            CollectionAssert.Contains(tiles, new GridCoord(-1, 1));
        }

        [Test]
        public void TilesInFrontCone_ZeroRange_ReturnsEmpty()
        {
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInFrontCone(new GridCoord(0, 0), GridDirection.Down, 0);

            Assert.AreEqual(0, tiles.Count);
        }

        [Test]
        public void Dash_BlockedPath_StopsBeforeObstacleAtWarriorDashRange()
        {
            // Warrior's dash range is 3 tiles (WarriorSkillCatalog.DashSkillId,
            // Presentation-side) - reuses the exact same GridMover.TryBlink
            // mechanic mage's teleport already uses (see GridBlinkTests),
            // just documented here under the warrior's own range value.
            const int warriorDashRangeTiles = 3;
            var map = new GridMap(10, 10);
            map.SetBlocked(new GridCoord(3, 1), true);
            var mover = new GridMover(new GridCoord(1, 1), GridDirection.Right);

            MoveResult result = mover.TryBlink(warriorDashRangeTiles, map);

            Assert.AreEqual(MoveResult.Started, result);
            Assert.AreEqual(new GridCoord(2, 1), mover.Position);
        }
    }
}
