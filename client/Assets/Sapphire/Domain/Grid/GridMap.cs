using System;
using System.Collections.Generic;

namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Map bounds and walkability. Obstacle coordinates are registered
    /// externally (e.g. by a level-loading adapter) via SetBlocked.
    /// </summary>
    public class GridMap
    {
        private readonly int width;
        private readonly int height;
        private readonly HashSet<GridCoord> blockedCoords = new HashSet<GridCoord>();

        public GridMap(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "GridMap width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "GridMap height must be greater than zero.");
            }

            this.width = width;
            this.height = height;
        }

        public int Width => width;
        public int Height => height;

        public void SetBlocked(GridCoord coord, bool blocked)
        {
            if (blocked)
            {
                blockedCoords.Add(coord);
            }
            else
            {
                blockedCoords.Remove(coord);
            }
        }

        public bool IsInBounds(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < width && coord.Y >= 0 && coord.Y < height;
        }

        public bool IsWalkable(GridCoord coord)
        {
            return IsInBounds(coord) && !blockedCoords.Contains(coord);
        }
    }
}
