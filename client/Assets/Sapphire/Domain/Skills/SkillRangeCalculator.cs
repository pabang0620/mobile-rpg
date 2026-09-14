using System;
using System.Collections.Generic;
using Sapphire.Domain.Grid;

namespace Sapphire.Domain.Skills
{
    /// <summary>
    /// Pure tile-distance math for skill ranges. Grid-based on purpose -
    /// the 02_SYSTEM_CONTRACTS.md skill table expresses range in world
    /// units ("u"), but GridWorldConversion.CellSize is 1, so 1 world
    /// unit of range equals exactly 1 tile and no conversion is needed.
    /// This has no notion of targets, monsters or damage - only which
    /// grid cells a skill's shape covers from a given origin/facing.
    /// </summary>
    public static class SkillRangeCalculator
    {
        /// <summary>
        /// Chebyshev (chessboard) distance: the number of grid steps needed
        /// when diagonal movement counts the same as orthogonal movement.
        /// Used so a square "N tiles" range reads the same in every direction.
        /// </summary>
        public static int ChebyshevDistance(GridCoord a, GridCoord b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return Math.Max(dx, dy);
        }

        public static bool IsWithinRange(GridCoord origin, GridCoord target, int rangeTiles)
        {
            return ChebyshevDistance(origin, target) <= rangeTiles;
        }

        /// <summary>
        /// The tiles directly ahead of the caster along one direction, from
        /// 1 tile out up to and including rangeTiles. Excludes the origin
        /// tile itself. Returns an empty list when rangeTiles &lt;= 0.
        /// </summary>
        public static IReadOnlyList<GridCoord> TilesInLine(GridCoord origin, GridDirection facing, int rangeTiles)
        {
            var result = new List<GridCoord>();
            if (rangeTiles <= 0)
            {
                return result;
            }

            GridCoord step = facing.ToOffset();
            GridCoord cursor = origin;
            for (int i = 0; i < rangeTiles; i++)
            {
                cursor += step;
                result.Add(cursor);
            }

            return result;
        }

        /// <summary>
        /// The square block of tiles within radiusTiles of origin (Chebyshev
        /// distance), including the origin tile itself. A radius of 0
        /// returns just the origin.
        /// </summary>
        public static IReadOnlyList<GridCoord> TilesInRadius(GridCoord origin, int radiusTiles)
        {
            var result = new List<GridCoord>();
            if (radiusTiles < 0)
            {
                return result;
            }

            for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
            {
                for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
                {
                    result.Add(new GridCoord(origin.X + dx, origin.Y + dy));
                }
            }

            return result;
        }
    }
}
