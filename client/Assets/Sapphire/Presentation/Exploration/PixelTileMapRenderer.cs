using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sapphire
{
    /// <summary>
    /// A small, reusable, sprite-rendered pixel tilemap. It deliberately has no collision or game
    /// state: callers feed it an art-only grid produced from their zone snapshot.
    /// </summary>
    public sealed class PixelTileMapRenderer : MonoBehaviour
    {
        [SerializeField] int width = 20;
        [SerializeField] int height = 11;
        [SerializeField] float tileSize = 1f;
        [SerializeField] Vector2 origin;
        [SerializeField] string sortingLayer = "Default";
        [SerializeField] int groundOrder = -1000;
        [SerializeField] int overlayOrder = -600;
        readonly Dictionary<Vector2Int, SpriteRenderer> cells = new Dictionary<Vector2Int, SpriteRenderer>();
        ExplorationTile[,] map;

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public float TileSize { get { return tileSize; } }
        public Vector2 Origin { get { return origin; } }
        public Rect WorldBounds { get { return new Rect(origin, new Vector2(width * tileSize, height * tileSize)); } }

        public void Configure(int mapWidth, int mapHeight, float cellSize, Vector2 bottomLeft)
        {
            width = Mathf.Max(1, mapWidth);
            height = Mathf.Max(1, mapHeight);
            tileSize = Mathf.Max(.01f, cellSize);
            origin = bottomLeft;
            map = new ExplorationTile[width, height];
            Rebuild();
        }

        public void SetTiles(ExplorationTile[,] source)
        {
            if (source == null) throw new ArgumentNullException("source");
            width = source.GetLength(0);
            height = source.GetLength(1);
            map = (ExplorationTile[,])source.Clone();
            Rebuild();
        }

        /// <summary>Rows are supplied top-to-bottom. Unknown characters become grass.</summary>
        public void SetAscii(IList<string> rows)
        {
            if (rows == null || rows.Count == 0) throw new ArgumentException("At least one map row is required.", "rows");
            int widest = 1;
            for (int i = 0; i < rows.Count; i++) widest = Mathf.Max(widest, rows[i] == null ? 0 : rows[i].Length);
            width = widest;
            height = rows.Count;
            map = new ExplorationTile[width, height];
            for (int row = 0; row < height; row++)
            {
                string text = rows[row] ?? string.Empty;
                for (int x = 0; x < width; x++) map[x, height - 1 - row] = x < text.Length ? FromAscii(text[x]) : ExplorationTile.Grass;
            }
            Rebuild();
        }

        public void SetTile(int x, int y, ExplorationTile tile)
        {
            EnsureMap();
            if (x < 0 || y < 0 || x >= width || y >= height) throw new ArgumentOutOfRangeException("Tile coordinate is outside the map.");
            map[x, y] = tile;
            DrawCell(x, y);
        }

        public ExplorationTile GetTile(int x, int y)
        {
            EnsureMap();
            if (x < 0 || y < 0 || x >= width || y >= height) throw new ArgumentOutOfRangeException("Tile coordinate is outside the map.");
            return map[x, y];
        }

        public Vector2 CellCenter(int x, int y) { return origin + new Vector2((x + .5f) * tileSize, (y + .5f) * tileSize); }

        public void Rebuild()
        {
            EnsureMap();
            ClearCells();
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) DrawCell(x, y);
        }

        public void Clear()
        {
            ClearCells();
            map = new ExplorationTile[width, height];
        }

        void Awake() { EnsureMap(); }

        void OnDestroy() { cells.Clear(); }

        void EnsureMap()
        {
            if (map == null || map.GetLength(0) != width || map.GetLength(1) != height) map = new ExplorationTile[Mathf.Max(1, width), Mathf.Max(1, height)];
        }

        void DrawCell(int x, int y)
        {
            var coordinate = new Vector2Int(x, y);
            SpriteRenderer renderer;
            if (!cells.TryGetValue(coordinate, out renderer))
            {
                var child = new GameObject("Tile " + x + "," + y);
                child.transform.SetParent(transform, false);
                renderer = child.AddComponent<SpriteRenderer>();
                cells.Add(coordinate, renderer);
            }
            var tile = map[x, y];
            renderer.sprite = PixelSpriteLibrary.Tile(tile);
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = (tile == ExplorationTile.Tree || tile == ExplorationTile.Wall || tile == ExplorationTile.Roof || tile == ExplorationTile.Door ? overlayOrder : groundOrder) - y;
            renderer.transform.localPosition = CellCenter(x, y);
            renderer.transform.localScale = new Vector3(tileSize, tileSize, 1f);
        }

        void ClearCells()
        {
            foreach (var renderer in cells.Values)
            {
                if (renderer == null) continue;
                if (Application.isPlaying) Destroy(renderer.gameObject); else DestroyImmediate(renderer.gameObject);
            }
            cells.Clear();
        }

        static ExplorationTile FromAscii(char value)
        {
            switch (value)
            {
                case '.': return ExplorationTile.Path;
                case '~': return ExplorationTile.Water;
                case 's': return ExplorationTile.Sand;
                case '*': return ExplorationTile.Flowers;
                case 'T': return ExplorationTile.Tree;
                case '#': return ExplorationTile.Wall;
                case '^': return ExplorationTile.Roof;
                case 'D': return ExplorationTile.Door;
                default: return ExplorationTile.Grass;
            }
        }
    }
}
