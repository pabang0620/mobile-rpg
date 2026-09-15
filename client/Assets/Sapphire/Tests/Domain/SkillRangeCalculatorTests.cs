using System.Collections.Generic;
using NUnit.Framework;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;

namespace Sapphire.Domain.Tests
{
    public class SkillRangeCalculatorTests
    {
        [Test]
        public void ChebyshevDistance_SameCoord_ReturnsZero()
        {
            var coord = new GridCoord(3, 5);

            Assert.AreEqual(0, SkillRangeCalculator.ChebyshevDistance(coord, coord));
        }

        [Test]
        public void ChebyshevDistance_DiagonalOffset_ReturnsMaxOfAxes()
        {
            var origin = new GridCoord(0, 0);
            var target = new GridCoord(2, 5);

            Assert.AreEqual(5, SkillRangeCalculator.ChebyshevDistance(origin, target));
        }

        [Test]
        public void TilesInLine_ZeroRange_ReturnsEmpty()
        {
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInLine(new GridCoord(0, 0), GridDirection.Up, 0);

            Assert.AreEqual(0, tiles.Count);
        }

        [Test]
        public void TilesInLine_Right_ReturnsSequentialTilesExcludingOrigin()
        {
            var origin = new GridCoord(2, 2);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInLine(origin, GridDirection.Right, 3);

            Assert.AreEqual(3, tiles.Count);
            Assert.AreEqual(new GridCoord(3, 2), tiles[0]);
            Assert.AreEqual(new GridCoord(4, 2), tiles[1]);
            Assert.AreEqual(new GridCoord(5, 2), tiles[2]);
            CollectionAssert.DoesNotContain(tiles, origin);
        }

        [Test]
        public void TilesInLine_Up_MatchesFacingOffset()
        {
            var origin = new GridCoord(0, 0);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInLine(origin, GridDirection.Up, 2);

            Assert.AreEqual(new GridCoord(0, 1), tiles[0]);
            Assert.AreEqual(new GridCoord(0, 2), tiles[1]);
        }

        [Test]
        public void TilesInRadius_RadiusZero_ReturnsOnlyOrigin()
        {
            var origin = new GridCoord(1, 1);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInRadius(origin, 0);

            Assert.AreEqual(1, tiles.Count);
            Assert.AreEqual(origin, tiles[0]);
        }

        [Test]
        public void TilesInRadius_RadiusOne_ReturnsThreeByThreeBlockIncludingOrigin()
        {
            var origin = new GridCoord(5, 5);

            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInRadius(origin, 1);

            Assert.AreEqual(9, tiles.Count);
            CollectionAssert.Contains(tiles, origin);
            CollectionAssert.Contains(tiles, new GridCoord(4, 4));
            CollectionAssert.Contains(tiles, new GridCoord(6, 6));
        }

        [Test]
        public void TilesInRadius_NegativeRadius_ReturnsEmpty()
        {
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInRadius(new GridCoord(0, 0), -1);

            Assert.AreEqual(0, tiles.Count);
        }
    }
}
