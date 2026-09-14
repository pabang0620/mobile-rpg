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
            ConfigureSkillButtonFrame();
            ConfigureHealthBarFrame();
            ConfigureSkillIconsSet();

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
            // 2026-09-14 (later same day): swapped in the v4 body-stable
            // walk-cycle artwork (same 1086x1448 / 362px-cell layout, no grid
            // change needed). Re-measured per-cell alpha bounding box (center-x,
            // bottom-y) across all 12 cells and re-checked the previous grouping
            // assumption: idle/walkA/walkB no longer differ meaningfully within a
            // direction (within-row spread across the 3 poses is only 0.14-0.55
            // percentage points on both axes - noise, not a real walkB offset).
            // Each direction now gets exactly one pivot shared by all 3 poses in
            // that row - 4 pivots total, one per row, instead of the previous
            // approach of crossing 2 Y groups x 2 X groups. See BuildMageGridSlices.
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
            // UI: message panel frame, 1649x954 ornate rounded-rect border (gold
            // trim + corner gem flourishes) around a navy fill, replacing the old
            // 48x48 FantasyPanelBorder.png (2026-09-14 full UI asset replacement).
            // Border measured directly from the alpha/color transition between the
            // gold frame and the navy interior fill, sampled at several points
            // along each edge away from the corners and the small mid-edge star
            // accents (which locally read as thicker/thinner and would skew a
            // single-sample measurement): left/right stable at ~116px, top stable
            // at 181px, bottom stable at ~244-252px (this frame's bottom trim is
            // genuinely thicker than its top trim, not a measurement artifact -
            // confirmed by resampling top at 7 different x-fractions, all exactly
            // 181, vs bottom varying only 244-252 which is real interior-texture
            // noise around a thicker true border).
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MessagePanelFrame.png",
                border: new Vector4(116, 248, 116, 181),
                filterMode: FilterMode.Bilinear,
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

        private static void ConfigureSkillButtonFrame()
        {
            // UI: circular skill button frame, 1536x1024, 2 cells side by side -
            // left cell is the plain skill-slot ring, right cell is the larger
            // basic-attack ring (2026-09-14 full UI asset replacement, was the
            // Unity builtin UI/Skin/Knob.psd). Measured the alpha content of each
            // circle (column-count profile): left circle spans columns 69-631,
            // right circle spans columns 738-1488, with a shared zero-alpha gap
            // at columns 632-737. An even 768/768 half-width split would fall
            // INSIDE the right circle's own content (738-1488 straddles 768), so
            // the cell boundary is placed at the gap's midpoint (684) instead.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillButtonFrame.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillButtonFrame_Skill", new Rect(0, 0, 684, 1024), centerPivot),
                    ("SkillButtonFrame_BasicAttack", new Rect(684, 0, 852, 1024), centerPivot),
                });
        }

        private static void ConfigureHealthBarFrame()
        {
            // UI: HP bar frame, 1774x887, 2 cells stacked vertically - top cell is
            // the capsule outline/track, bottom cell is the crimson fill capsule
            // (2026-09-14, new HealthBarView scaffold - see docs/HANDOFF.md).
            // Measured via row-count alpha profile: the two cells are NOT an even
            // 443/443 vertical split - top (track) content spans rows 173-451,
            // bottom (fill) content spans rows 509-705 (image top-left origin),
            // with a shared zero-alpha gap at rows 452-508. Cell boundary placed
            // at the gap's midpoint (480, top-left origin) rather than the image's
            // literal half-height.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/HealthBarFrame.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("HealthBarFrame_Track", new Rect(0, 407, 1774, 480), centerPivot),
                    ("HealthBarFrame_Fill", new Rect(0, 0, 1774, 407), centerPivot),
                });
        }

        private static void ConfigureSkillIconsSet()
        {
            // Skill icons: 1536x1024, 3x2 grid, 6 cells (2026-09-14 full UI asset
            // replacement, merges the old 4-icon SkillIcons.png + 2-icon
            // SkillIconsExtra.png into one sheet - both source files removed).
            // Reading order (top-left to bottom-right, same convention as the old
            // 2x2 sheet) matches SkillCatalog.All's slot order plus the separate
            // basic-attack button: 기본공격/비전탄(arcane bolt)/서리파동(frost wave)
            // top row, 점멸(blink)/보호막(shield)/질주(haste) bottom row - visually
            // confirmed against each icon's artwork (staff+burst, flying shard,
            // snowflake, speed chevron, shield, winged bolt).
            //
            // Cells are NOT an even 512x512 grid - measured via alpha column/row
            // profiles: column gaps at 483-555 and 972-1028 (px, top-left origin),
            // row gap at 483-508. Cell boundaries placed at each gap's midpoint
            // (columns 519/1000, row 496) rather than the naive even thirds/halves,
            // the same "measure, don't assume equal cells" approach used above for
            // SkillButtonFrame/HealthBarFrame. Sprite names are unchanged from the
            // old two-sheet setup so SkillCatalog.cs and RadialSkillMenu need no
            // changes - only the source texture moved.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillIconsSet.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_BasicAttack", new Rect(0, 528, 519, 496), centerPivot),
                    ("SkillIcons_ArcaneBolt", new Rect(519, 528, 481, 496), centerPivot),
                    ("SkillIcons_FrostWave", new Rect(1000, 528, 536, 496), centerPivot),
                    ("SkillIcons_Blink", new Rect(0, 0, 519, 528), centerPivot),
                    ("SkillIcons_Shield", new Rect(519, 0, 481, 528), centerPivot),
                    ("SkillIcons_Haste", new Rect(1000, 0, 536, 528), centerPivot),
                });
        }

        private static IEnumerable<(string name, Rect rect, Vector2 pivot)> BuildMageGridSlices(
            int textureWidth, int textureHeight, int cellSize)
        {
            // Row order top-to-bottom in the source image: Down, Left, Right, Up.
            // Column order left-to-right: idle, walkA, walkB.
            string[] rowNames = { "Down", "Left", "Right", "Up" };
            string[] colNames = { "Idle", "WalkA", "WalkB" };

            // One pivot per direction, shared by all 3 poses in that row -
            // measured as the average alpha-bounding-box center-x/bottom-y across
            // idle/walkA/walkB (see ConfigureCharacterSheets for why no per-pose
            // split is needed with the v4 artwork). Pivot is Unity's bottom-up
            // normalized coordinate, so a value closer to 0 sits closer to the
            // cell's bottom edge.
            var pivotByRow = new Dictionary<string, Vector2>
            {
                ["Down"] = new Vector2(0.57f, 0.01f),
                ["Left"] = new Vector2(0.59f, 0.08f),
                // Side-view feet must share the same baseline.  The previous
                // zero Y pivot left the Right row's transparent bottom margin
                // above the grid centre, making rightward steps look airborne.
                ["Right"] = new Vector2(0.59f, 0.08f),
                ["Up"] = new Vector2(0.57f, 0.22f),
            };

            if (textureWidth != cellSize * colNames.Length || textureHeight != cellSize * rowNames.Length)
            {
                throw new Exception($"MageTopdownGridSheet grid size mismatch: expected {cellSize * colNames.Length}x{cellSize * rowNames.Length}, got {textureWidth}x{textureHeight}");
            }

            var result = new List<(string, Rect, Vector2)>();
            for (int r = 0; r < rowNames.Length; r++)
            {
                Vector2 pivot = pivotByRow[rowNames[r]];
                // Unity rects are bottom-up; row 0 (Down) is the topmost row in the image.
                float yBottom = textureHeight - (r + 1) * cellSize;

                for (int c = 0; c < colNames.Length; c++)
                {
                    float xLeft = c * cellSize;
                    string name = $"Mage_{rowNames[r]}_{colNames[c]}";
                    result.Add((name, new Rect(xLeft, yBottom, cellSize, cellSize), pivot));
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
