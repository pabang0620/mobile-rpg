using NUnit.Framework;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Tests
{
    public class GridWorldConversionTests
    {
        [Test]
        public void GridToWorld_NegativeCoord_ReturnsCellCenter()
        {
            // GridCoord (x, y) maps to the CENTER of that cell in world space,
            // i.e. the corner (x * CellSize) plus half a cell - matching
            // Unity's Tilemap.GetCellCenterWorld for cellSize=1, no cellGap.
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(-3, -2));

            Assert.AreEqual((-3f + 0.5f) * GridWorldConversion.CellSize, world.X);
            Assert.AreEqual((-2f + 0.5f) * GridWorldConversion.CellSize, world.Y);
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
        public void WorldToGrid_AtCellCenter_ReturnsSameCell()
        {
            // The center of cell 0 (world 0.5) must resolve back to cell 0,
            // not spill into cell 1 - this is exactly the bug that made the
            // player appear to stand on a tile border instead of its center.
            var world = new WorldPoint(0.5f * GridWorldConversion.CellSize, 0f);

            GridCoord grid = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(0, grid.X);
        }

        [Test]
        public void WorldToGrid_AtCellEdge_RoundsUpToNextCell()
        {
            // Cell 0 spans world [0, 1). The shared edge with cell 1 is at
            // world 1.0 exactly, which belongs to cell 1 (floor semantics),
            // matching Unity Tilemap.WorldToCell's bin boundaries.
            var world = new WorldPoint(1.0f * GridWorldConversion.CellSize, 0f);

            GridCoord grid = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(1, grid.X);
        }

        [Test]
        public void WorldToGrid_JustBelowCellEdge_StaysInLowerCell()
        {
            var world = new WorldPoint(0.99f * GridWorldConversion.CellSize, 0f);

            GridCoord grid = GridWorldConversion.WorldToGrid(world);

            Assert.AreEqual(0, grid.X);
        }
    }
}
