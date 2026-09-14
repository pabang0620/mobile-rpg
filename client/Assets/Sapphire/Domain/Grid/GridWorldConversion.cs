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
    /// </summary>
    public static class GridWorldConversion
    {
        public const float CellSize = 1f;

        public static WorldPoint GridToWorld(GridCoord coord)
        {
            return new WorldPoint(coord.X * CellSize, coord.Y * CellSize);
        }

        public static GridCoord WorldToGrid(WorldPoint world)
        {
            return new GridCoord(
                (int)Math.Floor(world.X / CellSize + 0.5f),
                (int)Math.Floor(world.Y / CellSize + 0.5f));
        }
    }
}
