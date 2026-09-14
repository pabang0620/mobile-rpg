using System;

namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Pure world-space point, expressed without any engine vector type.
    /// </summary>
    public readonly struct WorldPoint
    {
        public readonly float X;
        public readonly float Y;

        public WorldPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Pure conversion between grid coordinates and world-space points.
    /// Holds the cell size constant used by both directions.
    ///
    /// GridCoord (x, y) is defined to be the CENTER of that cell in world
    /// space - matching Unity's Tilemap.GetCellCenterWorld(cellPosition) for
    /// a Grid with cellSize=(1,1,1) and no cellGap/anchor offset (see
    /// TilemapGridMapBuilder / VillageHubTerrainBuilder, which map GridCoord
    /// 1:1 onto Tilemap cell coordinates with no additional offset). Unity's
    /// Tilemap.CellToWorld(cellPosition) returns the cell's lower-left
    /// CORNER, not its center, so this deliberately does NOT reuse that
    /// value directly - GridToWorld = (coord + 0.5) * CellSize is the corner
    /// plus half a cell.
    ///
    /// 2026-09-14: fixed from the previous `coord * CellSize` (corner, not
    /// center) formula, which put every grid-driven position (player, skill
    /// range markers) exactly on the shared corner of 4 tiles - the "walking
    /// on the tile border" bug.
    /// </summary>
    public static class GridWorldConversion
    {
        public const float CellSize = 1f;

        public static WorldPoint GridToWorld(GridCoord coord)
        {
            return new WorldPoint((coord.X + 0.5f) * CellSize, (coord.Y + 0.5f) * CellSize);
        }

        public static GridCoord WorldToGrid(WorldPoint world)
        {
            return new GridCoord(
                (int)Math.Floor(world.X / CellSize),
                (int)Math.Floor(world.Y / CellSize));
        }
    }
}
