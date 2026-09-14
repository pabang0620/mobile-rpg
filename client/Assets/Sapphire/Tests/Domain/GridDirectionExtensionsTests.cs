using System;
using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    public class GridDirectionExtensionsTests
    {
        [Test]
        public void ToOffset_Down_ReturnsNegativeYOffset()
        {
            GridCoord offset = GridDirection.Down.ToOffset();

            Assert.AreEqual(new GridCoord(0, -1), offset);
        }

        [Test]
        public void ToOffset_UndefinedDirectionValue_ThrowsArgumentOutOfRangeException()
        {
            var invalidDirection = (GridDirection)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => invalidDirection.ToOffset());
        }
    }
}
