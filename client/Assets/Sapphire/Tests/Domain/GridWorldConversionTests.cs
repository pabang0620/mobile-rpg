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

        [Test]
        public void GridToActorFeet_ReturnsCellCenterOffsetDownByFootOffset()
        {
            WorldPoint feet = GridWorldConversion.GridToActorFeet(new GridCoord(2, 5));
            WorldPoint center = GridWorldConversion.GridToWorld(new GridCoord(2, 5));

            Assert.AreEqual(center.X, feet.X);
            Assert.AreEqual(center.Y + GridWorldConversion.ActorFootOffsetY, feet.Y);
        }

        [Test]
        public void ActorFeetToGrid_RoundTripsWithGridToActorFeet()
        {
            var original = new GridCoord(3, 4);
            WorldPoint feet = GridWorldConversion.GridToActorFeet(original);

            GridCoord roundTripped = GridWorldConversion.ActorFeetToGrid(feet);

            Assert.AreEqual(original, roundTripped);
        }

        [Test]
        public void ActorFeetToGrid_NegativeCoordRoundTrip_ReturnsOriginalCoord()
        {
            var original = new GridCoord(-6, -9);
            WorldPoint feet = GridWorldConversion.GridToActorFeet(original);

            GridCoord roundTripped = GridWorldConversion.ActorFeetToGrid(feet);

            Assert.AreEqual(original, roundTripped);
        }

        [Test]
        public void ActorFeetToGrid_MultipleCoords_AllRoundTrip()
        {
            GridCoord[] coords =
            {
                new GridCoord(0, 0),
                new GridCoord(1, 0),
                new GridCoord(0, 1),
                new GridCoord(-1, -1),
                new GridCoord(10, -10),
                new GridCoord(-15, 22),
            };

            foreach (GridCoord coord in coords)
            {
                WorldPoint feet = GridWorldConversion.GridToActorFeet(coord);
                GridCoord roundTripped = GridWorldConversion.ActorFeetToGrid(feet);
                Assert.AreEqual(coord, roundTripped);
            }
        }

        [Test]
        public void GridToActorFeet_FeetSitAboveTileBottomEdge()
        {
            // The tile's bottom edge is at center.Y - 0.5; feet should sit
            // strictly above it (standard top-down RPG look), not flush with
            // the edge and not up at the tile center.
            var coord = new GridCoord(0, 0);
            WorldPoint feet = GridWorldConversion.GridToActorFeet(coord);
            WorldPoint center = GridWorldConversion.GridToWorld(coord);
            float bottomEdge = center.Y - 0.5f * GridWorldConversion.CellSize;

            Assert.IsTrue(feet.Y > bottomEdge);
            Assert.IsTrue(feet.Y < center.Y);
        }
    }
}
