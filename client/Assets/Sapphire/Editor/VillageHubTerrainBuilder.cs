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
            var (blockerTile, grassTiles, dirtTiles, dirtEdgeTiles, waterTiles, shoreTiles) = CreateTiles();

            var (gridGo, groundTilemap, collisionTilemap) = CreateGridAndTilemaps();
            PopulateGroundAndCollision(groundTilemap, collisionTilemap, grassTiles, dirtTiles, dirtEdgeTiles, waterTiles, shoreTiles, blockerTile);
            TilemapGridMapBuilder gridMapBuilder = BuildGridMapBuilder(gridGo, groundTilemap, collisionTilemap);

            BuildFences();
            
            var zones = new List<InteractableZone>();
            BuildLandmarks(collisionTilemap, blockerTile, zones);

            return new TerrainBuildResult(gridMapBuilder, zones.ToArray(), groundTilemap);
        }

        private static (Tile blocker, Tile[] grass, Tile[] dirt, Tile[] dirtEdge, Tile[] water, Tile[] shore) CreateTiles()
        {
            Tile blockerTile = CreateBlockerTile();
            string root = SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/SeamlessV5/";

            Tile[] CreateVariantTiles(string name, int count)
            {
                var tiles = new Tile[count];
                for (int i = 0; i < count; i++)
                {
                    tiles[i] = CreateGroundTile(root + name + i + ".png", SapphireSceneBuilder.GeneratedDir + "/Tile_V5_" + name + i + ".asset");
                }
                return tiles;
            }

            Tile[] grassTiles = CreateVariantTiles("Grass", 4);
            Tile[] dirtTiles = CreateVariantTiles("Dirt", 4);
            Tile[] dirtEdgeTiles = CreateVariantTiles("DirtEdge", 32);
            Tile[] waterTiles = CreateVariantTiles("Water", 4);
            Tile[] shoreTiles = CreateVariantTiles("Shore", 32);

            AssetDatabase.SaveAssets();
            return (blockerTile, grassTiles, dirtTiles, dirtEdgeTiles, waterTiles, shoreTiles);
        }

        private static (GameObject gridGo, Tilemap groundTilemap, Tilemap collisionTilemap) CreateGridAndTilemaps()
        {
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.GetComponent<Grid>().cellSize = Vector3.one;

            var groundGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGo.transform.SetParent(gridGo.transform);
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = -30000;

            var collisionGo = new GameObject("Collision", typeof(Tilemap));
            collisionGo.transform.SetParent(gridGo.transform);
            var collisionMap = collisionGo.GetComponent<Tilemap>();
            
            return (gridGo, groundGo.GetComponent<Tilemap>(), collisionMap);
        }

        private static void PopulateGroundAndCollision(
            Tilemap groundTilemap, Tilemap collisionTilemap, Tile[] grassTiles, Tile[] dirtTiles, Tile[] dirtEdgeTiles, Tile[] waterTiles, Tile[] shoreTiles, Tile blockerTile)
        {
            Sprite treeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/AutumnTree.png");

            for (int x = 0; x < SapphireSceneBuilder.MapWidth; x++)
            {
                for (int y = 0; y < SapphireSceneBuilder.MapHeight; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    
                    bool liquid = IsLiquid(x, y);
                    bool path = IsPath(x, y);
                    bool forest = IsForest(x, y);

                    Tile chosen;
                    if (liquid) chosen = SelectWaterOrShore(waterTiles, shoreTiles, x, y);
                    else if (path) chosen = SelectDirtOrEdge(dirtTiles, dirtEdgeTiles, x, y);
                    else chosen = SelectVariant(grassTiles, x, y);

                    groundTilemap.SetTile(cell, chosen);

                    if (liquid)
                    {
                        collisionTilemap.SetTile(cell, blockerTile);
                    }

                    if (forest && treeSprite != null)
                    {
                        // Spawn tree
                        GameObject tree = new GameObject($"Tree_{x}_{y}");
                        tree.transform.position = groundTilemap.GetCellCenterWorld(cell);
                        SpriteRenderer sr = tree.AddComponent<SpriteRenderer>();
                        sr.sprite = treeSprite;
                        sr.sortingLayerName = "Default"; // Adjust as needed
                        sr.sortingOrder = 100 - y; // Y-sorting

                        collisionTilemap.SetTile(cell, blockerTile);
                    }
                }
            }
        }

        private static bool IsForest(int x, int y)
        {
            // Trees around the borders with some noise
            float borderDist = Mathf.Min(x, Mathf.Min(y, Mathf.Min(SapphireSceneBuilder.MapWidth - 1 - x, SapphireSceneBuilder.MapHeight - 1 - y)));
            float noise = Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 4f;
            return borderDist + noise < 5f;
        }

        private static bool IsPath(int x, int y)
        {
            if (IsForest(x, y)) return false;
            
            // Central plaza area
            float dx = x - 16f;
            float dy = y - 16f;
            if ((dx * dx) / 64f + (dy * dy) / 64f < 1f) return true; // Large dirt area around center

            // Paths connecting around
            float waveV = Mathf.Sin(y * 0.3f) * 2f;
            bool verticalPath = Mathf.Abs(x - 16f + waveV) < 2f;
            
            float waveH = Mathf.Sin(x * 0.3f) * 2f;
            bool horizontalPath = Mathf.Abs(y - 12f + waveH) < 2f && x >= 4 && x <= 28;

            return verticalPath || horizontalPath;
        }

        private static bool IsLiquid(int x, int y)
        {
            if (IsForest(x, y)) return false;
            // A long pond at the bottom (like the image)
            // But bridge in the middle
            if (x >= 14 && x <= 18 && y >= 8 && y <= 12) return false; // Bridge/Dirt cross
            
            float dy = y - 10f;
            float dx = x - 16f;
            float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 2f;
            return Mathf.Abs(dy) + noise < 3f && Mathf.Abs(dx) < 10f;
        }

        private static Tile SelectVariant(Tile[] variants, int x, int y)
        {
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
            return SelectVariant(water, x, y);
        }

        private static Tile SelectDirtOrEdge(Tile[] dirt, Tile[] edge, int x, int y)
        {
            bool n = !IsPath(x, y + 1), s = !IsPath(x, y - 1), w = !IsPath(x - 1, y), e = !IsPath(x + 1, y);
            int phase = 8 * ((x & 1) + 2 * (y & 1));
            if (n && w) return edge[phase + 7];
            if (n && e) return edge[phase + 6];
            if (s && w) return edge[phase + 5];
            if (s && e) return edge[phase + 4];
            if (n) return edge[phase + 1];
            if (s) return edge[phase];
            if (w) return edge[phase + 3];
            if (e) return edge[phase + 2];
            return SelectVariant(dirt, x, y);
        }

        private static TilemapGridMapBuilder BuildGridMapBuilder(GameObject gridGo, Tilemap ground, Tilemap collision)
        {
            var builder = gridGo.AddComponent<TilemapGridMapBuilder>();
            AssignField(builder, "width", SapphireSceneBuilder.MapWidth);
            AssignField(builder, "height", SapphireSceneBuilder.MapHeight);
            AssignField(builder, "groundTilemap", ground);
            AssignField(builder, "collisionTilemap", collision);
            return builder;
        }

        private static Matrix4x4 GetFlipMatrix(uint hash)
        {
            bool flipX = (hash & 1) == 1;
            bool flipY = (hash & 2) == 2;
            return Matrix4x4.Scale(new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f));
        }

        private static uint HashCell(int x, int y)
        {
            uint hash = (uint)(x * 73856093 ^ y * 19349663);
            hash = (hash ^ (hash >> 16)) * 0x85ebca6b;
            hash = (hash ^ (hash >> 13)) * 0xc2b2ae35;
            return hash ^ (hash >> 16);
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
                if (x >= 15 && x <= 17) continue; // gap for vertical path
                PlaceFence(fencesRoot.transform, fenceStraight, x, 0, 0f, "Fence_Bottom_" + x);
                PlaceFence(fencesRoot.transform, fenceStraight, x, mapHeight - 1, 0f, "Fence_Top_" + x);
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

            // Welcome sign (bottom entrance)
            BuildInteractable(root, collision, blocker, zones, SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "signpost", "VillageProps_Signpost", SapphireSceneBuilder.SignX, SapphireSceneBuilder.SignY,
                "초보 모험가의 마을, 사파이어 타운에 오신 것을 환영합니다.", null, 1.0f);

            // Village Props (placed logically on the expanded dirt plaza/grass edges)
            // Town Hall (Large generated building)
            string townHallPath = SapphireSceneBuilder.WorldArtDir + "/TownHallVertical.png";
            BuildInteractable(root, collision, blocker, zones, townHallPath, "town_hall", "TownHallVertical", 16, 16,
                "굳게 잠겨 있다. 지금은 들어갈 수 없다.", null, 1.0f);
            
            // Add collision around the large town hall (Width: 10 tiles, Solid Height: ~10 tiles)
            // The generated image is 1024x1024 at 100 PPU, so ~10x10 units.
            for (int dx = -5; dx <= 5; dx++) {
                for (int dy = 0; dy <= 9; dy++) {
                    // Leave a tiny space for the player to stand right at the center door
                    if (dx == 0 && dy == 0) continue;
                    collision.SetTile(new Vector3Int(16 + dx, 16 + dy, 0), blocker);
                }
            }
            PlaceBlockingFootprint(root, collision, blocker, PropsAtlas, "Slime2_Hedge", 8, 5, 1.5f, 1);

            // Gate to Slime Forest (Top exit)
            BuildInteractable(root, collision, blocker, zones, PrimaryAtlas, "slime_kingdom_gate", "SlimeProp_Gate", SapphireSceneBuilder.SpawnX, SapphireSceneBuilder.MapHeight - 1,
                "슬라임 숲으로 이동합니다.", "SlimeKingdom", 2.6f);

            
        }

        private static InteractableZone BuildNpc(Transform parent, Tilemap collision, Tile blocker, List<InteractableZone> zones,
            Sprite sprite, string id, int x, int y, string message, float scale)
        {
            var go = new GameObject(id, typeof(SpriteRenderer));
            go.transform.SetParent(parent); 
            go.transform.position = CellCenter(x, y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            
            var renderer = go.GetComponent<SpriteRenderer>(); 
            renderer.sprite = sprite; 
            // Y-sorting fix: replace static order with dynamic order component
            go.AddComponent<DynamicYSort>();
            
            collision.SetTile(new Vector3Int(x, y, 0), blocker);
            
            var zone = go.AddComponent<InteractableZone>();
            AssignField(zone, "interactableId", id); 
            AssignField(zone, "gridX", x); 
            AssignField(zone, "gridY", y);
            AssignField(zone, "message", message); 
            zones.Add(zone);
            return zone;
        }

        private static void PlaceFence(Transform parent, Sprite sprite, int x, int y, float rotationZ, string name)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.position = CellCenter(x, y);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100f);
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
            // First check if it's an atlas
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null)
            {
                // Fallback for single sprites
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null || sprite.name != name) 
                    throw new Exception($"Sprite '{name}' not found at {path}");
            }
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
            PlaceVisual(parent, atlas, sprite, x, y, scale, sprite + "_" + x + "_" + y);
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++) 
                    collision.SetTile(new Vector3Int(x + dx, y + dy, 0), blocker);
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
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent); 
            Vector3 center = CellCenter(x, y);
            go.transform.position = new Vector3(center.x, center.y + scale * 0.5f, center.z);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.GetComponent<SpriteRenderer>(); 
            renderer.sprite = LoadNamedSprite(atlas, sprite); 
            // Sort by foot (= original tile center y)
            renderer.sortingOrder = Mathf.RoundToInt(-center.y * 100f);
            return go;
        }
    }
}

