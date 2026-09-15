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

        // 2026-09-15 실측 확정 (PIL, 골드프레임<->내부 navy 색상 전이 지점을
        // 40%-70% 구간에서 여러 행/열 샘플링해 mode 산출, ArtImportConfigurator
        // 관례와 동일 방법). CharacterSlotFrame.png(793x1983): fill 색상
        // RGB(8,32,75) 기준 left=36 right=36 top=32 bottom=31, 9개 샘플 전부
        // 편차 1px 이내로 매우 안정적. InputFieldFrame.png(2170x725): fill
        // RGB(12,33,67) 기준 left=36 right=35 top=54(21개 샘플 전부 동일) bottom=60
        // (일부 코너 장식 오염 샘플 제외 후 mode). 좌/하/우/상 순서는 이
        // 코드베이스의 Vector4(left,bottom,right,top) 관례(ArtImportConfigurator.
        // ConfigureUiFrames의 MessagePanelFrameGold 예시 참고)를 따른다.
        private static readonly Vector4 CharacterSlotFrameBorder = new Vector4(36, 31, 36, 32);
        private static readonly Vector4 InputFieldFrameBorder = new Vector4(36, 60, 35, 54);

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
