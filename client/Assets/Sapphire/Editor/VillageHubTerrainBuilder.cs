using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Result of <see cref="VillageHubTerrainBuilder.Build"/>: the pieces the
    /// scene orchestrator (<see cref="SapphireSceneBuilder"/>) needs to wire
    /// into the rest of the scene (player spawn map, composition root).
    /// </summary>
    internal readonly struct TerrainBuildResult
    {
        internal readonly TilemapGridMapBuilder GridMapBuilder;
        internal readonly InteractableZone SignpostZone;

        internal TerrainBuildResult(TilemapGridMapBuilder gridMapBuilder, InteractableZone signpostZone)
        {
            GridMapBuilder = gridMapBuilder;
            SignpostZone = signpostZone;
        }
    }

    /// <summary>
    /// Builds the VillageHub scene's ground/collision tilemaps, border fences,
    /// and the interactable signpost. Split out of <see cref="SapphireSceneBuilder"/>
    /// (grid/tile/prop responsibility only - player, camera and UI are built
    /// elsewhere). Idempotent: reuses existing generated Tile assets by path
    /// instead of creating duplicates on re-run.
    /// </summary>
    internal static class VillageHubTerrainBuilder
    {
        internal static TerrainBuildResult Build()
        {
            (Tile blockerTile, Tile[] grassTiles, Tile[] dirtTiles) = CreateTiles();

            var (gridGo, groundTilemap, collisionTilemap) = CreateGridAndTilemaps();
            PopulateGroundAndCollision(groundTilemap, collisionTilemap, grassTiles, dirtTiles, blockerTile);
            TilemapGridMapBuilder gridMapBuilder = BuildGridMapBuilder(gridGo, groundTilemap, collisionTilemap);

            BuildFences();
            InteractableZone signpostZone = BuildSignpost();

            return new TerrainBuildResult(gridMapBuilder, signpostZone);
        }

        private static (Tile blocker, Tile[] grass, Tile[] dirt) CreateTiles()
        {
            Tile blockerTile = CreateBlockerTile();

            Tile grassTileA = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Grass_0", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_0.asset");
            Tile grassTileB = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Grass_1", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_1.asset");
            Tile grassTileC = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Grass_2", SapphireSceneBuilder.GeneratedDir + "/Tile_Grass_2.asset");
            Tile[] grassTiles = { grassTileA, grassTileB, grassTileC };

            Tile dirtTileA = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Dirt_0", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_0.asset");
            Tile dirtTileB = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Dirt_1", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_1.asset");
            Tile dirtTileC = CreateGroundTile(SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png", "GroundTiles_Dirt_2", SapphireSceneBuilder.GeneratedDir + "/Tile_Dirt_2.asset");
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
            int pathColumn = SapphireSceneBuilder.SpawnX;
            for (int x = 0; x < SapphireSceneBuilder.MapWidth; x++)
            {
                for (int y = 0; y < SapphireSceneBuilder.MapHeight; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isBorder = x == 0 || x == SapphireSceneBuilder.MapWidth - 1 || y == 0 || y == SapphireSceneBuilder.MapHeight - 1;
                    bool onPath = x == pathColumn && !isBorder;

                    Tile tile = onPath ? dirtTiles[(x + y) % dirtTiles.Length] : grassTiles[(x + y) % grassTiles.Length];
                    groundTilemap.SetTile(cell, tile);

                    if (isBorder)
                    {
                        collisionTilemap.SetTile(cell, blockerTile);
                    }
                }
            }

            // Sign stands on its own cell - blocked so the player walks up to it instead of onto it.
            collisionTilemap.SetTile(new Vector3Int(SapphireSceneBuilder.SignX, SapphireSceneBuilder.SignY, 0), blockerTile);
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

        // --- Border fences (decorative only - collision comes from the Collision tilemap) ---
        private static void BuildFences()
        {
            var fencesRoot = new GameObject("Fences");
            Sprite fenceStraight = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceStraight");
            Sprite fenceCornerA = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceCornerA");

            int mapWidth = SapphireSceneBuilder.MapWidth;
            int mapHeight = SapphireSceneBuilder.MapHeight;

            for (int x = 1; x < mapWidth - 1; x++)
            {
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

        // --- Signpost (interactable) ---
        private static InteractableZone BuildSignpost()
        {
            Sprite signSprite = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_Signpost");
            var signGo = new GameObject("Signpost", typeof(SpriteRenderer));
            signGo.transform.position = CellCenter(SapphireSceneBuilder.SignX, SapphireSceneBuilder.SignY);
            signGo.GetComponent<SpriteRenderer>().sprite = signSprite;

            var interactableZone = signGo.AddComponent<InteractableZone>();
            AssignField(interactableZone, "interactableId", "signpost");
            AssignField(interactableZone, "gridX", SapphireSceneBuilder.SignX);
            AssignField(interactableZone, "gridY", SapphireSceneBuilder.SignY);
            AssignField(interactableZone, "message", "Welcome to the village hub.");
            return interactableZone;
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
            // Delegates to the single conversion source (GridWorldConversion)
            // instead of re-deriving corner vs. center math here - fixes the
            // same "0.5 unit off" bug this file used to duplicate (fences and
            // the signpost were rendering on the tile corner, not its center).
            WorldPoint world = GridWorldConversion.GridToWorld(new GridCoord(x, y));
            return new Vector3(world.X, world.Y, 0f);
        }

        private static Tile CreateBlockerTile()
        {
            string assetPath = SapphireSceneBuilder.GeneratedDir + "/Tile_Blocker.asset";
            EnsureGeneratedDir();
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, assetPath);
            return tile;
        }

        private static Tile CreateGroundTile(string spritesheetPath, string spriteName, string assetPath)
        {
            EnsureGeneratedDir();
            Sprite sprite = LoadNamedSprite(spritesheetPath, spriteName);

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
            if (sprite == null)
            {
                throw new Exception($"Sprite '{name}' not found at {path}");
            }

            return sprite;
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found on {type.Name}");
            }

            field.SetValue(target, value);
        }
    }
}
