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

        /// <summary>
        /// The ring of tiles at exactly radiusTiles around origin (Chebyshev),
        /// excluding the origin itself - used by area skills that hit tiles
        /// around the caster but not the caster's own tile (e.g. a melee
        /// whirlwind). A radius of 1 returns the 8 surrounding tiles.
        /// </summary>
        public static IReadOnlyList<GridCoord> TilesInRing(GridCoord origin, int radiusTiles)
        {
            var result = new List<GridCoord>();
            IReadOnlyList<GridCoord> block = TilesInRadius(origin, radiusTiles);
            foreach (GridCoord tile in block)
            {
                if (tile != origin)
                {
                    result.Add(tile);
                }
            }

            return result;
        }

        /// <summary>
        /// A widening fan directly ahead of the caster: for each forward
        /// distance from 1 to rangeTiles, the tile straight ahead plus its
        /// two perpendicular neighbors at that same distance (a 3-wide swathe
        /// the whole way out, not a narrowing/widening triangle). rangeTiles=1
        /// yields exactly 3 tiles (center + left + right of the first step) -
        /// a compact frontal slam. Excludes the origin tile itself.
        /// </summary>
        public static IReadOnlyList<GridCoord> TilesInFrontCone(GridCoord origin, GridDirection facing, int rangeTiles)
        {
            var result = new List<GridCoord>();
            if (rangeTiles <= 0)
            {
                return result;
            }

            GridCoord forwardStep = facing.ToOffset();
            GridCoord perpendicularStep = PerpendicularOffset(facing);
            GridCoord cursor = origin;

            for (int i = 0; i < rangeTiles; i++)
            {
                cursor += forwardStep;
                result.Add(cursor);
                result.Add(cursor + perpendicularStep);
                result.Add(cursor - perpendicularStep);
            }

            return result;
        }

        // The axis perpendicular to facing, as a unit GridCoord offset:
        // Up/Down facing widens along the X axis, Left/Right facing widens
        // along the Y axis.
        private static GridCoord PerpendicularOffset(GridDirection facing)
        {
            switch (facing)
            {
                case GridDirection.Up:
                case GridDirection.Down:
                    return new GridCoord(1, 0);
                case GridDirection.Left:
                case GridDirection.Right:
                    return new GridCoord(0, 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(facing), facing, "Unknown grid direction.");
            }
        }
    }
}
