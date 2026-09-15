using System;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Import settings for the Login/CharacterSelect/CharacterCreate art
    /// (Art/UI/Title/*.png). TitleBackground/TitleLogo/PortraitMage/
    /// PortraitWarrior are Image.Type.Simple (no border needed - see
    /// ConfigureFullSprite). CharacterSlotFrame/InputFieldFrame are
    /// 9-sliced; their border thickness is a property of the drawn artwork,
    /// not something the commissioning spec fixes, so it was measured with
    /// PIL once the art landed (2026-09-15, see the constants below).
    /// pixelsPerUnit for those two is computed automatically from each
    /// texture's real width (TextureImporter.GetSourceTextureWidthAndHeight,
    /// same technique Editor/SkillVfxImporter.cs already uses).
    /// </summary>
    internal static class CharacterFlowArtImportConfigurator
    {
        internal const string TitleArtDir = "Assets/Sapphire/Art/UI/Title";

        // 2026-09-15 (gemless MapleStory-M rebuild) - both files replaced with
        // build_ui_kit.py output, re-measured with PIL against the NEW art
        // (old values above this comment described the retired gold-gem
        // assets and no longer apply):
        // CharacterSlotFrame.png (780x1950, build_beige_panel(260,650)*SCALE3):
        // same double-line beige panel family as MenuPanelOdin/
        // MessagePanelFrameGold (ArtImportConfigurator.ConfigureUiFrames) -
        // inward-scan border settles at 32px on all 4 edges (decoration spans
        // px 17-32 from every edge, confirmed via direct pixel dump at top/
        // bottom/left/right, all symmetric).
        // InputFieldFrame.png (1080x180, single flat-fill rounded rect, not the
        // double-line family - build_input_field_frame draws it directly):
        // inward-scan border settles at 22px on all 4 edges (single outline,
        // no inner accent line to conflate with).
        private static readonly Vector4 CharacterSlotFrameBorder = new Vector4(32, 32, 32, 32);
        private static readonly Vector4 InputFieldFrameBorder = new Vector4(22, 22, 22, 22);

        // Target on-screen widths these two 9-sliced frames are actually
        // built at (see LoginUiBuilder/CharacterSelectUiBuilder/
        // CharacterCreateUiBuilder) - pixelsPerUnit is calibrated against
        // this per ArtImportConfigurator's established *100 convention
        // (Image.pixelsPerUnit = sprite.pixelsPerUnit / canvas.referencePixelsPerUnit).
        internal const float CharacterSlotFrameTargetWidth = 260f;
        internal const float InputFieldFrameTargetWidth = 360f;

        internal static void ConfigureAll()
        {
            ConfigureFullSprite(TitleArtDir + "/TitleBackground.png");
            ConfigureFullSprite(TitleArtDir + "/TitleLogo.png");
            ConfigureFullSprite(TitleArtDir + "/PortraitMage.png");
            ConfigureFullSprite(TitleArtDir + "/PortraitWarrior.png");

            ConfigureSlicedSprite(TitleArtDir + "/CharacterSlotFrame.png", CharacterSlotFrameBorder, CharacterSlotFrameTargetWidth);
            ConfigureSlicedSprite(TitleArtDir + "/InputFieldFrame.png", InputFieldFrameBorder, InputFieldFrameTargetWidth);
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
