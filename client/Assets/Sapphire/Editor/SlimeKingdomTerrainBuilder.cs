using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    // Authored occupancy first; every visual is an unscaled Modular64 tile or prop.
    internal static class SlimeKingdomTerrainBuilder
    {
        internal const int Width = 40, Height = 30, SpawnX = 19, SpawnY = 2;
        private const string Root = "Assets/Sapphire/Art/World/Modular64/";
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private static readonly int[] Dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] Dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

        internal static TerrainBuildResult Build()
        {
            var tiles = ReadLibrary();
            var land = new bool[Width, Height];
            var path = new bool[Width, Height];
            var high = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                land[x, y] = !IsWater(x, y);
                high[x, y] = x >= 28 && x <= 36 && y >= 21 && y <= 27 && !(y == 27 && (x == 28 || x == 36));
            }
            // Winding approach, central breathing space, fork to both crossings,
            // western treasure loop, northern boss clearing and eastern stair approach.
            Route(path, 1, 19, 1, 19, 4, 16, 7, 18, 10, 22, 11);
            Disk(path, 19, 9, 3);
            Route(path, 1, 18, 10, 13, 11, 12, 13, 12, 17, 16, 19, 20, 22, 19, 25);
            Route(path, 1, 22, 11, 27, 12, 27, 18, 30, 18, 31, 19);
            Route(path, 1, 12, 18, 8, 20, 6, 24, 9, 25);
            Route(path, 1, 16, 19, 23, 19, 27, 18);
            Disk(path, 19, 25, 3);
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++) path[x, y] &= land[x, y] && !high[x, y] && y > 0 && y < Height - 1 && x > 0 && x < Width - 1;

            var grid = new GameObject("Grid", typeof(Grid));
            grid.GetComponent<Grid>().cellSize = Vector3.one;
            Tilemap water = Layer(grid, "Water", -30010), foam = Layer(grid, "Foam", -30009);
            Tilemap ground = Layer(grid, "Ground", -30000), road = Layer(grid, "Path", -29990);
            Tilemap shadow = Layer(grid, "Shadow", -29980), elevation = Layer(grid, "Elevated Top", -29960);
            Tilemap cliff = Layer(grid, "Cliff", -29970), stairs = Layer(grid, "Stairs", -29950);
            Tilemap bridges = Layer(grid, "Bridges", -29940), collision = Layer(grid, "Collision", -31000);
            collision.GetComponent<TilemapRenderer>().enabled = false;
            Tile blocker = Blocker();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                // Water below the land masks makes transparent shoreline corners honest.
                water.SetTile(cell, tiles["W" + (1 + Hash(x, y) % 8).ToString("D3")]);
                if (land[x, y]) ground.SetTile(cell, Blob(tiles, "G", land, x, y));
                if (path[x, y]) road.SetTile(cell, Blob(tiles, "PA", path, x, y));
                if (high[x, y]) elevation.SetTile(cell, Blob(tiles, "ET", high, x, y));
                if (!land[x, y] || x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                    collision.SetTile(cell, blocker);
                // The current movement map is cell-based: reserve the complete rim,
                // leaving exactly the stair opening as the only entry to the plateau.
                if (high[x, y] && Directions.Any(d => !At(high, x + d.x, y + d.y)) && !(x == 31 && y == 21))
                    collision.SetTile(cell, blocker);
            }
            AddBridge(12, 13, 15, tiles, bridges, ground, collision, blocker);
            AddBridge(27, 14, 16, tiles, bridges, ground, collision, blocker);
            AddShorelineFoam(foam, land, tiles);
            // Two-row southern cliff, with a single narrow south-facing staircase.
            for (int x = 28; x <= 36; x++)
            {
                if (x == 31) continue;
                int shape = x == 28 || x == 32 ? 2 : x == 30 || x == 36 ? 3 : 1;
                for (int y = 19; y <= 20; y++)
                {
                    cliff.SetTile(new Vector3Int(x, y, 0), tiles["C" + shape.ToString("D3")]);
                    collision.SetTile(new Vector3Int(x, y, 0), blocker);
                    road.SetTile(new Vector3Int(x, y, 0), null);
                }
                shadow.SetTile(new Vector3Int(x, 18, 0), tiles["SH" + (shape + 4).ToString("D3")]);
            }
            for (int y = 19; y <= 21; y++)
                stairs.SetTile(new Vector3Int(31, y, 0), tiles["S" + (22 - y).ToString("D3")]);

            var decor = new GameObject("Decorations").transform;
            decor.SetParent(grid.transform, false);
            Decorate(decor, tiles, land, path, high, collision, blocker);
            var zones = new List<InteractableZone>();
            Landmark(decor, tiles["D021"].sprite, "return_gate", 19, 0, "시작 마을(사파이어 허브)로 돌아갑니다.", "VillageHub", collision, blocker, zones);
            Landmark(decor, tiles["D024"].sprite, "west_chest", 7, 25, "오래된 나무 아래 숨겨진 보물 상자입니다.", null, collision, blocker, zones);
            Landmark(decor, tiles["D024"].sprite, "east_chest", 34, 24, "고대 유적 안의 보물 상자입니다.", null, collision, blocker, zones);
            Landmark(decor, tiles["D109"].sprite, "crystal_cave", 32, 25, "봉인된 성소입니다. 안에서 희미한 빛이 새어 나옵니다.", null, collision, blocker, zones);
            Prop(decor, tiles["D108"].sprite, "RuinPillarWest", 29, 23, collision, blocker, true, 2);
            Prop(decor, tiles["D108"].sprite, "RuinPillarEast", 34, 26, collision, blocker, true, 2);
            Sprite slime = AssetDatabase.LoadAllAssetsAtPath("Assets/Sapphire/Art/World/SlimeKingdomAtlas.png").OfType<Sprite>().Single(s => s.name == "SlimeProp_Slime");
            int[,] encounters = { { 8, 8 }, { 12, 6 }, { 25, 8 }, { 31, 10 }, { 16, 11 }, { 9, 19 }, { 17, 19 }, { 24, 20 }, { 6, 23 }, { 22, 26 } };
            var encounterPositions = new List<Vector2Int>();
            for (int i = 0; i < encounters.GetLength(0); i++) Encounter(decor, slime, encounters[i, 0], encounters[i, 1], false, collision, blocker, encounterPositions);
            Encounter(decor, slime, 19, 25, true, collision, blocker, encounterPositions);
            Validate(land, high, collision, ground, zones, encounterPositions);
            var builder = grid.AddComponent<TilemapGridMapBuilder>();
            Assign(builder, "groundTilemap", ground); Assign(builder, "collisionTilemap", collision);
            Assign(builder, "width", Width); Assign(builder, "height", Height);
            Assign(builder, "originCellX", 0); Assign(builder, "originCellY", 0);
            AssetDatabase.SaveAssets();
            return new TerrainBuildResult(builder, zones.ToArray(), ground);
        }

        private static bool IsWater(int x, int y)
        {
            int bottom = x < 5 ? 12 : x < 10 ? 13 : x < 16 ? 13 : x < 22 ? 12 : x < 31 ? 14 : x < 36 ? 13 : 12;
            return y >= bottom && y <= bottom + (x >= 17 && x <= 20 ? 3 : 2);
        }

        private static void AddBridge(int x, int bottom, int top, Dictionary<string, Tile> tiles, Tilemap bridges, Tilemap ground, Tilemap collision, Tile occupancy)
        {
            for (int y = bottom - 1; y <= top + 1; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bridges.SetTile(cell, tiles[y == bottom - 1 ? "B005" : y == top + 1 ? "B006" : "B004"]);
                if (y >= bottom && y <= top) ground.SetTile(cell, occupancy);
                collision.SetTile(cell, null);
            }
        }

        private static void AddShorelineFoam(Tilemap foam, bool[,] land, Dictionary<string, Tile> tiles)
        {
            var cells = new List<Vector3Int>();
            var ordinals = new List<int>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                // TS03 foam occupies water; its mask describes adjacent land.
                if (land[x, y]) continue;
                int mask = 0;
                for (int d = 0; d < 8; d++) if (At(land, x + Dx[d], y + Dy[d])) mask |= 1 << d;
                mask = Normalize(mask);
                if (mask == 0) continue;
                cells.Add(new Vector3Int(x, y, 0));
                ordinals.Add(Array.IndexOf(Masks, mask));
            }
            TileBase[] frames = Enumerable.Range(1, 188).Select(i => (TileBase)tiles["F" + i.ToString("D3")]).ToArray();
            foam.gameObject.AddComponent<ModularFoamAnimator>().Configure(foam, cells.ToArray(), ordinals.ToArray(), frames);
        }

        private static void Decorate(Transform root, Dictionary<string, Tile> tiles, bool[,] land, bool[,] path, bool[,] high, Tilemap collision, Tile blocker)
        {
            // Dress only the already-blocked rim, retaining every inner corridor.
            // Small props join irregular tree groups without scaling their modules.
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (x != 0 && x != Width - 1 && y != 0 && y != Height - 1) continue;
                if (!land[x, y] || (y == 0 && x >= 18 && x <= 20)) continue;
                int h = Hash(x, y);
                bool riverNear = !At(land, x, y + 1) && y < Height - 1
                    || !At(land, x, y + 2) && y < Height - 2;
                bool tree = h % 5 == 0 && !riverNear;
                string id = tree ? (h % 2 == 0 ? "D101" : "D102")
                    : h % 3 == 0 ? "D005" : h % 2 == 0 ? "D012" : "D013";
                Prop(root, tiles[id].sprite, "Border_" + x + "_" + y, x, y, collision, blocker, true);
            }
            // Trunks occupy one cell; the 2x3 canopy is visual and remains walkable underneath.
            int[,] trees = { { 2, 2 }, { 5, 3 }, { 9, 2 }, { 13, 3 }, { 25, 2 }, { 29, 3 }, { 34, 2 }, { 37, 4 },
                { 3, 7 }, { 5, 10 }, { 9, 10 }, { 34, 8 }, { 37, 11 }, { 2, 18 }, { 4, 21 }, { 2, 25 },
                { 4, 27 }, { 10, 27 }, { 13, 24 }, { 14, 27 }, { 24, 27 }, { 26, 24 }, { 37, 18 }, { 38, 25 } };
            for (int i = 0; i < trees.GetLength(0); i++)
                Prop(root, tiles[i % 3 == 0 ? "D102" : "D101"].sprite, "ForestTree_" + i, trees[i, 0], trees[i, 1], collision, blocker, true);
            Prop(root, tiles["D114"].sprite, "TreasureAncientTree", 7, 27, collision, blocker, true);
            for (int x = 2; x < Width - 2; x++)
            for (int y = 3; y < Height - 2; y++)
            {
                if (!land[x, y] || path[x, y] || high[x, y] || collision.HasTile(new Vector3Int(x, y, 0))) continue;
                int h = Hash(x, y);
                if (h % 23 != 0) continue;
                string[] small = { "D007", "D009", "D010", "D014", "D027", "D029" };
                Prop(root, tiles[small[h % small.Length]].sprite, "Undergrowth_" + x + "_" + y, x, y, collision, blocker, false);
            }
        }

        private static GameObject Prop(Transform parent, Sprite sprite, string id, int x, int y, Tilemap collision, Tile blocker, bool blocked, int footprintWidth = 1)
        {
            var go = new GameObject(id, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = SapphireSceneBuilder.CellCenter(x, y) + Vector3.right * ((footprintWidth - 1) * .5f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100);
            if (blocked) for (int dx = 0; dx < footprintWidth; dx++) collision.SetTile(new Vector3Int(x + dx, y, 0), blocker);
            return go;
        }

        private static InteractableZone Landmark(Transform parent, Sprite sprite, string id, int x, int y, string message, string destination, Tilemap collision, Tile blocker, List<InteractableZone> zones)
        {
            var go = Prop(parent, sprite, id, x, y, collision, blocker, true, id == "crystal_cave" ? 2 : 1);
            var zone = go.AddComponent<InteractableZone>();
            Assign(zone, "interactableId", id); Assign(zone, "gridX", x); Assign(zone, "gridY", y);
            Assign(zone, "message", message); Assign(zone, "destinationScene", destination);
            zones.Add(zone); return zone;
        }

        private static void Encounter(Transform parent, Sprite sprite, int x, int y, bool boss, Tilemap collision, Tile blocker, List<Vector2Int> positions)
        {
            var cell = new Vector3Int(x, y, 0);
            if (collision.HasTile(cell)) throw new Exception("Slime encounter overlaps a blocker: " + cell);
            var monster = Prop(parent, sprite, "slime_" + x + "_" + y, x, y, collision, blocker, false);
            collision.SetTile(cell, blocker);
            positions.Add(new Vector2Int(x, y));
            float scale = boss ? 1.5f : .82f;
            monster.transform.localScale = new Vector3(scale, scale, 1);
            monster.transform.position += Vector3.up * (sprite.pivot.y / sprite.pixelsPerUnit * scale);
            monster.AddComponent<Sapphire.Presentation.Combat.MonsterController>().Initialize(x, y, boss ? 150 : 30, boss ? 15 : 5, boss ? 5 : 2, boss);
        }

        private static void Validate(bool[,] land, bool[,] high, Tilemap collision, Tilemap ground, IEnumerable<InteractableZone> zones, IEnumerable<Vector2Int> encounterPositions)
        {
            var start = new Vector2Int(SpawnX, SpawnY);
            if (!land[SpawnX, SpawnY] || collision.HasTile(new Vector3Int(SpawnX, SpawnY, 0))) throw new Exception("Slime spawn is not dry and open.");
            foreach (int x in new[] { 12, 27 })
            {
                int bottom = x == 12 ? 13 : 14, top = bottom + 2;
                if (!land[x, bottom - 1] || !land[x, top + 1]) throw new Exception("Bridge endpoints are not land.");
                for (int y = bottom; y <= top; y++)
                    if (land[x, y] || collision.HasTile(new Vector3Int(x, y, 0))) throw new Exception("Bridge deck is not open over water.");
            }
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                bool bridge = (x == 12 && y >= 13 && y <= 15) || (x == 27 && y >= 14 && y <= 16);
                if (!land[x, y] && !bridge && !collision.HasTile(new Vector3Int(x, y, 0))) throw new Exception("Unblocked water.");
                if (high[x, y] && Directions.Any(d => !At(high, x + d.x, y + d.y)) && !(x == 31 && y == 21) && !collision.HasTile(new Vector3Int(x, y, 0))) throw new Exception("Plateau has a non-stair entrance.");
            }
            var reached = new HashSet<Vector2Int> { start }; var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in Directions)
                {
                    var next = current + direction; var cell = new Vector3Int(next.x, next.y, 0);
                    if (next.x < 0 || next.y < 0 || next.x >= Width || next.y >= Height || reached.Contains(next) || !ground.HasTile(cell) || collision.HasTile(cell)) continue;
                    reached.Add(next); queue.Enqueue(next);
                }
            }
            foreach (var zone in zones)
            {
                if (!land[zone.Coord.X, zone.Coord.Y]) throw new Exception("Slime landmark placed in water: " + zone.InteractableIdValue);
                if (!Directions.Any(d => reached.Contains(new Vector2Int(zone.Coord.X, zone.Coord.Y) + d))) throw new Exception("Unreachable Slime landmark: " + zone.InteractableIdValue);
            }
            foreach (var position in encounterPositions)
            {
                if (!At(land, position.x, position.y)) throw new Exception("Slime encounter placed outside dry land: " + position);
                if (!Directions.Any(d => reached.Contains(position + d))) throw new Exception("Unreachable Slime encounter: " + position);
            }
            if (!reached.Contains(new Vector2Int(31, 22)) || reached.Count < 700) throw new Exception("Slime layout connectivity failed: " + reached.Count);
            string report = "Slime Kingdom Modular64 validation passed: " + reached.Count + " reachable cells; two wet bridges; stair-only plateau; all landmark approaches reachable.\nSpawn (19,2); bridges (12,13..15), (27,14..16); stairs (31,19..21); boss (19,25).\n";
            string verification = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../verification"));
            Directory.CreateDirectory(verification);
            File.WriteAllText(System.IO.Path.Combine(verification, "slime-kingdom-layout-validation.txt"), report);
            Debug.Log(report);
        }

        private static Dictionary<string, Tile> ReadLibrary()
        {
            var tiles = new Dictionary<string, Tile>();
            foreach (string file in Directory.GetFiles(Root, "*.csv"))
            {
                string[] lines = File.ReadAllLines(file); string[] headers = lines[0].Split(',');
                int assetColumn = Array.IndexOf(headers, "asset");
                foreach (string line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    string[] fields = line.Split(','); Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(fields[assetColumn]);
                    if (tile == null || tile.sprite == null || Mathf.Abs(tile.sprite.pixelsPerUnit - 64) > .001f) throw new Exception("Invalid Modular64 tile: " + fields[0]);
                    tiles.Add(fields[0], tile);
                }
            }
            return tiles;
        }

        private static Tile Blob(Dictionary<string, Tile> tiles, string prefix, bool[,] occupied, int x, int y)
        {
            int mask = 0;
            for (int d = 0; d < 8; d++) if (At(occupied, x + Dx[d], y + Dy[d])) mask |= 1 << d;
            mask = Normalize(mask);
            if (mask == 255 && prefix == "G") return tiles["G" + (48 + Hash(x, y) % 12).ToString("D3")];
            if (mask == 255 && prefix == "PA") return tiles["PV" + (1 + Hash(x, y) % 12).ToString("D3")];
            // All three manifests enumerate the same normalized masks in ascending order.
            int index = Array.IndexOf(Masks, mask) + 1;
            return tiles[prefix + index.ToString("D3")];
        }
        private static readonly int[] Masks = Enumerable.Range(0, 256).Select(Normalize).Distinct().OrderBy(m => m).ToArray();
        private static int Normalize(int mask) { for (int d = 1; d < 8; d += 2) if ((mask & (1 << (d - 1))) == 0 || (mask & (1 << ((d + 1) % 8))) == 0) mask &= ~(1 << d); return mask; }
        private static bool At(bool[,] cells, int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && cells[x, y];
        private static int Hash(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + 713); h = (h ^ (h >> 13)) * 1274126177; return (int)((h ^ (h >> 16)) & 0x7fffffff); } }
        private static void Disk(bool[,] cells, int cx, int cy, int radius) { for (int x = cx - radius; x <= cx + radius; x++) for (int y = cy - radius; y <= cy + radius; y++) if (x >= 0 && y >= 0 && x < Width && y < Height && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius) cells[x, y] = true; }
        private static void Route(bool[,] cells, int radius, params int[] points)
        {
            for (int i = 0; i < points.Length - 2; i += 2)
            {
                int x = points[i], y = points[i + 1], tx = points[i + 2], ty = points[i + 3];
                while (true) { Disk(cells, x, y, radius); if (x == tx && y == ty) break; if (x != tx) x += Math.Sign(tx - x); if (y != ty) y += Math.Sign(ty - y); }
            }
        }
        private static Tilemap Layer(GameObject grid, string name, int order) { var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(grid.transform, false); go.GetComponent<TilemapRenderer>().sortingOrder = order; return go.GetComponent<Tilemap>(); }
        private static Tile Blocker()
        {
            string path = SapphireSceneBuilder.GeneratedDir + "/Tile_SlimeBlocker.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = null; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }
        private static void Assign(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (info == null) throw new Exception("Field not found: " + field); info.SetValue(target, value);
        }
    }
}
