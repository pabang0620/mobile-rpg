using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    /// <summary>Builds the 32x24 Slime Kingdom exploration map.</summary>
    internal static class SlimeKingdomTerrainBuilder
    {
        internal const int Width = 32;
        internal const int Height = 24;
        internal const int SpawnX = 15;
        internal const int SpawnY = 2;

        private const string AtlasPath = "Assets/Sapphire/Art/World/SlimeKingdomAtlas.png";

        internal static TerrainBuildResult Build()
        {
            Tile blocker = CreateBlockerTile();
            Tile grass = CreateTileFromTexture("Grass.png", "Tile_SlimeGrass.asset");
            Tile road = CreateTileFromTexture("RoyalRoad.png", "Tile_SlimeRoad.asset");
            Tile pool = CreateTileFromTexture("JellyPool.png", "Tile_SlimePool.asset");
            Tile stone = CreateTileFromTexture("CastleStone.png", "Tile_SlimeStone.asset");

            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.GetComponent<Grid>().cellSize = Vector3.one;
            var groundGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGo.transform.SetParent(gridGo.transform);
            var ground = groundGo.GetComponent<Tilemap>();
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = -100;
            var collisionGo = new GameObject("Collision", typeof(Tilemap));
            collisionGo.transform.SetParent(gridGo.transform);
            var collision = collisionGo.GetComponent<Tilemap>();

            PopulateTerrain(ground, collision, grass, road, pool, stone, blocker);
            var zones = new List<InteractableZone>();
            BuildWorldProps(collision, blocker, zones);
            ValidatePlayableLayout(collision, zones);

            var gridBuilder = gridGo.AddComponent<TilemapGridMapBuilder>();
            AssignField(gridBuilder, "groundTilemap", ground);
            AssignField(gridBuilder, "collisionTilemap", collision);
            AssignField(gridBuilder, "width", Width);
            AssignField(gridBuilder, "height", Height);
            AssignField(gridBuilder, "originCellX", 0);
            AssignField(gridBuilder, "originCellY", 0);

            AssetDatabase.SaveAssets();
            return new TerrainBuildResult(gridBuilder, zones.ToArray(), ground);
        }

        private static void ValidatePlayableLayout(Tilemap collision, IEnumerable<InteractableZone> zones)
        {
            var reached = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(SpawnX, SpawnY);
            reached.Add(start);
            queue.Enqueue(start);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= Width || next.y < 0 || next.y >= Height || reached.Contains(next)) continue;
                    if (collision.HasTile(new Vector3Int(next.x, next.y, 0))) continue;
                    reached.Add(next);
                    queue.Enqueue(next);
                }
            }

            foreach (InteractableZone zone in zones)
            {
                Vector2Int target = new Vector2Int(zone.Coord.X, zone.Coord.Y);
                bool canApproach = directions.Any(direction => reached.Contains(target + direction));
                if (!canApproach)
                    throw new Exception($"Slime Kingdom interactable '{zone.InteractableIdValue}' cannot be reached from spawn.");
            }

            if (reached.Count < Width * Height / 2)
                throw new Exception($"Slime Kingdom exploration area is too constrained: only {reached.Count} reachable cells.");
        }

        private static void PopulateTerrain(Tilemap ground, Tilemap collision, Tile grass, Tile road, Tile pool, Tile stone, Tile blocker)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool border = x == 0 || x == Width - 1 || y == 0 || y == Height - 1;
                    int leftDx = x - 6;
                    int leftDy = y - 9;
                    int rightDx = x - 26;
                    int rightDy = y - 10;
                    bool leftPool = leftDx * leftDx * 4 + leftDy * leftDy * 5 <= 72 && y != 9;
                    bool rightPool = rightDx * rightDx * 4 + rightDy * rightDy * 5 <= 72 && y != 10;
                    bool royalRoad = Mathf.Abs(x - 15) <= 1
                        || (y == 9 && x >= 2 && x <= 15)
                        || (y == 10 && x >= 16 && x <= 29)
                        || ((y == 13 || y == 16) && x >= 10 && x <= 21)
                        || ((x == 10 || x == 21) && y >= 13 && y <= 16)
                        || (y == 14 && x >= 5 && x <= 10)
                        || (y == 15 && x >= 21 && x <= 27);
                    bool castle = y >= 18;

                    ground.SetTile(cell, castle ? stone : royalRoad ? road : leftPool || rightPool ? pool : grass);
                    ground.SetTransformMatrix(cell, GetGroundTransform(x, y));
                    if (border || leftPool || rightPool || (y == 17 && x != 14 && x != 15 && x != 16))
                    {
                        collision.SetTile(cell, blocker);
                    }
                }
            }
        }

        private static Matrix4x4 GetGroundTransform(int x, int y)
        {
            uint hash = (uint)(x * 374761393 + y * 668265263);
            hash = (hash ^ (hash >> 13)) * 1274126177u;
            bool flipX = (hash & 1u) != 0;
            bool flipY = (hash & 2u) != 0;
            return Matrix4x4.Scale(new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f));
        }

        private static void BuildWorldProps(Tilemap collision, Tile blocker, List<InteractableZone> zones)
        {
            var propsRoot = new GameObject("SlimeKingdomProps").transform;

            PlaceBlockingSet(propsRoot, collision, blocker, "SlimeProp_Tree", new[]
            {
                new Vector2Int(2,3), new Vector2Int(5,4), new Vector2Int(9,3),
                new Vector2Int(22,3), new Vector2Int(27,4), new Vector2Int(29,6),
                new Vector2Int(3,15), new Vector2Int(7,16), new Vector2Int(25,15), new Vector2Int(29,17),
                new Vector2Int(1,5), new Vector2Int(1,10), new Vector2Int(1,19),
                new Vector2Int(30,3), new Vector2Int(30,11), new Vector2Int(30,20),
            }, 1.55f);
            PlaceBlockingSet(propsRoot, collision, blocker, "SlimeProp_Crystal", new[]
            {
                new Vector2Int(10,7), new Vector2Int(20,7), new Vector2Int(5,13),
                new Vector2Int(26,14), new Vector2Int(10,16), new Vector2Int(21,16),
                new Vector2Int(6,22), new Vector2Int(24,22),
            }, 1.2f);
            PlaceBlockingSet(propsRoot, collision, blocker, "SlimeProp_Mushroom", new[]
            {
                new Vector2Int(11,4), new Vector2Int(19,4), new Vector2Int(9,12),
                new Vector2Int(21,13), new Vector2Int(4,18), new Vector2Int(27,19),
                new Vector2Int(1,14), new Vector2Int(30,15),
            }, 1.15f);
            PlaceBlockingSet(propsRoot, collision, blocker, "SlimeProp_Statue", new[]
            {
                new Vector2Int(11,18), new Vector2Int(19,18),
            }, 1.35f);

            BuildInteractable(propsRoot, collision, blocker, zones, "return_gate", "SlimeProp_Gate", 15, 0,
                "시작 마을로 돌아갑니다.", "VillageHub", 2.5f);
            BuildInteractable(propsRoot, collision, blocker, zones, "royal_chest_west", "SlimeProp_Chest", 5, 14,
                "왕국의 숨겨진 보물상자다. 푸른 젤리 조각을 발견했다!", null, 1.25f);
            BuildInteractable(propsRoot, collision, blocker, zones, "bridge_chest", "SlimeProp_Chest", 4, 9,
                "젤리 연못의 섬 상자다. 반짝이는 왕국 주화를 발견했다!", null, 1.15f);
            BuildInteractable(propsRoot, collision, blocker, zones, "royal_chest_east", "SlimeProp_Chest", 27, 16,
                "수정 숲의 보물상자다. 별빛 젤리 조각을 발견했다!", null, 1.25f);
            BuildInteractable(propsRoot, collision, blocker, zones, "slime_throne", "SlimeProp_Throne", 15, 22,
                "슬라임 왕의 왕좌다. 왕관의 빛이 다음 모험을 기다리고 있다.", null, 2.4f);

            BuildEncounter(propsRoot, collision, blocker, zones, 12, 8, "정찰 슬라임이 길을 지키고 있다.");
            BuildEncounter(propsRoot, collision, blocker, zones, 18, 11, "수정 슬라임이 마력을 모으고 있다.");
            BuildEncounter(propsRoot, collision, blocker, zones, 13, 15, "근위 슬라임이 왕궁 입구를 지키고 있다.");
            BuildEncounter(propsRoot, collision, blocker, zones, 20, 20, "왕실 슬라임이 왕좌를 순찰하고 있다.");
        }

        private static void BuildEncounter(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones, int x, int y, string message)
        {
            BuildInteractable(parent, collision, blocker, zones, $"slime_{x}_{y}", "SlimeProp_Slime", x, y, message, null, 0.85f);
        }

        private static void PlaceBlockingSet(Transform parent, Tilemap collision, Tile blocker, string spriteName, IEnumerable<Vector2Int> coords, float scale)
        {
            foreach (Vector2Int coord in coords)
            {
                PlaceProp(parent, spriteName, coord.x, coord.y, scale, spriteName + "_" + coord.x + "_" + coord.y);
                collision.SetTile(new Vector3Int(coord.x, coord.y, 0), blocker);
            }
        }

        private static InteractableZone BuildInteractable(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones,
            string id, string spriteName, int x, int y, string message, string destinationScene, float scale)
        {
            GameObject go = PlaceProp(parent, spriteName, x, y, scale, id);
            collision.SetTile(new Vector3Int(x, y, 0), blocker);
            var zone = go.AddComponent<InteractableZone>();
            AssignField(zone, "interactableId", id);
            AssignField(zone, "gridX", x);
            AssignField(zone, "gridY", y);
            AssignField(zone, "message", message);
            AssignField(zone, "destinationScene", destinationScene);
            zones.Add(zone);
            return zone;
        }

        private static GameObject PlaceProp(Transform parent, string spriteName, int x, int y, float scale, string name)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.position = SapphireSceneBuilder.CellCenter(x, y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spriteName);
            renderer.sortingOrder = 1;
            return go;
        }

        private static Tile CreateTileFromTexture(string textureName, string fileName)
        {
            EnsureGeneratedDir();
            string path = SapphireSceneBuilder.GeneratedDir + "/" + fileName;
            string texturePath = SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/Ground/" + textureName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null) throw new Exception("Ground sprite not found at " + texturePath);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tile CreateBlockerTile()
        {
            EnsureGeneratedDir();
            string path = SapphireSceneBuilder.GeneratedDir + "/Tile_SlimeBlocker.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Sprite LoadSprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null) throw new Exception($"Sprite '{name}' not found at {AtlasPath}");
            return sprite;
        }

        private static void EnsureGeneratedDir()
        {
            if (!AssetDatabase.IsValidFolder(SapphireSceneBuilder.GeneratedDir))
                AssetDatabase.CreateFolder("Assets/Sapphire", "Generated");
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null) throw new Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
