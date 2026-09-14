using System;

namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Integer grid coordinate. Pure value type, no engine dependency.
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static GridCoord operator +(GridCoord a, GridCoord b)
        {
            return new GridCoord(a.X + b.X, a.Y + b.Y);
        }

        public static GridCoord operator -(GridCoord a, GridCoord b)
        {
            return new GridCoord(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(GridCoord a, GridCoord b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GridCoord a, GridCoord b)
        {
            return !a.Equals(b);
        }

        public bool Equals(GridCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
