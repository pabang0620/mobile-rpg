using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.World
{
    /// <summary>
    /// Shares one live Domain GridMap among scene consumers during play.
    /// Editor builds read fresh tilemaps; runtime cache belongs to this scene component.
    /// </summary>
    public class TilemapGridMapBuilder : MonoBehaviour
    {
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap collisionTilemap;
        [SerializeField] private int width = 14;
        [SerializeField] private int height = 10;
        [SerializeField] private int originCellX;
        [SerializeField] private int originCellY;
        private GridMap runtimeMap;

        /// <summary>
        /// Converts a Domain GridCoord (always 0-based) into the Tilemap cell
        /// it corresponds to in this scene.
        /// </summary>
        public Vector3Int ToCell(GridCoord coord)
        {
            return new Vector3Int(originCellX + coord.X, originCellY + coord.Y, 0);
        }

        public GridMap Build()
        {
            if (UnityEngine.Application.isPlaying && runtimeMap != null) return runtimeMap;
            var map = new GridMap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var coord = new GridCoord(x, y);
                    Vector3Int cell = ToCell(coord);

                    bool hasGround = groundTilemap != null && groundTilemap.HasTile(cell);
                    bool blockedByCollision = collisionTilemap != null && collisionTilemap.HasTile(cell);

                    map.SetBlocked(coord, !hasGround || blockedByCollision);
                }
            }

            if (UnityEngine.Application.isPlaying) runtimeMap = map;
            return map;
        }

        public TileBase GetCollisionTile(GridCoord coord)
        {
            return collisionTilemap != null ? collisionTilemap.GetTile(ToCell(coord)) : null;
        }

        /// <summary>
        /// Updates visual collision and the shared movement map together. Clearing
        /// requires the expected occupant tile, so a replaced blocker is preserved.
        /// Callers must own the reservation they clear (monster spawn cells are reserved).
        /// </summary>
        public bool SetCollisionBlocked(GridCoord coord, bool blocked, TileBase occupantTile)
        {
            var map = Build();
            var cell = ToCell(coord);
            if (!map.IsInBounds(coord) || groundTilemap == null || !groundTilemap.HasTile(cell) ||
                collisionTilemap == null || occupantTile == null) return false;
            var current = collisionTilemap.GetTile(cell);
            if (blocked ? current != null || !map.IsWalkable(coord) : current != occupantTile) return false;
            collisionTilemap.SetTile(cell, blocked ? occupantTile : null);
            map.SetBlocked(coord, blocked);
            return true;
        }
    }
}
