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
            // UI: message panel frame, 1937x812 ornate rounded-rect border (gold
            // trim + corner gem flourishes + mid-edge diamond studs) around a navy
            // fill (2026-09-15 gold-tier replacement of the 2026-09-14
            // MessagePanelFrame.png, same role). Border measured the same way as
            // before - color transition between the gold frame and the flat navy
            // interior fill (RGB ~(8,24,58) sampled at image center) - but sampled
            // in a narrower 40%-60% window per edge instead of scattered points,
            // specifically to sit between the corner curve (reads thicker) and the
            // mid-edge diamond studs (also read thicker where they poke into the
            // interior); median of 15+ samples per edge in that window: left
            // 144px, right 145px, top 167px, bottom 188px.
            //
            // pixelsPerUnit is explicitly set to nativeWidth/sizeDelta.x
            // (1937/560 ~= 3.459) instead of leaving the old 100 default. At
            // ppu=100 these border pixel counts convert to under 2 canvas units,
            // i.e. the ornate gold trim would render as a near-invisible sliver
            // against the panel's 560x320 on-screen size - very likely the actual
            // mechanism behind "찌그러지고 이상하다" complaints, not just outdated
            // slice coordinates. Scaling pixelsPerUnit by nativeWidth/sizeDelta.x
            // uniformly rescales the whole texture to fit the panel's width,
            // which preserves the border-to-image ratio the artist actually drew
            // (border/nativeWidth) instead of an arbitrary one.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png",
                border: new Vector4(144, 188, 145, 167),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 1937f / 560f);

            // UI: wide pill button, 993x251 (2026-09-15, replaces the old flat
            // WideButton.png at every call site - message panel close button,
            // main menu open button, and the 7 menu list item buttons all now
            // share this one gold asset; see VillageHubUiBuilder). Reported as
            // "20px padding crop" but that claim was not trusted - re-measured
            // the actual gold-frame-to-navy-fill color transition directly (same
            // method as MessagePanelFrameGold above, narrow 40%-60% window):
            // left 114px, right 116px, top 77px, bottom 71px.
            //
            // pixelsPerUnit calibrated to nativeWidth/280 (~3.546) - 280 is the
            // close button's width, the middle of this sprite's three call-site
            // widths (150 main menu button, 280 close button, 310 menu items).
            // Checked this keeps the border comfortably under the smallest call
            // site (MainMenuButton, 150x72): border sums to ~65/150 = 43% of
            // width, ~42/72 = 58% of height on both axes, no overlap/negative
            // interior - while avoiding the same too-thin-border problem the
            // 100-default caused above.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png",
                border: new Vector4(114, 71, 116, 77),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 993f / 280f);

            // UI: menu list panel frame, 1007x1230 portrait, pre-decorated with 6
            // horizontal divider lines (7 rows - matching MainMenuPanel's 7 menu
            // items) (2026-09-15, replaces MessagePanelFrame.png being reused as
            // MainMenuPanel's background - now a dedicated asset). Border
            // measured the same color-transition method, narrow window: left
            // 87px, right 88px, top 92px, bottom 90px.
            //
            // pixelsPerUnit calibrated to nativeHeight/790 (~1.557) - height is
            // this portrait panel's defining dimension; 790 is MainMenuPanel's
            // pre-responsive-fix sizeDelta.y, kept as the reference height the
            // border proportions are calibrated against even though
            // VillageHubUiBuilder.BuildMainMenu now stretches the panel's actual
            // height to fit the screen (see that method's comment) - the border
            // in canvas units stays fixed regardless of how tall the stretched
            // panel ends up.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuPanelFrameGold.png",
                border: new Vector4(87, 90, 88, 92),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 1230f / 790f);
        }

        private static void ConfigureSkillButtonFrame()
        {
            // UI: circular skill button frame, 1536x1024, 2 cells side by side -
            // left cell is the plain skill-slot ring, right cell is the larger
            // basic-attack ring (2026-09-15 gold-tier replacement of the
            // 2026-09-14 SkillButtonFrame.png, same 2-cell layout). Measured each
            // circle's own alpha bounding box (column/row profile) instead of
            // reusing the old convention of a column-only split against the full
            // 1024px canvas height: left circle bbox x[12,671] y[178,843]
            // (659x665, ~1:1.01 aspect), right circle bbox x[711,1535] y[48,902]
            // (824x854, ~1:1.04 aspect).
            //
            // The old convention (full canvas height, column-cropped only)
            // produced non-square sprite rects (684x1024 and 852x1024, ~1:1.5
            // aspect) that a square button (sizeDelta.x == sizeDelta.y,
            // Image.Type.Simple, no preserveAspect - see
            // VillageHubUiBuilder.BuildRadialButton) then stretched into a
            // visible oval - very likely a real, concrete source of the "안 맞고
            // 뭉개진다" complaint, not just outdated slice coordinates. Cropping
            // tightly to each circle's own near-square content bbox fixes that
            // distortion at the source instead of adding a compensating
            // preserveAspect flag (which would letterbox gaps inside the round
            // frame instead).
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillButtonFrame_Skill", new Rect(12, 181, 659, 665), centerPivot),
                    ("SkillButtonFrame_BasicAttack", new Rect(711, 122, 824, 854), centerPivot),
                });
        }

        private static void ConfigureHealthBarFrame()
        {
            // UI: HP bar frame, 1774x887, 2 cells stacked vertically - top cell is
            // the capsule outline/track, bottom cell is the crimson fill capsule
            // (2026-09-15 gold-tier replacement of the 2026-09-14
            // HealthBarFrame.png - identical 1774x887 canvas size and 2-cell
            // vertical layout). Row-count alpha profile gap is at rows 487-521
            // (top-left origin; was 452-508 on the old asset) - re-measured
            // rather than reused, midpoint 504 (was 480). Track Rect
            // (0, 383, 1774, 504), Fill Rect (0, 0, 1774, 383) - both full canvas
            // width, since this is a wide horizontal pill (not squeezed into a
            // square button), so the old convention of a row-only split against
            // the full width is still correct here, unlike SkillButtonFrame
            // above.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/HealthBarFrameGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("HealthBarFrame_Track", new Rect(0, 383, 1774, 504), centerPivot),
                    ("HealthBarFrame_Fill", new Rect(0, 0, 1774, 383), centerPivot),
                });
        }

        private static void ConfigureSkillIconsSet()
        {
            // Skill icons: 1536x1024, 3x2 grid, 6 cells (2026-09-15 gold-tier
            // replacement of the 2026-09-14 SkillIconsSet.png, same 3x2 layout
            // and reading order - visually re-confirmed against each icon's
            // artwork: staff+starburst/flying shard/snowflake top row,
            // chevron/shield/winged bolt bottom row, matching
            // 기본공격/비전탄/서리파동 then 점멸/보호막/질주). Sprite names unchanged
            // so SkillCatalog.cs and RadialSkillMenu need no changes.
            //
            // Re-measured rather than reused: column gaps at 520-527 and
            // 1003-1022 (px, top-left origin) - NOT clean zero-alpha bands like
            // the old asset, both have a 1-13px noise blip inside the gap from
            // thin gold connector linework between the hex icon frames, so the
            // boundary is the midpoint of the full noisy gap span (not just a
            // "first/last exact zero" average): 524 and 1013. Row split is also
            // not a clean zero-alpha gap - the minimum-density row in the
            // 400-620 window is row 502 with 83 nonzero-alpha pixels (not 0),
            // meaning the top/bottom icon rows' decorative elements touch
            // slightly; used that density-minimum row directly as the split -
            // same "measure, don't assume equal cells" principle as before,
            // applied to a noisier image.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillIconsSetGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_BasicAttack", new Rect(0, 522, 524, 502), centerPivot),
                    ("SkillIcons_ArcaneBolt", new Rect(524, 522, 489, 502), centerPivot),
                    ("SkillIcons_FrostWave", new Rect(1013, 522, 523, 502), centerPivot),
                    ("SkillIcons_Blink", new Rect(0, 0, 524, 522), centerPivot),
                    ("SkillIcons_Shield", new Rect(524, 0, 489, 522), centerPivot),
                    ("SkillIcons_Haste", new Rect(1013, 0, 523, 522), centerPivot),
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

        private static void ConfigureSingleSprite(string path, Vector4 border, FilterMode filterMode, bool mipmaps, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            importer.SaveAndReimport();
        }
    }
}
