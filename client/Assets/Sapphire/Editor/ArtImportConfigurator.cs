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
            HudArtImportConfigurator.ConfigureAll();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 2026-09-15: one shared PPU for all 6 individually-imported ground
        // tiles (see ConfigureGroundAtlas below for why this isn't 512).
        private const float GroundTilePpu = 508f;

        private static void ConfigureGroundAtlas()
        {
            // Ground tiles: 2026-09-15 split from one shared 1536x1024 3x2
            // atlas (GroundTiles.png, kept in git history) into 6 standalone
            // 512x512 textures under Art/World/Ground/. Root cause was two
            // compounding artifacts that both showed up as a 1px dark seam at
            // tile boundaries in the orchestrator's diagnostic screenshot
            // (final2_1280_default.png, e.g. x~=207/527/447/687/1167/1247):
            //  (a) atlas bleed - all 6 cells shared one texture with
            //      bilinear filtering + Max Size 256 downscale, so a sprite's
            //      edge texel sampled a neighboring cell's edge texel across
            //      the shared atlas seam;
            //  (b) sub-pixel gaps between adjacent tile quads letting the
            //      camera background color show through at the seam.
            // Splitting into 6 separate textures with wrapMode=Clamp kills
            // (a) outright - there is no neighboring cell in the same texture
            // to bleed from. (b) is closed by importing at PPU=508 instead of
            // 512: each 512px-wide tile then renders as a 512/508 ~= 1.008
            // unit quad, ~0.4% larger than the 1x1 grid cell it's placed in,
            // so adjacent tiles overlap by that same ~0.4% and paper over any
            // sub-pixel placement gap instead of leaving the background
            // visible through it.
            foreach (string tileName in new[] { "Grass_0", "Grass_1", "Grass_2", "Dirt_0", "Dirt_1", "Dirt_2" })
            {
                ConfigureGroundTileSprite(SapphireSceneBuilder.WorldArtDir + "/Ground/" + tileName + ".png");
            }
        }

        // Standalone (non-atlas) sprite import for one ground tile texture:
        // Sprite/Single, center pivot, Clamp wrap (no neighboring cell exists
        // in the texture to bleed from), bilinear filtering, mipmaps off (a
        // ground-plane tile is always viewed at ~1:1 scale, never minified
        // enough to need mip levels - and mips would reintroduce the same
        // edge-bleed artifact this split is meant to remove), Max Size 256,
        // uncompressed, FullRect mesh (a plain rectangular tile doesn't need
        // Tight's alpha-hull trim), PPU 508 (see ConfigureGroundAtlas above).
        private static void ConfigureGroundTileSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GroundTilePpu;

            // spriteAlignment/spriteMeshType/spritePivot are not direct
            // TextureImporter properties (unlike spriteImportMode/
            // spritePixelsPerUnit/spriteBorder) - they live on
            // TextureImporterSettings and must be round-tripped via
            // Read/SetTextureSettings.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            importer.SaveAndReimport();
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
            // pixelsPerUnit is explicitly set to nativeWidth/sizeDelta.x, scaled
            // by the canvas's referencePixelsPerUnit (100) - NOT nativeWidth/
            // sizeDelta.x alone (2026-09-15 gold-tier pass got this wrong, see
            // 2026-09-16 fix below). Image.pixelsPerUnit is
            // sprite.pixelsPerUnit / canvas.referencePixelsPerUnit (Image.cs), so
            // sprite.pixelsPerUnit must equal the desired ratio times 100 to make
            // Image.pixelsPerUnit equal that ratio - otherwise it's off by 100x
            // and Image.Type.Sliced's GetAdjustedBorders (which divides border
            // AND padding by that same value) inflates both ~100x, forcing the
            // border-clamp to eat the whole rect while the un-clamped padding
            // inset stays huge, making the outer padding vert land past the
            // border verts - a negative-width "slice" that Unity 6's zero/
            // negative-dimension guard (UUM-71372) then skips for all 9 quads,
            // rendering nothing. Confirmed via Image.OnPopulateMesh producing
            // currentVertCount=0 for MainMenuButton (see docs/HANDOFF.md
            // 2026-09-16 entry) before this fix.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png",
                border: new Vector4(144, 188, 145, 167),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 100f * 1937f / 560f);

            // UI: wide pill button, 993x251 (2026-09-15, replaces the old flat
            // WideButton.png at every call site - message panel close button,
            // main menu open button, and the 7 menu list item buttons all now
            // share this one gold asset; see VillageHubUiBuilder). Reported as
            // "20px padding crop" but that claim was not trusted - re-measured
            // the actual gold-frame-to-navy-fill color transition directly (same
            // method as MessagePanelFrameGold above, narrow 40%-60% window):
            // left 114px, right 116px, top 77px, bottom 71px.
            //
            // pixelsPerUnit calibrated to nativeWidth/280 (~3.546), scaled by
            // referencePixelsPerUnit (100) - see the *100 note on
            // MessagePanelFrameGold above; without it this exact asset/border
            // combo is what produced the "메뉴 버튼 배경이 안 보인다" bug (main
            // menu open button, MenuButtonGold border sums (114+116)/280 x
            // 100 no longer fits the ~28x-inflated math, collapsing the
            // Sliced mesh to 0 vertices). 280 is the close button's width, the
            // middle of this sprite's three call-site widths (150 main menu
            // button, 280 close button, 310 menu items). Border comfortably
            // under the smallest call site (MainMenuButton, 150x72): sums to
            // ~65/150 = 43% of width, ~42/72 = 58% of height, no overlap.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png",
                border: new Vector4(114, 71, 116, 77),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 100f * 993f / 280f);

            // 2026-09-16 (Phase 3, REMEDIATION_PLAN.md D2(b)): MenuPanelFrameGold.png
            // (the old vertical text-list panel background, 7 divider lines for
            // the retired 7-item MainMenuPanel) is retired - see
            // HudArtImportConfigurator.ConfigureMenuPanelOdin for its
            // replacement, MenuPanelOdin.png. The old ConfigureSingleSprite call
            // for MenuPanelFrameGold.png that used to live here was removed
            // along with the source PNG (git rm, confirmed no remaining
            // references via grep).
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
            // (top-left origin; was 452-508 on the old asset), midpoint 504
            // (was 480) - unchanged from the 2026-09-15 pass.
            //
            // 2026-09-16: the row-only half-cell split above (Track Rect (0, 383,
            // 1774, 504), Fill Rect (0, 0, 1774, 383)) was never actually cropped
            // to the visible art - re-measuring each cell's own alpha channel
            // (PIL/numpy) found ~354px of fully-transparent margin inside the
            // 504-tall Track cell alone (alpha bbox y:[129,486] of 504, top gem
            // finial included) and a further offset in the Fill cell (alpha bbox
            // y:[18,230] of 383). Because BuildHealthBar renders the Track with
            // Image.Type.Simple (no preserveAspect), that whole half-cell
            // including its transparent margin gets stretched to fill barWidth x
            // barHeight, shrinking and off-centering the actual gold capsule
            // inside the assigned rect - the concrete mechanism behind "HP바가
            // 프레임에 안 맞음". Fix: crop both cells tightly to their own alpha
            // bounding box instead of the naive half-cell split, matching this
            // file's convention for SkillButtonFrame above. Track tight bbox
            // (bottom-up): x=15 y=400 width=1744 height=358 (aspect ~4.872,
            // BuildHealthBar's barHeight recalibrated to match, see that
            // method). Fill tight bbox (bottom-up): x=54 y=152 width=1665
            // height=213 (aspect ~7.817). Interior navy window inside the Track
            // crop was also re-measured (longest contiguous non-gold run per
            // row/column, avoiding the corner scrollwork and center gem studs)
            // at x:[182,1587] y:[216,398] (top-left origin, out of the 504-tall
            // cell) - BuildHealthBar's Fill anchors are recalibrated to this
            // window (previous anchors extended past it on every edge).
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/HealthBarFrameGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("HealthBarFrame_Track", new Rect(15, 400, 1744, 358), centerPivot),
                    ("HealthBarFrame_Fill", new Rect(54, 152, 1665, 213), centerPivot),
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

        // internal (not private): HudArtImportConfigurator (split out of this
        // file for the same "keep files under ~500 lines" reason
        // VillageHubUiBuilder was split into VillageHubSkillMenuBuilder/
        // VillageHubMenuBuilder) reuses this instead of duplicating it.
        internal static void ConfigureMultiSprite(
            string path, float ppu, FilterMode filterMode, bool mipmaps, int? maxSize,
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

        internal static void ConfigureSingleSprite(string path, Vector4 border, FilterMode filterMode, bool mipmaps, float pixelsPerUnit)
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
