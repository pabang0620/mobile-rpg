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
    internal readonly struct TerrainBuildResult
    {
        internal readonly TilemapGridMapBuilder GridMapBuilder;
        internal readonly InteractableZone[] InteractableZones;
        internal readonly Tilemap GroundTilemap;

        internal TerrainBuildResult(TilemapGridMapBuilder gridMapBuilder, InteractableZone[] interactableZones, Tilemap groundTilemap)
        {
            GridMapBuilder = gridMapBuilder;
            InteractableZones = interactableZones;
            GroundTilemap = groundTilemap;
        }
    }

    internal static class VillageHubTerrainBuilder
    {
        private const string PropsAtlas = "Assets/Sapphire/Art/World/SlimeKingdomProps2.png";
        private const string PrimaryAtlas = "Assets/Sapphire/Art/World/SlimeKingdomAtlas.png";

        internal static TerrainBuildResult Build()
        {
            (Tile blockerTile, Tile[] grassTiles, Tile[] dirtTiles) = CreateTiles();

            var (gridGo, groundTilemap, collisionTilemap) = CreateGridAndTilemaps();
            PopulateGroundAndCollision(groundTilemap, collisionTilemap, grassTiles, dirtTiles, blockerTile);
            TilemapGridMapBuilder gridMapBuilder = BuildGridMapBuilder(gridGo, groundTilemap, collisionTilemap);

            BuildFences();
            
            var zones = new List<InteractableZone>();
            BuildLandmarks(collisionTilemap, blockerTile, zones);

            return new TerrainBuildResult(gridMapBuilder, zones.ToArray(), groundTilemap);
        }

        private static (Tile blocker, Tile[] grass, Tile[] dirt) CreateTiles()
        {
            Tile blockerTile = CreateBlockerTile();

            Tile grassTileA = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Grass_0.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_0.asset");
            Tile grassTileB = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Grass_1.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_1.asset");
            Tile grassTileC = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Grass_2.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_2.asset");
            Tile[] grassTiles = { grassTileA, grassTileB, grassTileC };

            Tile dirtTileA = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Dirt_0.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_0.asset");
            Tile dirtTileB = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Dirt_1.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_1.asset");
            Tile dirtTileC = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/Ground/Dirt_2.png", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_2.asset");
            Tile[] dirtTiles = { dirtTileA, dirtTileB, dirtTileC };

            AssetDatabase.SaveAssets();
            return (blockerTile, grassTiles, dirtTiles);
        }

        private static (GameObject gridGo, Tilemap groundTilemap, Tilemap collisionTilemap) CreateGridAndTilemaps()
        {
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.GetComponent<Grid>().cellSize = Vector3.one;

            var groundGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGo.transform.SetParent(gridGo.transform);
            var groundTilemap = groundGo.GetComponent<Tilemap>();
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = -100;

            var collisionGo = new GameObject("Collision", typeof(Tilemap));
            collisionGo.transform.SetParent(gridGo.transform);
            var collisionTilemap = collisionGo.GetComponent<Tilemap>();

            return (gridGo, groundTilemap, collisionTilemap);
        }

        private static void PopulateGroundAndCollision(
            Tilemap groundTilemap, Tilemap collisionTilemap, Tile[] grassTiles, Tile[] dirtTiles, Tile blockerTile)
        {
            for (int x = 0; x < SapphireSceneBuilder.MapWidth; x++)
            {
                for (int y = 0; y < SapphireSceneBuilder.MapHeight; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isBorder = x == 0 || x == SapphireSceneBuilder.MapWidth - 1 || y == 0 || y == SapphireSceneBuilder.MapHeight - 1;
                    
                    // Center plaza and paths
                    bool plaza = (x >= 9 && x <= 15 && y >= 6 && y <= 12);
                    bool verticalPath = (x >= 11 && x <= 13);
                    bool horizontalPath = (y >= 8 && y <= 10 && x >= 4 && x <= 20);
                    bool onPath = (plaza || verticalPath || horizontalPath) && !isBorder;

                    Tile[] variants = onPath ? dirtTiles : grassTiles;
                    uint hash = HashCell(x, y);
                    Tile tile = variants[(int)(hash % (uint)variants.Length)];
                    groundTilemap.SetTile(cell, tile);
                    groundTilemap.SetTransformMatrix(cell, GetFlipMatrix(hash));

                    if (isBorder)
                    {
                        collisionTilemap.SetTile(cell, blockerTile);
                    }
                }
            }
        }

        private static uint HashCell(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return h;
        }

        private static Matrix4x4 GetFlipMatrix(uint hash)
        {
            uint flipState = (hash / 3) % 4;
            bool flipX = (flipState & 1) != 0;
            bool flipY = (flipState & 2) != 0;
            var scale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
            return Matrix4x4.Scale(scale);
        }

        private static TilemapGridMapBuilder BuildGridMapBuilder(GameObject gridGo, Tilemap groundTilemap, Tilemap collisionTilemap)
        {
            var gridMapBuilder = gridGo.AddComponent<TilemapGridMapBuilder>();
            AssignField(gridMapBuilder, "groundTilemap", groundTilemap);
            AssignField(gridMapBuilder, "collisionTilemap", collisionTilemap);
            AssignField(gridMapBuilder, "width", SapphireSceneBuilder.MapWidth);
            AssignField(gridMapBuilder, "height", SapphireSceneBuilder.MapHeight);
            AssignField(gridMapBuilder, "originCellX", 0);
            AssignField(gridMapBuilder, "originCellY", 0);
            return gridMapBuilder;
        }

        private static void BuildFences()
        {
            var fencesRoot = new GameObject("Fences");
            Sprite fenceStraight = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceStraight");
            Sprite fenceCornerA = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceCornerA");

            int mapWidth = SapphireSceneBuilder.MapWidth;
            int mapHeight = SapphireSceneBuilder.MapHeight;

            for (int x = 1; x < mapWidth - 1; x++)
            {
                if (x != SapphireSceneBuilder.SpawnX)
                {
                    PlaceFence(fencesRoot.transform, fenceStraight, x, 0, 0f, "Fence_Bottom_" + x);
                    PlaceFence(fencesRoot.transform, fenceStraight, x, mapHeight - 1, 0f, "Fence_Top_" + x);
                }
            }

            for (int y = 1; y < mapHeight - 1; y++)
            {
                PlaceFence(fencesRoot.transform, fenceStraight, 0, y, 90f, "Fence_Left_" + y);
                PlaceFence(fencesRoot.transform, fenceStraight, mapWidth - 1, y, 90f, "Fence_Right_" + y);
            }

            PlaceFence(fencesRoot.transform, fenceCornerA, 0, 0, 0f, "Fence_Corner_BL");
            PlaceFence(fencesRoot.transform, fenceCornerA, mapWidth - 1, 0, 90f, "Fence_Corner_BR");
            PlaceFence(fencesRoot.transform, fenceCornerA, 0, mapHeight - 1, 270f, "Fence_Corner_TL");
            PlaceFence(fencesRoot.transform, fenceCornerA, mapWidth - 1, mapHeight - 1, 180f, "Fence_Corner_TR");
        }

        private static void BuildLandmarks(Tilemap collision, Tile blocker, List<InteractableZone> zones)
        {
            Transform root = new GameObject("VillageLandmarks").transform;

            // Welcome sign
            Sprite signSprite = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_Signpost");
            var signGo = new GameObject("Signpost", typeof(SpriteRenderer));
            signGo.transform.SetParent(root);
            signGo.transform.position = CellCenter(13, 7);
            signGo.GetComponent<SpriteRenderer>().sprite = signSprite;
            var signZone = signGo.AddComponent<InteractableZone>();
            AssignField(signZone, "interactableId", "signpost");
            AssignField(signZone, "gridX", 13); AssignField(signZone, "gridY", 7);
            AssignField(signZone, "message", "초보 모험가의 마을, 사파이어 타운에 오신 것을 환영합니다.");
            zones.Add(signZone);
            collision.SetTile(new Vector3Int(13, 7, 0), blocker);

            // Village Props (moved from SlimeKingdom)
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_House", 6, 12, 1.8f, 1);
            BuildInteractable(root, collision, blocker, zones, PropsAtlas, "elder_house", "Slime2_House", 18, 12,
                "촌장님의 집이다. 문이 굳게 잠겨 있다.", null, 1.8f);

            BuildInteractable(root, collision, blocker, zones, PropsAtlas, "village_shop", "Slime2_Shop", 8, 8,
                "잡화점이다. 상인이 아직 출근하지 않은 것 같다.", null, 1.8f);
                
            BuildInteractable(root, collision, blocker, zones, PropsAtlas, "village_fountain", "Slime2_Fountain", 12, 9,
                "마을의 맑은 분수다. 마음이 편안해진다.", null, 1.55f);

            PlaceVisual(root, PropsAtlas, "Slime2_Lamp", 10, 6, .9f, "Lamp1", 1);
            PlaceVisual(root, PropsAtlas, "Slime2_Lamp", 14, 6, .9f, "Lamp2", 1);
            PlaceVisual(root, PropsAtlas, "Slime2_Flowers", 7, 7, .8f, "Flowers1", 1);
            PlaceVisual(root, PropsAtlas, "Slime2_Flowers", 17, 7, .8f, "Flowers2", 1);
            
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 18, 9, 1.5f, 1);
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 6, 9, 1.5f, 1);

            // Gate to Slime Forest (Top exit)
            BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, "slime_kingdom_gate", "SlimeProp_Gate", SapphireSceneBuilder.SpawnX, SapphireSceneBuilder.MapHeight - 1,
                "슬라임 숲으로 이동합니다.", "SlimeKingdom", 2.6f);
        }

        private static void PlaceFence(Transform parent, Sprite sprite, int x, int y, float rotationZ, string name)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.position = CellCenter(x, y);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            go.GetComponent<SpriteRenderer>().sprite = sprite;
        }

        private static Vector3 CellCenter(int x, int y)
        {
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            return new Vector3(world.X, world.Y, 0f);
        }

        private static Tile CreateBlockerTile()
        {
            string assetPath = SapphireSceneBuilder.GeneratedDir + "/Tile_Blocker.asset";
            EnsureGeneratedDir();
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null) return existing;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, assetPath);
            return tile;
        }

        private static Tile CreateGroundTile(string texturePath, string assetPath)
        {
            EnsureGeneratedDir();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null) throw new Exception($"Sprite not found at {texturePath}");

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
            {
                existing.sprite = sprite;
                existing.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, assetPath);
            return tile;
        }

        private static void EnsureGeneratedDir()
        {
            if (!AssetDatabase.IsValidFolder(SapphireSceneBuilder.GeneratedDir))
            {
                AssetDatabase.CreateFolder("Assets/Sapphire", "Generated");
            }
        }

        private static Sprite LoadNamedSprite(string path, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null) throw new Exception($"Sprite '{name}' not found at {path}");
            return sprite;
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null) throw new Exception($"Field '{fieldName}' not found on {type.Name}");
            field.SetValue(target, value);
        }
        
        private static void PlaceBlockingFootprint(Transform parent, Tilemap collision, Tile blocker, string atlas, string sprite, int x, int y, float scale, int radius)
        {
            PlaceVisual(parent, atlas, sprite, x, y, scale, sprite + "_" + x + "_" + y, 1);
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++) 
                    collision.SetTile(new Vector3Int(x + dx, y + dy, 0), blocker);
        }

        private static InteractableZone BuildInteractable(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones,
            string atlas, string id, string sprite, int x, int y, string message, string destination, float scale)
        {
            GameObject go = PlaceVisual(parent, atlas, sprite, x, y, scale, id, 2);
            collision.SetTile(new Vector3Int(x, y, 0), blocker);
            var zone = go.AddComponent<InteractableZone>();
            AssignField(zone, "interactableId", id); AssignField(zone, "gridX", x); AssignField(zone, "gridY", y);
            AssignField(zone, "message", message); AssignField(zone, "destinationScene", destination); zones.Add(zone);
            return zone;
        }

        private static GameObject PlaceVisual(Transform parent, string atlas, string sprite, int x, int y, float scale, string name, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent); 
            go.transform.position = CellCenter(x, y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.GetComponent<SpriteRenderer>(); 
            renderer.sprite = LoadNamedSprite(atlas, sprite); 
            renderer.sortingOrder = order;
            return go;
        }
    }
}
