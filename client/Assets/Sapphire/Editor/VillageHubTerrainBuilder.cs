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
        private const string PrimaryAtlas = "Assets/Sapphire/Art/World/SlimeKingdomAtlas.png";

        internal static TerrainBuildResult Build()
        {
            (Tile blockerTile, Tile[] grassTiles, Tile[] dirtTiles) = CreateTiles();

            var (gridGo, groundTilemap, collisionTilemap) = CreateGridAndTilemaps();
            PopulateGroundAndCollision(groundTilemap, collisionTilemap, grassTiles, dirtTiles, blockerTile);
            TilemapGridMapBuilder gridMapBuilder = BuildGridMapBuilder(gridGo, groundTilemap, collisionTilemap);

            BuildFences(collisionTilemap, blockerTile);

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
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = -30000;

            var collisionGo = new GameObject("Collision", typeof(Tilemap));
            collisionGo.transform.SetParent(gridGo.transform);
            var collisionMap = collisionGo.GetComponent<Tilemap>();
            
            return (gridGo, groundGo.GetComponent<Tilemap>(), collisionMap);
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
                    
                    // Cross-shaped "spine" layout: one vertical main path + one side
                    // branch (smith, NPCs TBD). 2026-09-21: the old "plaza"
                    // (x13-19,y12-20) is removed - it was wider than mainPath/the branches
                    // and had no fences or real destination, so it just read as a
                    // shapeless brown smear on both flanks of the town hall (user report:
                    // "집 왼쪽에 갈색타일 없애주고"). Dirt is now only the actual walkable
                    // routes below.
                    //
                    // 2026-09-21 (west branch removed): the market branch (x8-15,y13-14)
                    // and its flanking fences are deleted outright per user instruction
                    // ("왼쪽에 있는길은 그럼 없애버려") - that whole cell range reverts to
                    // grass. Only the east smith branch remains, and even that is now
                    // clipped by the corrected underTownHall range below (see comment there).
                    bool mainPath = (x >= 15 && x <= 17 && y >= 1 && y <= 31);
                    bool smithBranch = (x >= 17 && x <= 24 && y >= 13 && y <= 14);
                    // Town hall footprint - measured from the actual sprite, not guessed.
                    // TownHallVertical.png is 1024x1024px @ 100 PPU (pivot 0.5,0.5 per its
                    // .meta), so untrimmed half-extent is 5.12 world units each way. The
                    // ACTUAL non-transparent content (alpha>10) bounding box, measured with
                    // PIL, is x_px[154,869] / y_px[21,940] (top-down file coords). Converted
                    // to world space around the sprite's placement point - CellCenter(16,16)
                    // = (16.5,16.5), then PlaceVisual() shifts Y by +scale*0.5 = +0.5, so the
                    // pivot sits at world (16.5, 17.0) - the visible content spans world
                    // X[12.92,20.07] / Y[12.71,21.90], i.e. grid columns 12-20 and rows 12-21.
                    // The OLD collision block below used dx[-5,5]/dy[0,9] (rows 16-25): X was
                    // fine (cols 11-21 already covers 12-20 with margin) but Y undershot the
                    // south edge by 4 rows (12-15 was visually building but had no collision -
                    // this is what let the market/smith branch dirt+fences at y13-14 render
                    // and be walked on top of the building's base - user report: "건물위로
                    // 타일을 깔아놓은것도 문제야"/"건물은 물리적으로 올라갈수없는거"), while
                    // overshooting the north edge by 4 rows (22-25 blocked despite no visible
                    // building there). Fixed to y >= 12 && y <= 21 to match the measured rows.
                    bool underTownHall = (x >= 11 && x <= 21 && y >= 12 && y <= 21);
                    // Detour corridor: the town hall footprint (x11-21,y12-21) fully
                    // swallows the mainPath/plaza spine (x13-19) in that band, so the
                    // only way from spawn to the gate is around the building. This adds
                    // an explicit east-side bypass instead of leaving it as unmarked
                    // grass: a vertical corridor at x22-24 (merges with smithBranch,
                    // which already occupies x17-24/y13-14, at x22-24) running the full
                    // y11-26 span where the main guide rails have a gap (rails run
                    // y1-11 and y26-30 - see BuildMainPathGuideFences), plus two short
                    // horizontal connectors at y11 and y26 (x18-24) that bridge the gap
                    // between the main rail's east column (x=18) and the corridor mouth
                    // (x=22) so the route reads as one continuous turn, not two
                    // disconnected dirt patches.
                    bool detourCorridor = (x >= 22 && x <= 24 && y >= 11 && y <= 26);
                    bool detourConnectorSouth = (x >= 18 && x <= 24 && y == 11);
                    bool detourConnectorNorth = (x >= 18 && x <= 24 && y == 26);
                    bool detourPath = detourCorridor || detourConnectorSouth || detourConnectorNorth;
                    bool onPath = (mainPath || smithBranch || detourPath) && !isBorder && !underTownHall;

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

        private static void BuildFences(Tilemap collision, Tile blocker)
        {
            var fencesRoot = new GameObject("Fences");
            Sprite fenceStraight = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceStraight");
            Sprite fenceCornerA = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "VillageProps_FenceCornerA");
            // Dedicated vertical-perspective fence art (see ArtImportConfigurator.
            // ConfigureVillageFenceVertical for the PPU derivation) - replaces the
            // old "rotate fenceStraight 90 degrees" hack for every north-south
            // fence run below (map border left/right + the main-path guide rails).
            Sprite fenceVertical = LoadNamedSprite(SapphireSceneBuilder.WorldArtDir + "/VillageFenceVertical.png", "VillageFenceVertical");

            int mapWidth = SapphireSceneBuilder.MapWidth;
            int mapHeight = SapphireSceneBuilder.MapHeight;

            for (int x = 1; x < mapWidth - 1; x++)
            {
                if (x >= 15 && x <= 17) continue; // gap for vertical path
                PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceStraight, x, 0, 0f, "Fence_Bottom_" + x);
                PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceStraight, x, mapHeight - 1, 0f, "Fence_Top_" + x);
            }

            for (int y = 1; y < mapHeight - 1; y++)
            {
                PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceVertical, 0, y, 0f, "Fence_Left_" + y);
                PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceVertical, mapWidth - 1, y, 0f, "Fence_Right_" + y);
            }

            PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceCornerA, 0, 0, 0f, "Fence_Corner_BL");
            PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceCornerA, mapWidth - 1, 0, 90f, "Fence_Corner_BR");
            PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceCornerA, 0, mapHeight - 1, 270f, "Fence_Corner_TL");
            PlaceFenceWithCollision(fencesRoot.transform, collision, blocker, fenceCornerA, mapWidth - 1, mapHeight - 1, 180f, "Fence_Corner_TR");

            BuildMainPathGuideFences(fencesRoot.transform, collision, blocker, fenceVertical);
            BuildDetourGuideFences(fencesRoot.transform, collision, blocker, fenceVertical, fenceStraight);
            // BuildBranchGuideFences removed 2026-09-21: the west (market) branch and its
            // fences are deleted outright (user instruction), and the east (smith) branch's
            // guide fences (x18-21 @ y12/y15) sit entirely inside the corrected town hall
            // collision footprint (x11-21,y12-21 - see underTownHall above), so they would
            // render clipped into the building. The building's own solid collision already
            // forms the west wall of the remaining detour corridor (x22-24) at those rows,
            // so no separate fence sprite is needed there.
        }

        // Flanks the vertical spine (mainPath, x15-17) with fences on x=14/x=18 so the
        // player cannot wander off the dirt path into open grass - this is the "울타리로
        // 길을 만든다" requirement. The gap between y11-26 (skipped here) is where the
        // town hall footprint / branch dirt takes over - BuildLandmarks() already
        // solid-blocks x11-21/y12-21 for the town hall, so fence sprites there would
        // double up with (and visually clash against) the building art. Both segments
        // below connect with zero gap to the map border fences BuildFences() already
        // places at x=14/x=18, y=0 and y=mapHeight-1 (only x15-17 is skipped there), so
        // the corridor reads as continuous from spawn at y0 up to the gate at y31.
        //
        // x=18's two runs stop at y=10 / start at y=27 instead of the symmetric 11/26 that
        // x=14 uses. Bug fix 2026-09-21: the old range (1-11 and 26-30 for BOTH columns)
        // placed a fence+collision blocker directly on (18,11) and (18,26) - but those are
        // exactly the detourConnectorSouth/North doorway cells (see PopulateGroundAndCollision)
        // that let the player step off the spine into the east detour corridor. That fence
        // silently sealed the doorway shut right next to the town hall's east side - this was
        // the "집 우측에 하나 잘못 놓여져있는것같네" bug. x=14 has no such doorway on the west
        // side, so its range is unchanged. Ending x=18 at y=10/starting at y=27 leaves (18,11)
        // and (18,26) fully open, and the resulting caps at (18,10)/(18,27) line up with
        // BuildDetourGuideFences' x19-24 caps on the same rows, reading as one continuous wall
        // with a single doorway gap rather than two disconnected fence runs.
        private static void BuildMainPathGuideFences(Transform parent, Tilemap collision, Tile blocker, Sprite fenceVertical)
        {
            PlaceMainRailColumn(parent, collision, blocker, fenceVertical, 14, 1, 11, "Fence_MainL_");
            PlaceMainRailColumn(parent, collision, blocker, fenceVertical, 18, 1, 10, "Fence_MainR_");
            PlaceMainRailColumn(parent, collision, blocker, fenceVertical, 14, 26, 30, "Fence_MainL_");
            PlaceMainRailColumn(parent, collision, blocker, fenceVertical, 18, 27, 30, "Fence_MainR_");
        }

        // Gap rule: every y where y % RailGapInterval == 0 is left open (no fence
        // sprite, no collision tile) so the player can step sideways off the main
        // spine into the flanking grass at that row - both the x=14 and x=18 columns
        // use the same y values so each gap forms a full doorway straight across the
        // corridor, not a staggered pinch point. Interval of 4 keeps 2-3 tile fenced
        // segments between openings, matching "3~4칸마다 1칸" - the rail still reads
        // as a continuous guide, it just isn't an unbroken wall.
        private const int RailGapInterval = 4;

        private static void PlaceMainRailColumn(Transform parent, Tilemap collision, Tile blocker, Sprite sprite, int x, int yStart, int yEnd, string prefix)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                if (y % RailGapInterval == 0) continue; // gap: player can cross into the side grass here
                PlaceFenceWithCollision(parent, collision, blocker, sprite, x, y, 0f, prefix + y);
            }
        }

        // 2026-09-21 (revised): the original west-side (x=21) rail only had room for two
        // isolated tiles (y12, y15 - everything else was excluded by the connector rows,
        // the smithBranch dirt, or the town hall footprint already blocking that edge) and
        // read as two disconnected fence scraps rather than a guide. Replaced with a single
        // unbroken rail on the corridor's OUTER (east, x=25) edge plus short horizontal
        // "framing" fences at the two turns, so the whole bypass reads as one continuous
        // route instead of a stub:
        //   x=25, y12-23  - east rail, reuses PlaceMainRailColumn so it gets the exact same
        //                   RailGapInterval=4 dashed pattern as the main spine rails
        //                   (visual consistency). x=25 sits one tile outside detourCorridor
        //                   (x22-24) so it never overlaps the dirt tiles. Capped at y=23
        //                   (not 25) 2026-09-21: yEnd=25 landed one tile past the gap at
        //                   y=24, leaving a single orphaned fence at (25,25) with no
        //                   neighbor on either side (24 is a gap, 26 is past yEnd) - read
        //                   as one stray misplaced fence sticking out near the building's
        //                   corner (user report: "건물 바로 오른쪽아래에 울타리가 1개
        //                   잘못 놓여져있는거같은데"). y=23 ends the rail cleanly on the
        //                   last full 3-tile segment (21-22-23) instead.
        //   y=10, x19-24  - horizontal cap just above detourConnectorSouth (y=11, x18-24),
        //                   frames the south turn. x=18 is excluded because
        //                   PlaceMainRailColumn(..., 18, 1, 10, ...) already places a fence
        //                   there (y=10 is that column's last row and isn't a gap row), so
        //                   adding another sprite at the same cell would double it up.
        //   y=27, x19-24  - same framing for the north turn, mirrored below
        //                   detourConnectorNorth (y=26, x18-24); x=18 excluded for the same
        //                   reason (PlaceMainRailColumn(..., 18, 27, 30, ...) already covers it).
        private static void BuildDetourGuideFences(Transform parent, Tilemap collision, Tile blocker, Sprite fenceVertical, Sprite fenceStraight)
        {
            PlaceMainRailColumn(parent, collision, blocker, fenceVertical, 25, 12, 23, "Fence_Detour_East_");

            for (int x = 19; x <= 24; x++)
            {
                PlaceFenceWithCollision(parent, collision, blocker, fenceStraight, x, 10, 0f, "Fence_Detour_SouthCap_" + x);
                PlaceFenceWithCollision(parent, collision, blocker, fenceStraight, x, 27, 0f, "Fence_Detour_NorthCap_" + x);
            }
        }

        private static void BuildLandmarks(Tilemap collision, Tile blocker, List<InteractableZone> zones)
        {
            Transform root = new GameObject("VillageLandmarks").transform;

            // Welcome sign (bottom entrance)
            BuildInteractable(root, collision, blocker, zones, SapphireSceneBuilder.WorldArtDir + "/VillageProps.png", "signpost", "VillageProps_Signpost", SapphireSceneBuilder.SignX, SapphireSceneBuilder.SignY,
                "초보 모험가의 마을, 사파이어 타운. 마을회관은 굳게 잠겨 있다 - 마을을 돌아 북쪽 관문으로 가면 슬라임 숲으로 향할 수 있다.", null, 1.0f);

            // Village Props (placed logically on the expanded dirt plaza/grass edges)
            // Town Hall (Large generated building)
            string townHallPath = SapphireSceneBuilder.WorldArtDir + "/TownHallVertical.png";
            BuildInteractable(root, collision, blocker, zones, townHallPath, "town_hall", "TownHallVertical", 16, 16,
                "굳게 잠겨 있다. 지금은 들어갈 수 없다.", null, 1.0f);
            
            // Collision footprint, measured (not guessed) from the sprite - see the
            // underTownHall comment in PopulateGroundAndCollision for the full pixel-to-world
            // derivation. Visible content covers grid columns 12-20 / rows 12-21 around the
            // (16,16) placement; dx[-5,5]/dy[-4,5] (cols 11-21, rows 12-21) covers that with a
            // 1-column margin on X and an exact match on Y (was dy[0,9]/rows 16-25, which
            // undershot the south edge by 4 rows and overshot the north edge by 4 rows).
            for (int dx = -5; dx <= 5; dx++) {
                for (int dy = -4; dy <= 5; dy++) {
                    // Leave a tiny space for the player to stand right at the door - the
                    // south-most row of the footprint, facing the spawn/south side.
                    if (dx == 0 && dy == -4) continue;
                    collision.SetTile(new Vector3Int(16 + dx, 16 + dy, 0), blocker);
                }
            }
            // NOTE: a decorative "Slime2_Hedge" prop used to be placed near (8,5) here.
            // Removed 2026-09-21: SlimeKingdomProps2.png.meta's sprite rect for
            // "Slime2_Hedge" (x1086,y362,362x362) is misaligned in the source atlas - it
            // straddles the neighboring sprite above (a gold dome/trophy shape) and the one
            // below (blue gem icons), so the hedge always rendered with a clipped foreign
            // object floating above it. Re-slicing the atlas rect is an art-import fix, not
            // a placement fix, and the hedge was pure decoration (no InteractableZone), so
            // per user instruction it is removed outright rather than left broken.

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

        private static void PlaceFenceWithCollision(Transform parent, Tilemap collision, Tile blocker, Sprite sprite, int x, int y, float rotationZ, string name)
        {
            PlaceFence(parent, sprite, x, y, rotationZ, name);
            collision.SetTile(new Vector3Int(x, y, 0), blocker);
        }

        // NOTE 2026-09-21 (resolved): a pivot-rotation compensation was tried
        // here for the vertical (rotationZ=90) placements and reverted. Root
        // cause per user review was not a pivot/rotation math bug -
        // VillageProps_FenceStraight is a horizontal-only piece of art
        // (top-down perspective), and rotating it 90 degrees to fake a
        // vertical rail was fundamentally the wrong asset, not a wrong
        // transform. Resolution: VillageFenceVertical.png, a dedicated
        // vertical-perspective sprite, now covers every north-south fence run
        // (Fence_Left/Right, PlaceMainRailColumn) at rotationZ=0 - see the
        // fenceVertical load in BuildFences and ArtImportConfigurator.
        // ConfigureVillageFenceVertical for the PPU derivation.
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

