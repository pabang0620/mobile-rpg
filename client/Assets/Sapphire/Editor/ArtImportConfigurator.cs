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
            // Ground atlas: 3072x2048, 3x2 grid (3 grass + 3 dirt), each tile 1024x1024.
            // PPU = tile pixel size so one sliced sprite exactly fills one 1x1 grid cell
            // (matches Sapphire.Domain.Grid.GridWorldConversion.CellSize = 1f).
            // Max Size 256 per user instruction: the atlas renders at ~100px/tile on
            // screen (Pixel Perfect Camera assetsPPU=100 - see SapphireSceneBuilder's
            // camera setup for why), so the 1024px source is downscaled at import
            // instead of shipping full-resolution art. 256px source for a 100px
            // on-screen tile still leaves ~2.5x supersampling headroom.
            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/GroundTiles.png",
                ppu: 1024,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: 256,
                slices: new[]
                {
                    ("GroundTiles_Grass_0", new Rect(0, 1024, 1024, 1024)),
                    ("GroundTiles_Grass_1", new Rect(1024, 1024, 1024, 1024)),
                    ("GroundTiles_Grass_2", new Rect(2048, 1024, 1024, 1024)),
                    ("GroundTiles_Dirt_0", new Rect(0, 0, 1024, 1024)),
                    ("GroundTiles_Dirt_1", new Rect(1024, 0, 1024, 1024)),
                    ("GroundTiles_Dirt_2", new Rect(2048, 0, 1024, 1024)),
                });
        }

        private static void ConfigureVillagePropsAtlas()
        {
            // Village props atlas: 1536x1024, 2x2 grid, each cell 768x512.
            // PPU = cell height so each prop is ~1 grid cell tall (props are wider
            // than 1 cell by design - fence rails span slightly more than one tile).
            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/VillageProps.png",
                ppu: 512,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: new[]
                {
                    ("VillageProps_FenceStraight", new Rect(0, 512, 768, 512)),
                    ("VillageProps_FenceCornerA", new Rect(768, 512, 768, 512)),
                    ("VillageProps_FenceCornerB", new Rect(0, 0, 768, 512)),
                    ("VillageProps_Signpost", new Rect(768, 0, 768, 512)),
                },
                pivot: new Vector2(0.5f, 0f));
        }

        private static void ConfigureCharacterSheets()
        {
            // Idle sheet: 1024x1536, 4 rows (Down/Left/Right/Up) x 2 columns (frames).
            // PPU chosen so the character renders ~1.2 grid cells tall, matching the
            // walk sheet's world size below (see comment there).
            ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/MageIdleDirectional.png",
                ppu: 320,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: BuildDirectionalGridSlices(1024, 1536, columns: 2, rows: 4, cellW: 512, cellH: 384, prefix: "MageIdle"),
                pivot: new Vector2(0.5f, 0f));

            // Walk sheet: 1086x1448, 4 rows (Down/Left/Right/Up) x 3 columns (frames).
            // PPU=302 -> 362px cell / 302 ~= 1.2 world units tall, matching idle above.
            ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/MageWalk4x3-v2.png",
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: BuildDirectionalGridSlices(1086, 1448, columns: 3, rows: 4, cellW: 362, cellH: 362, prefix: "MageWalk"),
                pivot: new Vector2(0.5f, 0f));
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
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillIcons.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_ArcaneBolt", new Rect(0, 611, 643, 611)),
                    ("SkillIcons_FrostWave", new Rect(643, 611, 644, 611)),
                    ("SkillIcons_Blink", new Rect(0, 0, 643, 611)),
                    ("SkillIcons_Shield", new Rect(643, 0, 644, 611)),
                });
        }

        private static IEnumerable<(string name, Rect rect)> BuildDirectionalGridSlices(
            int textureWidth, int textureHeight, int columns, int rows, int cellW, int cellH, string prefix)
        {
            // Row order top-to-bottom in the source image: Down, Left, Right, Up.
            string[] rowNames = { "Down", "Left", "Right", "Up" };
            var result = new List<(string, Rect)>();

            for (int r = 0; r < rows; r++)
            {
                // Unity rects are bottom-up; row 0 (Down) is the topmost row in the image.
                float yBottom = textureHeight - (r + 1) * cellH;
                for (int c = 0; c < columns; c++)
                {
                    float xLeft = c * cellW;
                    string name = $"{prefix}_{rowNames[r]}_{c}";
                    result.Add((name, new Rect(xLeft, yBottom, cellW, cellH)));
                }
            }

            return result;
        }

        private static void ConfigureMultiSprite(
            string path, int ppu, FilterMode filterMode, bool mipmaps, int? maxSize,
            IEnumerable<(string name, Rect rect)> slices, Vector2? pivot = null)
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

            Vector2 usedPivot = pivot ?? new Vector2(0.5f, 0.5f);
            var metas = new List<SpriteMetaData>();
            foreach (var (name, rect) in slices)
            {
                metas.Add(new SpriteMetaData
                {
                    name = name,
                    rect = rect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = usedPivot,
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
