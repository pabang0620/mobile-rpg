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
        internal readonly InteractableZone[] InteractableZones;
        // 2026-09-15 (Phase 1): exposed so the camera (CameraFollowRig) can read
        // the map's actual extent from cellBounds instead of a hardcoded size -
        // see SapphireSceneBuilder.BuildCamera.
        internal readonly Tilemap GroundTilemap;

        internal TerrainBuildResult(TilemapGridMapBuilder gridMapBuilder, InteractableZone[] interactableZones, Tilemap groundTilemap)
        {
            GridMapBuilder = gridMapBuilder;
            InteractableZones = interactableZones;
            GroundTilemap = groundTilemap;
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
            InteractableZone slimeGateZone = BuildSlimeGate();

            return new TerrainBuildResult(gridMapBuilder, new[] { signpostZone, slimeGateZone }, groundTilemap);
        }

        private static (Tile blocker, Tile[] grass, Tile[] dirt) CreateTiles()
        {
            Tile blockerTile = CreateBlockerTile();

            // 2026-09-15: each ground variant is now its own standalone
            // Sprite/Single texture (Art/World/Ground/<Name>.png) instead of
            // a named sub-sprite sliced out of one shared GroundTiles.png
            // atlas - see ArtImportConfigurator.ConfigureGroundAtlas for why
            // (atlas bleed was the source of the tile-boundary seam lines).
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
            int pathColumn = SapphireSceneBuilder.SpawnX;
            for (int x = 0; x < SapphireSceneBuilder.MapWidth; x++)
            {
                for (int y = 0; y < SapphireSceneBuilder.MapHeight; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isBorder = x == 0 || x == SapphireSceneBuilder.MapWidth - 1 || y == 0 || y == SapphireSceneBuilder.MapHeight - 1;
                    bool onPath = x == pathColumn && !isBorder;

                    // 2026-09-16 (REMEDIATION_PLAN.md Phase 2 item 9): the old
                    // `(x + y) % variants.Length` selection is a period-3
                    // diagonal stripe pattern - with only 3 grass variants it
                    // repeats visibly every 3 tiles regardless of how seamless
                    // each individual tile is. Replaced with a deterministic
                    // per-cell spatial hash that picks the variant AND one of 4
                    // flip states independently (12 combinations total) - see
                    // HashCell/GetFlipMatrix below.
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

            // Sign stands on its own cell - blocked so the player walks up to it instead of onto it.
            collisionTilemap.SetTile(new Vector3Int(SapphireSceneBuilder.SignX, SapphireSceneBuilder.SignY, 0), blockerTile);
        }

        // Spatial hash (not a per-tile RNG seeded by call order, so re-running
        // BuildAll always reproduces the exact same layout - this method's
        // "idempotent" class doc guarantee). Two independent bit-groups pulled
        // from one hash: `hash % 3` picks the grass/dirt variant, `(hash / 3) %
        // 4` (a disjoint slice of the same value, not a second hash) picks the
        // flip state, keeping the two choices uncorrelated enough that no
        // visible secondary pattern emerges from reusing one hash for both.
        private static uint HashCell(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return h;
        }

        // 4 flip states: identity / horizontal / vertical / both. Deliberately
        // NOT a 90-degree rotation - these tiles are seamless only along their
        // left-right and top-bottom edges (by design, per the source art), and
        // rotating a tile 90 degrees swaps which edges need to match which
        // neighbors, reintroducing visible seams; mirroring keeps every edge
        // matched against the same corresponding edge on its neighbor, just
        // read in reverse, which seamless tiling art tolerates.
        //
        // 2026-09-16 (F1 fix): no translation. Ground tile sprites are
        // imported with a CENTER pivot (0.5, 0.5) - see
        // ArtImportConfigurator.ConfigureGroundAtlas - so a -1 scale flip
        // already mirrors the tile in place around its own center; the cell
        // origin never moves. The previous `Matrix4x4.TRS(translate, ...)`
        // with translate=(flipX?1:0, flipY?1:0, 0) assumed a CORNER pivot
        // (where you must shift by +1 cell after negating a corner-anchored
        // axis to keep the flipped quad in the same cell) - with a center
        // pivot that extra +1 unit shove pushed every flipped tile into the
        // adjacent cell, leaving its own cell showing bare skybox (the "바닥
        // 구멍" bug) and shifting the dirt path into a zig-zag.
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
                if (x != SapphireSceneBuilder.SpawnX)
                {
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

        private static InteractableZone BuildSlimeGate()
        {
            Sprite gateSprite = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/SlimeKingdomAtlas.png", "SlimeProp_Gate");
            var gateGo = new GameObject("SlimeKingdomGate", typeof(SpriteRenderer));
            gateGo.transform.position = CellCenter(SapphireSceneBuilder.SpawnX, SapphireSceneBuilder.MapHeight - 1);
            gateGo.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            var renderer = gateGo.GetComponent<SpriteRenderer>();
            renderer.sprite = gateSprite;
            renderer.sortingOrder = 2;

            var zone = gateGo.AddComponent<InteractableZone>();
            AssignField(zone, "interactableId", "slime_kingdom_gate");
            AssignField(zone, "gridX", SapphireSceneBuilder.SpawnX);
            AssignField(zone, "gridY", SapphireSceneBuilder.MapHeight - 1);
            AssignField(zone, "message", "슬라임 왕국으로 이동합니다.");
            AssignField(zone, "destinationScene", "SlimeKingdom");
            return zone;
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

        // 2026-09-15: texturePath now points at a standalone Sprite/Single
        // texture (one ground tile = one file, see CreateTiles above), so the
        // sprite is loaded directly by asset path instead of by name inside a
        // shared multi-sprite atlas.
        private static Tile CreateGroundTile(string texturePath, string assetPath)
        {
            EnsureGeneratedDir();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
            {
                throw new Exception($"Sprite not found at {texturePath}");
            }

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
