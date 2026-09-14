using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Configures texture importer settings (slicing, PPU, filtering, borders)
    /// for every source art asset the VillageHub scene depends on. Split out of
    /// <see cref="SapphireSceneBuilder"/> so scene-construction and art-import
    /// concerns don't live in the same 500+ line file. Safe to re-run: it only
    /// (re)writes importer settings, it does not create/delete assets.
    /// </summary>
    internal static class ArtImportConfigurator
    {
        internal static void ConfigureArtImportSettings()
        {
            ConfigureGroundAtlas();
            ConfigureVillagePropsAtlas();
            ConfigureCharacterSheets();
            ConfigureUiFrames();
            ConfigureSkillIcons();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureGroundAtlas()
        {
            // Ground atlas: 2026-09-14 regenerated from scratch (GroundTiles.png
            // replaced, see docs/DECISIONS.md) after discovering the previous
            // "hq" 1024x1024-cell atlas had each cell internally composed of four
            // different 512x512 sub-images (real content difference, not a
            // seam-only artifact - measured cross-quadrant mean abs RGB diff ~39-49
            // on the old asset). The workaround of slicing only a 512x512 sub-rect
            // per cell (kept in git history) avoided the symptom without fixing the
            // source asset, and is no longer needed.
            //
            // New atlas: 1536x1024, 3x2 grid (3 grass + 3 dirt), each cell a true
            // 512x512 whole tile generated as one continuous texture. Verified
            // internally uniform (cross-quadrant mean abs diff ~17-25 on the final
            // seamless-processed asset, in the same range as the known-good
            // pre-regen reference asset ~25, well below the broken asset's ~39-49)
            // and seamless when tiled (wrap-around edge-vs-interior-baseline ratio
            // ~1.0-1.1x after the roll+blend seamless-ify pass, vs ~1.8-2.4x on the
            // raw unprocessed generation). Whole-cell slicing restored - ppu=512
            // (cell height/width) so each cell fills exactly 1 world unit, matching
            // pre-bugfix behavior now that the source is actually correct.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png",
                ppu: 512,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: 256,
                slices: new[]
                {
                    // Top row = grass (y=512..1024), bottom row = dirt (y=0..512).
                    ("GroundTiles_Grass_0", new Rect(0, 512, 512, 512), centerPivot),
                    ("GroundTiles_Grass_1", new Rect(512, 512, 512, 512), centerPivot),
                    ("GroundTiles_Grass_2", new Rect(1024, 512, 512, 512), centerPivot),
                    ("GroundTiles_Dirt_0", new Rect(0, 0, 512, 512), centerPivot),
                    ("GroundTiles_Dirt_1", new Rect(512, 0, 512, 512), centerPivot),
                    ("GroundTiles_Dirt_2", new Rect(1024, 0, 512, 512), centerPivot),
                });
        }

        private static void ConfigureVillagePropsAtlas()
        {
            // Village props atlas: 1536x1024, 2x2 grid, each cell 768x512.
            // PPU = cell height so each prop is ~1 grid cell tall (props are wider
            // than 1 cell by design - fence rails span slightly more than one tile).
            var bottomCenterPivot = new Vector2(0.5f, 0f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/VillageProps.png",
                ppu: 512,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: new[]
                {
                    ("VillageProps_FenceStraight", new Rect(0, 512, 768, 512), bottomCenterPivot),
                    ("VillageProps_FenceCornerA", new Rect(768, 512, 768, 512), bottomCenterPivot),
                    ("VillageProps_FenceCornerB", new Rect(0, 0, 768, 512), bottomCenterPivot),
                    ("VillageProps_Signpost", new Rect(768, 0, 768, 512), bottomCenterPivot),
                });
        }

        private static void ConfigureCharacterSheets()
        {
            // 2026-09-14: replaced the two separate idle (MageIdleDirectional.png)
            // and walk (MageWalk4x3-v2.png) sheets with a single unified sheet,
            // MageTopdownGridSheet.png (1086x1448, 3 columns x 4 rows, 362x362
            // cells). Columns are idle/walkA/walkB; rows are Down/Left/Right/Up
            // (see BuildMageGridSlices). PPU=302 is carried over unchanged from
            // the old walk sheet (same 362px cell size -> 362/302 ~= 1.2 world
            // units tall, within the previously-verified 1.0-1.5 unit target band).
            //
            // Pivot: measured per-cell alpha bounding box (center-x, bottom-y)
            // across all 12 cells instead of hardcoding one pivot per cell
            // (12 one-off values, previous approach). The measurements cluster
            // into just two independent groups instead of being random per-cell:
            //
            // - Foot baseline (pivot Y): Down/Left/Right feet sit at ~97-100% down
            //   the cell and are within a fraction of a percentage point of each
            //   other - one shared "front" pivot Y covers all three. Up (the only
            //   back-facing row) sits higher, ~90% down the cell (the robe/cloak
            //   drawn from behind extends lower into the frame) - one shared
            //   "back" pivot Y covers it alone.
            // - Horizontal center (pivot X): idle and walkA columns are both
            //   close to the cell's horizontal center (~49-57%) regardless of
            //   direction, so they share one "normal" pivot X. walkB is
            //   consistently offset left by ~5-12 percentage points in every
            //   direction, so it gets its own "walkB" pivot X.
            //
            // That gives 2 (Y groups) x 2 (X groups) = 4 pivot combinations total,
            // reused across all 12 cells by (row, column) group membership - see
            // BuildMageGridSlices.
            ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/MageTopdownGridSheet.png",
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: BuildMageGridSlices(1086, 1448, cellSize: 362));
        }

        private static void ConfigureUiFrames()
        {
            // UI: small 48x48 ornate frame. Measured alpha shows a 2px border line
            // inset ~4-5px, with corner flourishes extending to ~10px - border=10
            // keeps the whole corner ornament fixed while edges/middle stretch.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/FantasyPanelBorder.png",
                border: new Vector4(10, 10, 10, 10),
                filterMode: FilterMode.Point,
                mipmaps: false);

            // UI: wide pill button, 2149x732. Border measured from the alpha
            // channel's flat-wall position (left=137/right=138 px in, corner
            // curve ends ~y=316/316 from top/bottom).
            ConfigureSingleSprite(
                SapphireSceneBuilder.RootArtDir + "/WideButton.png",
                border: new Vector4(137, 316, 138, 316),
                filterMode: FilterMode.Bilinear,
                mipmaps: false);
        }

        private static void ConfigureSkillIcons()
        {
            // Skill bar icons: 1287x1222, 2x2 grid (measured via alpha bounding box per
            // quadrant - each icon sits well clear of the midlines, so an equal split is
            // safe). Reading order matches docs/planning/02_SYSTEM_CONTRACTS.md's skill
            // table: arcane bolt (top-left) / frost wave (top-right) / blink (bottom-left)
            // / shield (bottom-right). Plain (non-sliced) icons for a UI Image, so PPU is
            // cosmetic only.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillIcons.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_ArcaneBolt", new Rect(0, 611, 643, 611), centerPivot),
                    ("SkillIcons_FrostWave", new Rect(643, 611, 644, 611), centerPivot),
                    ("SkillIcons_Blink", new Rect(0, 0, 643, 611), centerPivot),
                    ("SkillIcons_Shield", new Rect(643, 0, 644, 611), centerPivot),
                });
        }

        private static IEnumerable<(string name, Rect rect, Vector2 pivot)> BuildMageGridSlices(
            int textureWidth, int textureHeight, int cellSize)
        {
            // Row order top-to-bottom in the source image: Down, Left, Right, Up.
            // Column order left-to-right: idle, walkA, walkB.
            string[] rowNames = { "Down", "Left", "Right", "Up" };
            string[] colNames = { "Idle", "WalkA", "WalkB" };

            // Pivot Y group: front-facing rows (Down/Left/Right) share one value,
            // the back-facing row (Up) gets its own - see ConfigureCharacterSheets
            // for the measurement this is based on. Pivot is Unity's bottom-up
            // normalized coordinate, so "closer to the cell's bottom edge" == "closer to 0".
            const float frontPivotY = 0.01f;
            const float backPivotY = 0.10f;

            // Pivot X group: idle/walkA share one centered value, walkB shares a
            // separate value offset left of center.
            const float normalPivotX = 0.50f;
            const float walkBPivotX = 0.44f;

            if (textureWidth != cellSize * colNames.Length || textureHeight != cellSize * rowNames.Length)
            {
                throw new Exception($"MageTopdownGridSheet grid size mismatch: expected {cellSize * colNames.Length}x{cellSize * rowNames.Length}, got {textureWidth}x{textureHeight}");
            }

            var result = new List<(string, Rect, Vector2)>();
            for (int r = 0; r < rowNames.Length; r++)
            {
                bool isBackRow = rowNames[r] == "Up";
                float pivotY = isBackRow ? backPivotY : frontPivotY;
                // Unity rects are bottom-up; row 0 (Down) is the topmost row in the image.
                float yBottom = textureHeight - (r + 1) * cellSize;

                for (int c = 0; c < colNames.Length; c++)
                {
                    bool isWalkBColumn = colNames[c] == "WalkB";
                    float pivotX = isWalkBColumn ? walkBPivotX : normalPivotX;
                    float xLeft = c * cellSize;
                    string name = $"Mage_{rowNames[r]}_{colNames[c]}";
                    result.Add((name, new Rect(xLeft, yBottom, cellSize, cellSize), new Vector2(pivotX, pivotY)));
                }
            }

            return result;
        }

        private static void ConfigureMultiSprite(
            string path, int ppu, FilterMode filterMode, bool mipmaps, int? maxSize,
            IEnumerable<(string name, Rect rect, Vector2 pivot)> slices)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            if (maxSize.HasValue)
            {
                importer.maxTextureSize = maxSize.Value;
            }

            var metas = new List<SpriteMetaData>();
            foreach (var (name, rect, pivot) in slices)
            {
                metas.Add(new SpriteMetaData
                {
                    name = name,
                    rect = rect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                });
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
        }

        private static void ConfigureSingleSprite(string path, Vector4 border, FilterMode filterMode, bool mipmaps)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            importer.SaveAndReimport();
        }
    }
}
