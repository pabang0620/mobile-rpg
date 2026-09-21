using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    internal static class SlimeKingdomTerrainBuilder
    {
        internal const int Width = 40;
        internal const int Height = 30;
        internal const int SpawnX = 19;
        internal const int SpawnY = 2;

        private const string PrimaryAtlas = "Assets/Sapphire/Art/World/SlimeKingdomAtlas.png";
        private const string PropsAtlas = "Assets/Sapphire/Art/World/SlimeKingdomProps2.png";
        private const string SeamlessTileDir = "Assets/Sapphire/Art/World/SlimeKingdom/SeamlessV4/";

        internal static TerrainBuildResult Build()
        {
            Tile blocker = CreateBlockerTile();
            // Retain Tile asset paths/GUIDs while replacing shared atlas slices
            // with standalone, edge-matched textures like the starting map.
            Tile[] grass = CreateStandaloneTiles("Grass", 4, "Tile_SlimeV3_Grass_");
            Tile[] road = CreateStandaloneTiles("Dirt", 4, "Tile_SlimeV3_Dirt_");
            Tile[] water = CreateStandaloneTiles("Water", 4, "Tile_SlimeV3_Water_");
            Tile[] stone = CreateStandaloneTiles("Stone", 4, "Tile_SlimeV3_Stone_");
            // South, north, east, west land edges, then NW/NE/SW/SE water corners.
            Tile[] shore = CreateStandaloneTiles("Shore", 32, "Tile_SlimeV3_Shore_");

            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.GetComponent<Grid>().cellSize = Vector3.one;
            var groundGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGo.transform.SetParent(gridGo.transform);
            var ground = groundGo.GetComponent<Tilemap>();
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = -30000;
            var collisionGo = new GameObject("Collision", typeof(Tilemap));
            collisionGo.transform.SetParent(gridGo.transform);
            var collision = collisionGo.GetComponent<Tilemap>();

            PopulateTerrain(ground, collision, grass, road, water, stone, shore, blocker);
            var zones = new List<InteractableZone>();
            BuildLandmarks(collision, blocker, zones);
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

        private static void PopulateTerrain(Tilemap ground, Tilemap collision, Tile[] grass, Tile[] royalRoad, Tile[] water, Tile[] stone, Tile[] shore, Tile blocker)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool border = x == 0 || x == Width - 1 || y == 0 || y == Height - 1;
                bool liquid = IsLiquid(x, y);
                bool bridge = IsBridge(x, y);
                bool palace = y >= 24;
                bool road = IsRoyalRoad(x, y);
                // A bridge is a walkable overlay above water. Never replace its
                // river/lake bed with a road tile or it visibly sits on dry land.
                Tile chosen = palace ? SelectVariant(stone, x, y, 62, 82, 94)
                    : liquid ? SelectWaterOrShore(water, shore, x, y)
                    : road ? SelectVariant(royalRoad, x, y, 64, 82, 95)
                    : SelectVariant(grass, x, y, 55, 78, 93);
                ground.SetTile(cell, chosen);
                if (border || (liquid && !bridge)) collision.SetTile(cell, blocker);
            }
        }

        private static bool IsLiquid(int x, int y)
        {
            bool kingdomRiver = y >= 14 && y <= 16 && x >= 1 && x <= 38;
            bool palaceMoat = y >= 22 && y <= 23 && x >= 5 && x <= 34;
            return kingdomRiver || palaceMoat;
        }

        private static bool IsBridge(int x, int y)
        {
            bool riverBridges = y >= 14 && y <= 16 && ((x >= 7 && x <= 9) || (x >= 18 && x <= 20) || (x >= 30 && x <= 32));
            bool palaceBridge = y >= 22 && y <= 23 && x >= 18 && x <= 20;
            return riverBridges || palaceBridge;
        }

        private static bool IsRoyalRoad(int x, int y)
        {
            bool main = x >= 18 && x <= 20 && y >= 1;
            int dx = x - 19, dy = y - 9;
            bool plaza = dx * dx + dy * dy <= 24;
            bool west = x >= 7 && x <= 9 && y >= 3 && y <= 21;
            bool east = x >= 30 && x <= 32 && y >= 5 && y <= 21;
            bool lowerLoop = y >= 8 && y <= 10 && x >= 7 && x <= 32;
            bool upperLoop = y >= 18 && y <= 20 && x >= 7 && x <= 32;
            return main || plaza || west || east || lowerLoop || upperLoop;
        }

        private static void BuildLandmarks(Tilemap collision, Tile blocker, List<InteractableZone> zones)
        {
            Transform root = new GameObject("SlimeForestLandmarks").transform;

            // Forest Entrance

            // Natural Blockers (Cliffs & Hedges representing dense forest edges)
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 8, 6, 1.5f, 1);
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 30, 6, 1.5f, 1);
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 12, 8, 1.5f, 1);
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 26, 8, 1.5f, 1);

            // Mysteries of the forest
            BuildInteractable(root, collision, blocker, zones, PropsAtlas, "crystal_cave", "Slime2_Cave", 3, 21,
                "봉인된 동굴입니다. 마력으로 봉인되어 있습니다. 안에서 빛이 새어 들어옵니다.", null, 2f);
            BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, "west_chest", "SlimeProp_Chest", 12, 19,
                "낡은 보물 상자입니다. 누군가 숨겨둔 마력이 흘러나옵니다!", null, 1.15f);
            BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, "east_chest", "SlimeProp_Chest", 27, 19,
                "모닥불 옆에 숨겨진 상자입니다. 별빛 결정이 가득 담겨 있습니다!", null, 1.15f);

            // Bridges over the river
            PlaceVisualScaled(root, PropsAtlas, "Slime2_BridgeV", 8, 15, 3f, 3f, "WestRiverBridge");
            PlaceVisualScaled(root, PropsAtlas, "Slime2_BridgeV", 19, 15, 3f, 3f, "CentralRiverBridge");
            PlaceVisualScaled(root, PropsAtlas, "Slime2_BridgeV", 31, 15, 3f, 3f, "EastRiverBridge");
            PlaceVisualScaled(root, PropsAtlas, "Slime2_BridgeV", 19, 22, 3f, 2f, "NorthBridge");
            
            // Natural Terrain (Cliffs)
            PlaceBlockingSet(root, collision, blocker, PropsAtlas, "Slime2_Cliff", new[]
            {
                new Vector2Int(2,6), new Vector2Int(2,12), new Vector2Int(2,24), new Vector2Int(37,6),
                new Vector2Int(37,13), new Vector2Int(37,24), new Vector2Int(7,26), new Vector2Int(31,26),
                new Vector2Int(14,24), new Vector2Int(24,24) // Replaced statues with cliffs
            }, 1.65f);
            
            // Crystals and Mushrooms
            PlaceBlockingSet(root, collision, blocker, PrimaryAtlas, "SlimeProp_Crystal", new[]
            {
                new Vector2Int(5,11), new Vector2Int(13,16), new Vector2Int(26,15),
                new Vector2Int(34,12), new Vector2Int(8,24), new Vector2Int(30,24),
                new Vector2Int(19, 28) // Replaced throne with a giant crystal
            }, 1.1f);
            PlaceBlockingSet(root, collision, blocker, PrimaryAtlas, "SlimeProp_Mushroom", new[]
            {
                new Vector2Int(6,5), new Vector2Int(32,5), new Vector2Int(5,20), new Vector2Int(34,20)
            }, 1f);

            // Remove cut-off buildings, add more mushrooms and slimes instead.

            // Wild Slimes (Monsters) - Hunting Ground!
            BuildEncounter(root, collision, blocker, zones, 16, 13, "야생 슬라임이 길을 막고 있습니다.");
            BuildEncounter(root, collision, blocker, zones, 14, 18, "수풀 근처에서 서성이는 슬라임 무리입니다.");
            BuildEncounter(root, collision, blocker, zones, 25, 18, "마력을 머금은 변종 슬라임이 경계하고 있습니다.");
            BuildEncounter(root, collision, blocker, zones, 8, 8, "점액질을 흘리는 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 30, 8, "통통 튀어다니는 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 10, 20, "화가 난 듯한 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 28, 22, "거대한 슬라임 무리입니다.");
            BuildEncounter(root, collision, blocker, zones, 6, 15, "반짝이는 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 32, 14, "먹이를 찾는 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 12, 26, "숲의 기운을 받은 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 26, 26, "단단해 보이는 슬라임입니다.");
            BuildEncounter(root, collision, blocker, zones, 19, 25, "보스!", true);
            
            // Exit Gate
            BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, "return_gate", "SlimeProp_Gate", 19, 0,
                "시작 마을(사파이어 허브)로 돌아갑니다.", "VillageHub", 2.5f);
        }

        private static void BuildEncounter(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones, int x, int y, string message, bool isBoss = false)
        {
            var zone = BuildInteractable(parent, collision, blocker, zones, PrimaryAtlas, "slime_" + x + "_" + y, "SlimeProp_Slime", x, y, message, null, .82f);
            var monster = zone.gameObject.AddComponent<Sapphire.Presentation.Combat.MonsterController>();
            // Basic Slime Stats: 30 HP, 5 ATK, 2 DEF
            if (isBoss) { zone.transform.localScale = new Vector3(1.5f, 1.5f, 1f); monster.Initialize(x, y, 150, 15, 5, true); } else { monster.Initialize(x, y, 30, 5, 2, false); }
        }

        private static void PlaceBlockingFootprint(Transform parent, Tilemap collision, Tile blocker, string atlas, string sprite, int x, int y, float scale, int radius)
        {
            PlaceVisual(parent, atlas, sprite, x, y, scale, sprite + "_" + x + "_" + y);
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++) collision.SetTile(new Vector3Int(x + dx, y + dy, 0), blocker);
        }

        private static void PlaceBlockingSet(Transform parent, Tilemap collision, Tile blocker, string atlas, string sprite, IEnumerable<Vector2Int> coords, float scale)
        {
            foreach (Vector2Int coord in coords)
            {
                PlaceVisual(parent, atlas, sprite, coord.x, coord.y, scale, sprite + "_" + coord.x + "_" + coord.y);
                collision.SetTile(new Vector3Int(coord.x, coord.y, 0), blocker);
            }
        }

        private static InteractableZone BuildInteractable(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones,
            string atlas, string id, string sprite, int x, int y, string message, string destination, float scale)
        {
            GameObject go = PlaceVisual(parent, atlas, sprite, x, y, scale, id);
            collision.SetTile(new Vector3Int(x, y, 0), blocker);
            var zone = go.AddComponent<InteractableZone>();
            AssignField(zone, "interactableId", id); AssignField(zone, "gridX", x); AssignField(zone, "gridY", y);
            AssignField(zone, "message", message); AssignField(zone, "destinationScene", destination); zones.Add(zone);
            return zone;
        }

        private static GameObject PlaceVisual(Transform parent, string atlas, string sprite, int x, int y, float scale, string name)
        {
            return PlaceVisualScaled(parent, atlas, sprite, x, y, scale, scale, name);
        }

        private static GameObject PlaceVisualScaled(Transform parent, string atlas, string sprite, int x, int y, float scaleX, float scaleY, string name)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            // Shift the sprite UP by half its Y scale so the foot (bottom edge)
            // sits exactly on tile y, not half a unit below it.
            Vector3 center = SapphireSceneBuilder.CellCenter(x, y);
            go.transform.position = new Vector3(center.x, center.y + scaleY * 0.5f, center.z);
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = LoadSprite(atlas, sprite);
            // Sort by tile foot (center y) so tall props always render above lower ground.
            renderer.sortingOrder = Mathf.RoundToInt(-center.y * 100f);
            return go;
        }

        private static Tile SelectVariant(Tile[] variants, int x, int y, int firstCut, int secondCut, int thirdCut)
        {
            // Four adjacent crops of one continuous material, never shuffled.
            return variants[(x & 1) + 2 * (y & 1)];
        }

        private static Tile SelectWaterOrShore(Tile[] water, Tile[] shore, int x, int y)
        {
            bool n = !IsLiquid(x, y + 1), s = !IsLiquid(x, y - 1), w = !IsLiquid(x - 1, y), e = !IsLiquid(x + 1, y);
            int phase = 8 * ((x & 1) + 2 * (y & 1));
            if (n && w) return shore[phase + 7];
            if (n && e) return shore[phase + 6];
            if (s && w) return shore[phase + 5];
            if (s && e) return shore[phase + 4];
            if (n) return shore[phase + 1];
            if (s) return shore[phase];
            if (w) return shore[phase + 3];
            if (e) return shore[phase + 2];
            return SelectVariant(water, x, y, 56, 78, 93);
        }

        private static void ValidatePlayableLayout(Tilemap collision, IEnumerable<InteractableZone> zones)
        {
            var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
            Vector2Int start = new Vector2Int(SpawnX, SpawnY);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            reached.Add(start); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in dirs)
                {
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= Width || next.y < 0 || next.y >= Height || reached.Contains(next)) continue;
                    if (collision.HasTile(new Vector3Int(next.x, next.y, 0))) continue;
                    reached.Add(next); queue.Enqueue(next);
                }
            }
            foreach (InteractableZone zone in zones)
            {
                Vector2Int target = new Vector2Int(zone.Coord.X, zone.Coord.Y);
                if (!dirs.Any(d => reached.Contains(target + d))) throw new Exception("Unreachable Slime Kingdom landmark: " + zone.InteractableIdValue);
            }
            if (reached.Count < Width * Height * 55 / 100) throw new Exception("Slime Kingdom reachable area is too small: " + reached.Count);
        }

        private static Tile CreateGroundTile(string textureName, string fileName)
        {
            EnsureGeneratedDir(); string texturePath = SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/Ground/" + textureName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null) throw new Exception("Ground sprite not found at " + texturePath);
            string path = SapphireSceneBuilder.GeneratedDir + "/" + fileName; Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }

        private static Tile[] CreateStandaloneTiles(string family, int count, string filePrefix)
        {
            EnsureGeneratedDir();
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                string texturePath = SeamlessTileDir + family + i + ".png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (sprite == null) throw new Exception("Seamless tile sprite not found at " + texturePath);
                string path = SapphireSceneBuilder.GeneratedDir + "/" + filePrefix + i + ".asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); tiles[i] = tile;
            }
            return tiles;
        }

        private static Tile CreateBlockerTile()
        {
            EnsureGeneratedDir(); string path = SapphireSceneBuilder.GeneratedDir + "/Tile_SlimeBlocker.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = null; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }

        private static Sprite LoadSprite(string atlas, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null) throw new Exception("Sprite '" + name + "' not found at " + atlas); return sprite;
        }

        private static void EnsureGeneratedDir()
        {
            if (!AssetDatabase.IsValidFolder(SapphireSceneBuilder.GeneratedDir)) AssetDatabase.CreateFolder("Assets/Sapphire", "Generated");
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null) throw new Exception("Field not found: " + fieldName); field.SetValue(target, value);
        }
    }
}
