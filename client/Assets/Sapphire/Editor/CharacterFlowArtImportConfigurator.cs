using System;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Import settings for the Login/CharacterSelect/CharacterCreate art
    /// (Art/UI/Title/*.png). TitleBackground/TitleLogo/PortraitMage/
    /// PortraitWarrior are Image.Type.Simple (no border needed - see
    /// ConfigureFullSprite). InputFieldFrame and the 2026-09-16 v2 title-kit
    /// assets below are 9-sliced.
    /// </summary>
    internal static class CharacterFlowArtImportConfigurator
    {
        internal const string TitleArtDir = "Assets/Sapphire/Art/UI/Title";

        // InputFieldFrame.png (1080x180, single flat-fill rounded rect):
        // inward-scan border settles at 22px on all 4 edges (single outline,
        // no inner accent line to conflate with) - build_ui_kit.py's
        // build_input_field_frame, re-measured 2026-09-15 (gemless
        // MapleStory-M rebuild).
        private static readonly Vector4 InputFieldFrameBorder = new Vector4(22, 22, 22, 22);

        // Target on-screen widths these two 9-sliced frames are actually
        // built at (see LoginUiBuilder/CharacterSelectUiBuilder/
        // CharacterCreateUiBuilder) - pixelsPerUnit is calibrated against
        // this per ArtImportConfigurator's established *100 convention
        // (Image.pixelsPerUnit = sprite.pixelsPerUnit / canvas.referencePixelsPerUnit).
        internal const float InputFieldFrameTargetWidth = 360f;

        // 2026-09-16 (premium select/create/login rebuild, tools/ui_kit/
        // build_title_kit_v2.py): 11 new dark-navy/sapphire-glow assets
        // replacing the old beige CharacterSlotFrame.png (git rm'd once this
        // file + the two scene builders switched over and grep confirmed
        // zero remaining references). Every one of these follows the same
        // SCALE_V2=3
        // native=target*3 convention as the existing v3 beige kit
        // (ArtImportConfigurator.UiKitV3PixelsPerUnit=300 applies directly -
        // see that constant's doc comment for why 1 target px == 1 canvas
        // unit at this ppu), so borders below are given in NATIVE px (as the
        // generator script's own constants state) and every call here uses
        // the shared UiKitV3PixelsPerUnit instead of a per-asset dynamic
        // formula.
        //
        // CharacterSlotFrameV2 border: left/right = FRAME_BORDER(16 target)*3
        // = 48 native (generator's own uniform 9-slice value, safely clears
        // the FRAME_RADIUS=10 target corner radius). Top/bottom are NOT the
        // generator's own FRAME_BORDER (16 target/48 native) - that value is
        // only large enough for a plain 9-slice, not to protect the two
        // baked features that must stay pixel-fixed regardless of on-screen
        // card height (CharacterSelectSceneBuilder uses 340, CharacterCreate
        // SceneBuilder uses 250 - see build_slot_frame's own comment "height
        // is a 9-slice design height... both stretch the same borders fine"
        // which undersells this): the top badge notch (BADGE_NOTCH_CY=34 +
        // FRAME_MARGIN=4 = 38 target center, BADGE_NOTCH_R=30 target radius
        // + ~1.5 target ring => bottom edge ~69.5 target) needs a top border
        // of at least 70 target/210 native, and the divider glow line
        // (divider_y = FRAME_H(400) - NAMEPLATE_BAND_H(76) = 324 target from
        // top = 76 target from the bottom edge) needs a bottom border of at
        // least 76 target/228 native. Chosen 72/90 target (216/270 native)
        // give small safety buffers on both and, not coincidentally, sum to
        // exactly the on-screen footer budget CharacterSelectSceneBuilder's
        // nameplate+select/delete-button stack needs (see that file's own
        // layout comment) - the region below the divider is flat gradient
        // fill in the source art (no baked content), so widening the
        // "fixed" bottom zone past the generator's own reserved band is
        // purely a rendering choice, not a content risk.
        private static readonly Vector4 SlotFrameBorder = new Vector4(48, 270, 48, 216);
        // Empty-slot variant has no notch/divider (build_slot_frame(empty=true)
        // is a plain dashed rounded rect) - the generator's own FRAME_BORDER
        // (16 target/48 native) is already enough to protect FRAME_RADIUS on
        // all 4 edges, so this one stays uniform.
        private static readonly Vector4 SlotFrameEmptyBorder = new Vector4(48, 48, 48, 48);
        // NameplateBar: NAMEPLATE_BORDER_LR=14/NAMEPLATE_BORDER_TB=12 target *3.
        private static readonly Vector4 NameplateBorder = new Vector4(42, 36, 42, 36);
        // LoginPortalFrame: generator's own PORTAL_BORDER is 20 target/60
        // native, but the 4 corner "rune" ticks sit at inset=13 target +
        // tick_r=4.5 target => outer edge ~17.5 target/52.5 native from each
        // corner - 72 native (24 target) keeps them safely inside the fixed
        // zone with headroom for the tick's own soft blur.
        private static readonly Vector4 LoginPortalFrameBorder = new Vector4(72, 72, 72, 72);
        // ButtonSelectV2/ButtonDeleteV2 hex-cut corner: BTN_CUT_T = BTN_CELL_H
        // (40 target) * 0.30 = 12 target/36 native - 40 native border buffers it.
        private static readonly Vector4 SelectDeleteButtonBorder = new Vector4(40, 40, 40, 40);
        // ButtonCreateV2 hex-cut corner: CREATE_CUT_T = CREATE_CELL_H
        // (56 target) * 0.30 = 16.8 target/50.4 native - 56 native buffers it.
        private static readonly Vector4 CreateButtonBorder = new Vector4(56, 56, 56, 56);

        // 2026-09-16 (login input/button redesign task): NicknameInputFieldV2
        // (2172x408, single rounded-pill frame with a soft blue neon glow
        // outline, replaces InputFieldFrame.png at this one call site only -
        // InputFieldFrame.png itself stays for CharacterCreateSceneBuilder's
        // nickname field, still referenced there). PIL corner/glow scan (both
        // a column scan fixing x and sweeping y, and a row scan fixing y and
        // sweeping x) finds the rounded corner + its glow bleed fully
        // resolving into the flat edge only by ~x=90-150px from each edge
        // (softer/wider than a plain sharp rounded-rect corner because of the
        // glow) - 130 native px on all 4 sides comfortably clears that
        // measured range with a small buffer, reusing ConfigureSlicedSprite's
        // existing native-width/targetWidth PPU formula (same one
        // InputFieldFrame.png already uses) rather than a new one, and the
        // same InputFieldFrameTargetWidth(360) so the on-screen control size
        // LoginSceneBuilder builds it at is unchanged.
        private static readonly Vector4 NicknameInputFieldV2Border = new Vector4(130, 130, 130, 130);

        // 2026-09-16 (login input/button redesign task): LoginStartButtonV2
        // (1580x250, 2 cells of 750x250 with an 80px gutter - Normal/Pressed,
        // same sheet convention ConfigureTwoCellButton already handles for
        // ButtonSelectV2/ButtonDeleteV2/ButtonCreateV2 above). This is a true
        // pointed hexagon (not a rounded-rect-with-cut-corners like those
        // three), so its border needs to clear the point apex rather than a
        // small corner-cut: per-cell PIL scan for the leftmost/rightmost
        // alpha column at every row finds the apex at local x=114/631 in the
        // Normal cell (750-wide) and a wider x=75/674 in the (differently-
        // padded) Pressed cell - 120 native px left/right clears both with a
        // small buffer. Top/bottom is a flat, uncurved edge starting at
        // y~47-51 in both cells (PIL column scan across the flat middle
        // width), so the smaller 20px border the task recommended is safe
        // there (that whole 20px band is transparent padding either way -
        // stretching it changes nothing visible).
        private static readonly Vector4 LoginStartButtonV2Border = new Vector4(120, 20, 120, 20);

        internal static void ConfigureAll()
        {
            ConfigureFullSprite(TitleArtDir + "/TitleBackground.png");
            ConfigureFullSprite(TitleArtDir + "/TitleLogo.png");
            ConfigureFullSprite(TitleArtDir + "/PortraitMage.png");
            ConfigureFullSprite(TitleArtDir + "/PortraitWarrior.png");

            ConfigureSlicedSprite(TitleArtDir + "/InputFieldFrame.png", InputFieldFrameBorder, InputFieldFrameTargetWidth);

            ConfigureFullSpriteV3(TitleArtDir + "/CharacterPedestal.png");
            ConfigureFullSpriteV3(TitleArtDir + "/ClassBadgeMage.png");
            ConfigureFullSpriteV3(TitleArtDir + "/ClassBadgeWarrior.png");
            ConfigureFullSpriteV3(TitleArtDir + "/CharacterCreateSpotlight.png");

            ArtImportConfigurator.ConfigureSingleSprite(TitleArtDir + "/CharacterSlotFrameV2.png", SlotFrameBorder, FilterMode.Bilinear, mipmaps: false, pixelsPerUnit: ArtImportConfigurator.UiKitV3PixelsPerUnit);
            ArtImportConfigurator.ConfigureSingleSprite(TitleArtDir + "/CharacterSlotFrameEmptyV2.png", SlotFrameEmptyBorder, FilterMode.Bilinear, mipmaps: false, pixelsPerUnit: ArtImportConfigurator.UiKitV3PixelsPerUnit);
            ArtImportConfigurator.ConfigureSingleSprite(TitleArtDir + "/NameplateBar.png", NameplateBorder, FilterMode.Bilinear, mipmaps: false, pixelsPerUnit: ArtImportConfigurator.UiKitV3PixelsPerUnit);
            ArtImportConfigurator.ConfigureSingleSprite(TitleArtDir + "/LoginPortalFrame.png", LoginPortalFrameBorder, FilterMode.Bilinear, mipmaps: false, pixelsPerUnit: ArtImportConfigurator.UiKitV3PixelsPerUnit);

            ConfigureTwoCellButton(TitleArtDir + "/ButtonSelectV2.png", 288, 120, 72, SelectDeleteButtonBorder);
            ConfigureTwoCellButton(TitleArtDir + "/ButtonDeleteV2.png", 288, 120, 72, SelectDeleteButtonBorder);
            ConfigureTwoCellButton(TitleArtDir + "/ButtonCreateV2.png", 480, 168, 72, CreateButtonBorder);

            ConfigureSlicedSprite(TitleArtDir + "/NicknameInputFieldV2.png", NicknameInputFieldV2Border, InputFieldFrameTargetWidth);
            ConfigureTwoCellButton(TitleArtDir + "/LoginStartButtonV2.png", 750, 250, 80, LoginStartButtonV2Border);
        }

        // Full-canvas Simple sprite for the new v2 title-kit assets - same
        // shape as ConfigureFullSprite below but pixelsPerUnit doesn't matter
        // for Image.Type.Simple sprites either way (see that method's own
        // comment), so this just uses the shared v3 constant for consistency
        // with the rest of this asset generation batch instead of the older
        // fixed-100 convention.
        private static void ConfigureFullSpriteV3(string path)
        {
            ArtImportConfigurator.ConfigureSingleSprite(
                path,
                border: Vector4.zero,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: ArtImportConfigurator.UiKitV3PixelsPerUnit);
        }

        // Normal/Pressed 2-cell sheet, same layout convention build_title_kit_v2.py's
        // build_button_sheet uses for all 3 new buttons: [Normal][transparent
        // gutter][Pressed], each cell native cellW x cellH, gutter native width
        // given explicitly (not assumed) since it differs between the
        // 96-target (select/delete) and 160-target (create) button families.
        private static void ConfigureTwoCellButton(string path, int cellW, int cellH, int gutter, Vector4 border)
        {
            var centerPivot = new Vector2(0.5f, 0.5f);
            const string normalName = "Normal";
            const string pressedName = "Pressed";
            ArtImportConfigurator.ConfigureMultiSprite(
                path,
                ppu: ArtImportConfigurator.UiKitV3PixelsPerUnit,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    (normalName, new Rect(0, 0, cellW, cellH), centerPivot),
                    (pressedName, new Rect(cellW + gutter, 0, cellW, cellH), centerPivot),
                });
            ArtImportConfigurator.ApplySingleSliceBorder(path, normalName, border);
            ArtImportConfigurator.ApplySingleSliceBorder(path, pressedName, border);
        }

        // Image.Type.Simple + preserveAspect (background/logo/portraits) -
        // spritePixelsPerUnit doesn't affect on-screen size for Simple images
        // (see HudArtImportConfigurator.ConfigureMovementStick's note on the
        // same point), so 100 is just this codebase's usual default, not a
        // measured value.
        private static void ConfigureFullSprite(string path)
        {
            ArtImportConfigurator.ConfigureSingleSprite(
                path,
                border: Vector4.zero,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 100f);
        }

        private static void ConfigureSlicedSprite(string path, Vector4 border, float targetWidth)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.GetSourceTextureWidthAndHeight(out int nativeWidth, out _);
            float pixelsPerUnit = 100f * nativeWidth / targetWidth;

            ArtImportConfigurator.ConfigureSingleSprite(path, border, FilterMode.Bilinear, mipmaps: false, pixelsPerUnit: pixelsPerUnit);
        }
    }
}
