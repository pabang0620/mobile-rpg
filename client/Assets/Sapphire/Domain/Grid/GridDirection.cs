using System;

namespace Sapphire.Domain.Grid
{
    public enum GridDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class GridDirectionExtensions
    {
        public static GridCoord ToOffset(this GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up:
                    return new GridCoord(0, 1);
                case GridDirection.Down:
                    return new GridCoord(0, -1);
                case GridDirection.Left:
                    return new GridCoord(-1, 0);
                case GridDirection.Right:
                    return new GridCoord(1, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown grid direction.");
            }
        }
    }
}
