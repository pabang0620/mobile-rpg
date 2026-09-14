using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    public class GridWorldConversionTests
    {
        [Test]
        public void GridToWorld_NegativeCoord_ScalesByCellSize()
        {
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(-3, -2));

            Assert.AreEqual(-3f * GridWorldConversion.CellSize, world.X);
            Assert.AreEqual(-2f * GridWorldConversion.CellSize, world.Y);
        }

        [Test]
        public void WorldToGrid_NegativeCoordRoundTrip_ReturnsOriginalCoord()
        {
            var original = new GridCoord(-4, -7);
            WorldPoint world = GridWorldConversion.GridToWorld(original);

            GridCoord roundTripped = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(original, roundTripped);
        }

        [Test]
        public void WorldToGrid_HalfwayBoundary_RoundsUp()
        {
            var world = new WorldPoint(0.5f * GridWorldConversion.CellSize, 0f);

            GridCoord grid = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(1, grid.X);
        }

        [Test]
        public void WorldToGrid_JustBelowHalfwayBoundary_RoundsDown()
        {
            var world = new WorldPoint(0.49f * GridWorldConversion.CellSize, 0f);

            GridCoord grid = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(0, grid.X);
        }
    }
}
