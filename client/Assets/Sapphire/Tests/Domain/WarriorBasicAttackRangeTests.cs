using System.Collections.Generic;
using NUnit.Framework;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Skills;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// Documents the warrior basic attack's range value the same way
    /// WarriorSkillRangeTests documents the dash range - a dedicated test
    /// tying the named Domain constant to the actual tile-math it drives
    /// (SkillRangeCalculator.TilesInLine), so a future accidental edit to
    /// WarriorCombatConstants.BasicAttackRangeTiles is caught here.
    /// </summary>
    public class WarriorBasicAttackRangeTests
    {
        [Test]
        public void BasicAttackRange_TwoTilesInLine_CoversForwardTilesExcludingOrigin()
        {
            Assert.AreEqual(2, WarriorCombatConstants.BasicAttackRangeTiles);

            var origin = new GridCoord(4, 4);
            IReadOnlyList<GridCoord> tiles = SkillRangeCalculator.TilesInLine(origin, GridDirection.Right, WarriorCombatConstants.BasicAttackRangeTiles);

            Assert.AreEqual(2, tiles.Count);
            CollectionAssert.Contains(tiles, new GridCoord(5, 4));
            CollectionAssert.Contains(tiles, new GridCoord(6, 4));
            CollectionAssert.DoesNotContain(tiles, origin);
        }
    }
}
