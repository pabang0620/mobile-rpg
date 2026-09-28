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

        /// <summary>
        /// Vertical offset from a tile's CENTER (GridToWorld) to where an
        /// actor's FEET should be placed. Actors used to stand with their
        /// feet pinned to the exact tile center, which - combined with a
        /// foot-pivoted sprite (see Domain.Character.CharacterFootPivotCalculator)
        /// - made the body stick up into the tile above and read as floating
        /// in the upper half of its own tile. -0.3 puts the feet 0.2 tile
        /// above the tile's bottom edge (bottom edge is center - 0.5), which
        /// is the standard top-down RPG "feet near the bottom, small margin"
        /// look. 2026-09-28 (character/VFX position fix, docs/HANDOFF.md).
        /// </summary>
        public const float ActorFootOffsetY = -0.3f;

        /// <summary>
        /// Vertical distance from an actor's feet (GridToActorFeet) to the
        /// visual center of its body, for anchoring self-targeted effects
        /// (buffs/shields) on the torso instead of the ground. Measured with
        /// PIL from the actual character art (MageTopdownGridSheet.png /
        /// WarriorTopdownGridSheet.png, PPU=302, 362px cells, imported by
        /// Editor/ArtImportConfigurator.cs and
        /// Editor/WarriorArtImportConfigurator.cs): averaging each
        /// direction/pose frame's alpha bounding-box height (feet to top of
        /// head) across both sheets gives ~0.936 world units tall, so half
        /// of that - the body's vertical center - is ~0.468. Rounded to a
        /// clean Domain constant.
        /// </summary>
        public const float ActorBodyCenterHeight = 0.45f;

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

        /// <summary>
        /// Where an actor standing on <paramref name="coord"/> should place
        /// its transform (its FEET), instead of the raw tile center.
        /// </summary>
        public static WorldPoint GridToActorFeet(GridCoord coord)
        {
            WorldPoint center = GridToWorld(coord);
            return new WorldPoint(center.X, center.Y + ActorFootOffsetY);
        }

        /// <summary>
        /// Inverse of <see cref="GridToActorFeet"/> - recovers the tile an
        /// actor is standing on from its feet position, by undoing the foot
        /// offset before falling back to WorldToGrid. Use this (not
        /// WorldToGrid directly) whenever a tile is derived from an actor's
        /// transform.position.
        /// </summary>
        public static GridCoord ActorFeetToGrid(WorldPoint feet)
        {
            return WorldToGrid(new WorldPoint(feet.X, feet.Y - ActorFootOffsetY));
        }
    }
}
