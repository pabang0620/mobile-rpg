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
            ConfigureMovementStick();
            ConfigureMenuSectionHeader();
            // TODO(Phase 3, REMEDIATION_PLAN.md D2(b)): ConfigureMenuPanelOdin/
            // ConfigureMenuIconsSet/ConfigureMenuLockBadge land in the very next
            // commit alongside the Odin menu panel that uses them - the method
            // bodies already exist below (ready, unused) so that commit's diff
            // is just 3 call sites instead of 3 new methods.

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
            // ConfigureMenuPanelOdin below for its replacement, MenuPanelOdin.png.
            // The old ConfigureSingleSprite call for MenuPanelFrameGold.png that
            // used to live here was removed along with the source PNG (git rm,
            // confirmed no remaining references via grep).
        }

        private static void ConfigureMovementStick()
        {
            // UI: bottom-left virtual movement pad, 1438x902, 2 cells - a large
            // base ring (left) and a smaller knob (right), replacing the plain
            // built-in "Knob" UI sprite VirtualMovementPad used to reuse
            // (2026-09-16, REMEDIATION_PLAN.md Phase 2 item 8). Both are
            // Image.Type.Simple with preserveAspect (no 9-slice needed - they're
            // whole circular badges, not stretchable frames), so
            // spritePixelsPerUnit has no effect on the on-screen size (only
            // Sliced/Tiled scaling and Sprite Editor's "native size" preview
            // depend on it) - kept at 100 for consistency with the other
            // multi-sprite sheets in this file rather than because it matters
            // here.
            //
            // Measured (PIL alpha bbox, not an assumed even split - this file's
            // running convention): base ring bbox x=[90,812] y=[97,805] (pil
            // top-left origin) inside the full 902-tall left half of the image;
            // knob bbox x=[1010,1391] y=[263,639] inside the right half. Both
            // bboxes are centered in their respective cell to within ~1px
            // (base: content center (451,451) vs 902x902 cell center (451,451);
            // knob: content center (1200.5,451) vs 476x476 cell center at
            // (1200,451)), matching the background's own note that "each [is]
            // centered in its cell" - so no extra pivot offset is needed beyond
            // the default (0.5, 0.5).
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/MovementStickGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("MovementStickGold_Base", new Rect(90, 97, 722, 708), centerPivot),
                    ("MovementStickGold_Knob", new Rect(1010, 263, 381, 376), centerPivot),
                });
        }

        private static void ConfigureMenuPanelOdin()
        {
            // UI: Odin-style right-side menu panel background, 793x1983
            // portrait, decorated corners + a flat navy-alpha-200 middle
            // (2026-09-16, REMEDIATION_PLAN.md Phase 3 item 1 - replaces the
            // retired MenuPanelFrameGold.png text-list background). Border
            // measured via the flat-fill alpha/color signature (a==200,
            // r<40,g<50,b<80) sampled at 5 rows in the vertical 30%-70% band and
            // 5 columns in the horizontal 30%-70% band, all giving the exact
            // same values (no corner-decoration contamination): left 84px,
            // right 84px (flat run x=[84,708] of width 793), top 85px, bottom
            // 93px (flat run y=[85,1889] of height 1983).
            //
            // This panel is anchored (1,0)-(1,1) and vertically STRETCHED (see
            // VillageHubUiBuilder.BuildOdinMenu) with a fixed width of 400 -
            // width is this panel's one dimension that never changes at
            // runtime, so pixelsPerUnit is calibrated against it (nativeWidth/
            // 400), scaled by referencePixelsPerUnit(100) per this file's
            // established *100 convention (Image.pixelsPerUnit =
            // sprite.pixelsPerUnit / canvas.referencePixelsPerUnit, see the note
            // on MessagePanelFrameGold above) - this keeps the left/right border
            // a constant ~42 canvas units regardless of how tall the panel is
            // stretched, and the resulting top/bottom border (~43-47 canvas
            // units) reads as a thin cap relative to any on-screen panel height.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuPanelOdin.png",
                border: new Vector4(84, 93, 84, 85),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 100f * 793f / 400f);
        }

        private static void ConfigureMenuSectionHeader()
        {
            // UI: horizontal section-header banner, 2172x724 source canvas, but
            // the actual art (decorated ends + flat navy middle) only occupies a
            // 2138x281 sub-region (PIL alpha bbox x=[17,2155] y=[197,478]) - most
            // of the 724-tall canvas is transparent margin above/below the thin
            // banner shape. Sliced as SpriteImportMode.Multiple with a single
            // tightly-cropped slice (this file's tight-crop convention, e.g.
            // ConfigureHealthBarFrame's 2026-09-16 fix) rather than importing the
            // full canvas as a Single sprite, so Image.Type.Sliced doesn't
            // stretch that transparent margin into visible empty space above/
            // below the banner.
            //
            // Border measured within the cropped 2138x281 sprite: the flat-fill
            // signature (a>200, r<20,g<35,b<55) gives a clean single longest run
            // per edge - left/right from a horizontal scan at the crop's
            // vertical center (flat x run [249,1923] in un-cropped coords ->
            // relative to the crop's left edge at x=17: left=232, right=231),
            // top/bottom from a vertical scan at 4 different x positions inside
            // the flat band, all agreeing exactly (flat y run [296,398] in
            // un-cropped coords -> relative to the crop's top edge at y=197:
            // top=99, bottom=79).
            //
            // Used at a fixed width of 360 (see VillageHubUiBuilder's region-name
            // banner), so pixelsPerUnit is calibrated against the cropped
            // sprite's own width (2138/360), *100 per this file's convention.
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuSectionHeader.png",
                ppu: 100f * 2138f / 360f,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("MenuSectionHeader", new Rect(17, 246, 2138, 281), new Vector2(0.5f, 0.5f)),
                });

            // ConfigureMultiSprite's SpriteMetaData tuples (shared by every other
            // sheet in this file) don't carry a per-slice border - none of them
            // are used as Image.Type.Sliced. This is the one Multiple-mode sheet
            // that needs 9-slice border data, so it's applied as a small
            // follow-up pass instead of widening that shared helper's signature
            // for a single caller.
            ApplySingleSliceBorder(
                SapphireSceneBuilder.UiArtDir + "/MenuSectionHeader.png",
                "MenuSectionHeader",
                new Vector4(232, 79, 231, 99));
        }

        private static void ApplySingleSliceBorder(string path, string spriteName, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            SpriteMetaData[] sheet = importer.spritesheet;
            for (int i = 0; i < sheet.Length; i++)
            {
                if (sheet[i].name == spriteName)
                {
                    sheet[i].border = border;
                }
            }

            importer.spritesheet = sheet;
            importer.SaveAndReimport();
        }

        private static void ConfigureMenuIconsSet()
        {
            // UI: menu grid icons, 1774x887, 4x2 grid (row1: equipment/bag/
            // quest/settings, row2: skillbook/character-info/map/dungeon - per
            // asset naming handed off with this file, and visually confirmed).
            // Column/row boundaries measured via the zero-alpha-count gap
            // convention (this file's running principle: never assume an even
            // 4x2 split) - zero-alpha column runs at x=[0,29] (left margin),
            // [414,481], [849,887], [1330,1354], [1750,1773] (right margin) give
            // 3 internal gap midpoints (447.5, 868, 1342); the single zero-alpha
            // row run at y=[420,446] gives the row split midpoint (433). Every
            // icon's own alpha content bbox was checked and each sits within
            // ~1-2% of its cell's geometric center (well under half a canvas
            // pixel at the 56px on-screen icon size this sheet is used at - see
            // VillageHubUiBuilder.BuildOdinMenu), so no per-icon pivot offset
            // beyond the default center is needed.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuIconsSet.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("MenuIcons_Equipment", new Rect(0f, 454f, 447.5f, 433f), centerPivot),
                    ("MenuIcons_Bag", new Rect(447.5f, 454f, 420.5f, 433f), centerPivot),
                    ("MenuIcons_Quest", new Rect(868f, 454f, 474f, 433f), centerPivot),
                    ("MenuIcons_Settings", new Rect(1342f, 454f, 432f, 433f), centerPivot),
                    ("MenuIcons_SkillBook", new Rect(0f, 0f, 447.5f, 454f), centerPivot),
                    ("MenuIcons_CharacterInfo", new Rect(447.5f, 0f, 420.5f, 454f), centerPivot),
                    ("MenuIcons_Map", new Rect(868f, 0f, 474f, 454f), centerPivot),
                    ("MenuIcons_Dungeon", new Rect(1342f, 0f, 432f, 454f), centerPivot),
                });
        }

        private static void ConfigureMenuLockBadge()
        {
            // UI: single padlock icon, 1278x1230 canvas, overlaid on a locked
            // menu item's icon at 40% of the icon's own size (see
            // VillageHubUiBuilder.BuildOdinMenu). Tightly cropped to its own
            // alpha bbox (PIL: x=[239,1038] y=[50,1178], i.e. 799x1128, an
            // upright padlock silhouette) via SpriteImportMode.Multiple with a
            // single slice, same tight-crop convention as MenuSectionHeader
            // above, instead of importing the padded 1278x1230 canvas as a
            // Single sprite and relying on preserveAspect to hide the margin
            // (preserveAspect would still leave the padlock visually smaller
            // than the requested 40% box, since it centers within the
            // requested box scaled to the SPRITE's full, padded aspect ratio).
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuLockBadge.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("MenuLockBadge", new Rect(239, 52, 799, 1128), new Vector2(0.5f, 0.5f)),
                });
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

        private static void ConfigureMultiSprite(
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
