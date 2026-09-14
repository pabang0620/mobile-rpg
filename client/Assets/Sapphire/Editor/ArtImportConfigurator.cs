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
            // 2026-09-14 measured alpha bounding box per frame (both width AND
            // height, not just height as before). Overall size check: max width
            // across all frames = 0.87 units (idle) / 0.82 units (walk), both
            // <= 1 unit - no PPU change needed. Max height = 1.20 units (idle) /
            // 1.18 units (walk), within the 1.0-1.5 target band - no PPU change
            // needed either. ppu values below (320, 302) are therefore unchanged
            // from before.
            //
            // Pivot: a single uniform (0.5, 0) pivot per sheet (previous behavior)
            // does NOT correctly center every frame - measured per-frame alpha
            // bbox shows the character art is not horizontally centered within its
            // cell (idle frames offset by up to +-0.21 units from cell-center) and,
            // more importantly, does not consistently touch the cell's bottom edge
            // (idle Up-facing frames leave a 61px / 0.19-unit gap between the
            // character's feet and the cell bottom - Down/Left/Right leave 0px; walk
            // sheet leaves 0-48px / 0-0.16 units depending on direction). With a
            // single shared pivot, this makes the character visually float above
            // the tile by a direction-dependent amount and jitter left/right
            // between animation frames - a plausible cause of "걸쳐 있는 것처럼
            // 보인다" (looks like it's straddling tiles). Fixed by giving every
            // slice its own custom pivot computed
            // from that frame's own measured bbox (center-x, bottom-y), so the
            // character's feet are pinned to the tile's floor and its body stays
            // horizontally centered on the tile for every direction and frame.
            ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/MageIdleDirectional.png",
                ppu: 320,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: BuildDirectionalGridSlices(1024, 1536, columns: 2, rows: 4, cellW: 512, cellH: 384, prefix: "MageIdle",
                    pivotsRowMajor: new[]
                    {
                        new Vector2(0.6045f, 0.0000f), new Vector2(0.3896f, 0.0000f), // Down_0, Down_1
                        new Vector2(0.6035f, 0.0000f), new Vector2(0.3926f, 0.0000f), // Left_0, Left_1
                        new Vector2(0.6309f, 0.0000f), new Vector2(0.3955f, 0.0000f), // Right_0, Right_1
                        new Vector2(0.6328f, 0.1589f), new Vector2(0.4014f, 0.1589f), // Up_0, Up_1
                    }));

            // Walk sheet: 1086x1448, 4 rows (Down/Left/Right/Up) x 3 columns (frames).
            // PPU=302 -> 362px cell / 302 ~= 1.2 world units tall, matching idle above.
            ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/MageWalk4x3-v2.png",
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: BuildDirectionalGridSlices(1086, 1448, columns: 3, rows: 4, cellW: 362, cellH: 362, prefix: "MageWalk",
                    pivotsRowMajor: new[]
                    {
                        new Vector2(0.5111f, 0.0221f), new Vector2(0.4793f, 0.0221f), new Vector2(0.4683f, 0.0221f), // Down_0..2
                        new Vector2(0.5055f, 0.0801f), new Vector2(0.4793f, 0.0801f), new Vector2(0.4710f, 0.0801f), // Left_0..2
                        new Vector2(0.5552f, 0.0000f), new Vector2(0.4931f, 0.0000f), new Vector2(0.4696f, 0.0000f), // Right_0..2
                        new Vector2(0.5510f, 0.1022f), new Vector2(0.5193f, 0.1326f), new Vector2(0.4876f, 0.1022f), // Up_0..2
                    }));
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

        private static IEnumerable<(string name, Rect rect, Vector2 pivot)> BuildDirectionalGridSlices(
            int textureWidth, int textureHeight, int columns, int rows, int cellW, int cellH, string prefix,
            Vector2[] pivotsRowMajor)
        {
            // Row order top-to-bottom in the source image: Down, Left, Right, Up.
            string[] rowNames = { "Down", "Left", "Right", "Up" };
            var result = new List<(string, Rect, Vector2)>();

            if (pivotsRowMajor.Length != rows * columns)
            {
                throw new Exception($"pivotsRowMajor length {pivotsRowMajor.Length} does not match rows*columns {rows * columns} for {prefix}");
            }

            for (int r = 0; r < rows; r++)
            {
                // Unity rects are bottom-up; row 0 (Down) is the topmost row in the image.
                float yBottom = textureHeight - (r + 1) * cellH;
                for (int c = 0; c < columns; c++)
                {
                    float xLeft = c * cellW;
                    string name = $"{prefix}_{rowNames[r]}_{c}";
                    Vector2 pivot = pivotsRowMajor[r * columns + c];
                    result.Add((name, new Rect(xLeft, yBottom, cellW, cellH), pivot));
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
