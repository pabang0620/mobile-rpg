using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.World
{
    /// <summary>
    /// Builds a fresh Domain GridMap from the scene's Tilemap layers every
    /// time Build() is called (typically once per scene load). Never caches
    /// across scene loads - always reads the tilemaps as they currently are.
    /// </summary>
    public class TilemapGridMapBuilder : MonoBehaviour
    {
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap collisionTilemap;
        [SerializeField] private int width = 14;
        [SerializeField] private int height = 10;
        [SerializeField] private int originCellX;
        [SerializeField] private int originCellY;

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

            return map;
        }
    }
}
