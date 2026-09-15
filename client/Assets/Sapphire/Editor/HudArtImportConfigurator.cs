using System;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Configures texture importer settings for the HUD/menu art added in
    /// REMEDIATION_PLAN.md Phase 2/3 (movement stick + the 4 Odin-menu
    /// assets). Split out of <see cref="ArtImportConfigurator"/> (that file
    /// was pushing past this codebase's ~500-line convention once these 5
    /// methods were added - same "keep files under ~500 lines" split already
    /// applied to <see cref="VillageHubUiBuilder"/>/
    /// <see cref="VillageHubSkillMenuBuilder"/>/<see cref="VillageHubMenuBuilder"/>).
    /// Reuses <see cref="ArtImportConfigurator"/>'s ConfigureMultiSprite/
    /// ConfigureSingleSprite (made internal for this purpose) rather than
    /// duplicating them.
    /// </summary>
    internal static class HudArtImportConfigurator
    {
        internal static void ConfigureAll()
        {
            ConfigureMovementStick();
            ConfigureMenuPanelOdin();
            ConfigureMenuSectionHeader();
            ConfigureMenuIconsSet();
            ConfigureMenuLockBadge();
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
            ArtImportConfigurator.ConfigureMultiSprite(
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
            // (2026-09-16, REMEDIATION_PLAN.md Phase 3 - replaces the retired
            // MenuPanelFrameGold.png text-list background). Border measured
            // via the flat-fill alpha/color signature (a==200, r<40,g<50,b<80)
            // sampled at 5 rows in the vertical 30%-70% band and 5 columns in
            // the horizontal 30%-70% band, all giving the exact same values
            // (no corner-decoration contamination): left 84px, right 84px
            // (flat run x=[84,708] of width 793), top 85px, bottom 93px (flat
            // run y=[85,1889] of height 1983).
            //
            // This panel is anchored (1,0)-(1,1) and vertically STRETCHED (see
            // VillageHubMenuBuilder.Build) with a fixed width of 400 - width is
            // this panel's one dimension that never changes at runtime, so
            // pixelsPerUnit is calibrated against it (nativeWidth/400), scaled
            // by referencePixelsPerUnit(100) per ArtImportConfigurator's
            // established *100 convention (Image.pixelsPerUnit =
            // sprite.pixelsPerUnit / canvas.referencePixelsPerUnit, see the note
            // on MessagePanelFrameGold in that file) - this keeps the left/
            // right border a constant ~42 canvas units regardless of how tall
            // the panel is stretched, and the resulting top/bottom border
            // (~43-47 canvas units) reads as a thin cap relative to any
            // on-screen panel height.
            ArtImportConfigurator.ConfigureSingleSprite(
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
            // tightly-cropped slice (the tight-crop convention this codebase
            // already applies elsewhere, e.g. ArtImportConfigurator.
            // ConfigureHealthBarFrame's 2026-09-16 fix) rather than importing
            // the full canvas as a Single sprite, so Image.Type.Sliced doesn't
            // stretch that transparent margin into visible empty space above/
            // below the banner. Reused for two purposes: the top-center region
            // name banner (VillageHubUiBuilder.BuildRegionNameBanner) AND each
            // Odin-menu section header (VillageHubMenuBuilder).
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
            // sprite's own width (2138/360), *100 per this convention.
            ArtImportConfigurator.ConfigureMultiSprite(
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
            // sheet) don't carry a per-slice border - none of them are used as
            // Image.Type.Sliced. This is the one Multiple-mode sheet that needs
            // 9-slice border data, so it's applied as a small follow-up pass
            // instead of widening that shared helper's signature for a single
            // caller.
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
            // convention (never assume an even 4x2 split) - zero-alpha column
            // runs at x=[0,29] (left margin), [414,481], [849,887], [1330,1354],
            // [1750,1773] (right margin) give 3 internal gap midpoints (447.5,
            // 868, 1342); the single zero-alpha row run at y=[420,446] gives the
            // row split midpoint (433). Every icon's own alpha content bbox was
            // checked and each sits within ~1-2% of its cell's geometric center
            // (well under half a canvas pixel at the 56px on-screen icon size
            // this sheet is used at - see VillageHubMenuBuilder), so no
            // per-icon pivot offset beyond the default center is needed.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ArtImportConfigurator.ConfigureMultiSprite(
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
            // VillageHubMenuBuilder.BuildOdinMenuItem). Tightly cropped to its
            // own alpha bbox (PIL: x=[239,1038] y=[50,1178], i.e. 799x1128, an
            // upright padlock silhouette) via SpriteImportMode.Multiple with a
            // single slice, same tight-crop convention as MenuSectionHeader
            // above, instead of importing the padded 1278x1230 canvas as a
            // Single sprite and relying on preserveAspect to hide the margin
            // (preserveAspect would still leave the padlock visually smaller
            // than the requested 40% box, since it centers within the
            // requested box scaled to the SPRITE's full, padded aspect ratio).
            ArtImportConfigurator.ConfigureMultiSprite(
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
    }
}
