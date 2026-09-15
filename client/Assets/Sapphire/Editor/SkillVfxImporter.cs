using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sapphire.Domain.Vfx;
using Sapphire.Presentation.Skills;

namespace Sapphire.EditorTools
{
    /// <summary>Imports the generated 5-row, 8-frame atlas and persists its runtime library.</summary>
    public sealed class SkillVfxImporter : AssetPostprocessor
    {
        public const string AtlasPath = "Assets/Sapphire/Art/VFX/MageSkillVfxAtlas.png";
        public const string ThunderAtlasPath = "Assets/Sapphire/Art/VFX/ThunderFieldPadded.png";
        public const string ShieldAtlasPath = "Assets/Sapphire/Art/VFX/ManaShieldPadded.png";
        public const string DirectionalAtlasPath = "Assets/Sapphire/Art/VFX/MageDirectionalPadded.png";
        private const string LibraryPath = "Assets/Sapphire/Resources/MageSkillVfxLibrary.asset";
        private static readonly string[] Rows = { "Shield", "Teleport", "Thunder", "IceSpike", "LightningSpear" };
        private void OnPreprocessTexture()
        {
            if (assetPath == ThunderAtlasPath)
            {
                ConfigureStripTexture((TextureImporter)assetImporter, assetPath, "Thunder");
                return;
            }
            if (assetPath == ShieldAtlasPath)
            {
                ConfigureStripTexture((TextureImporter)assetImporter, assetPath, "Shield");
                return;
            }
            if (assetPath == DirectionalAtlasPath)
            {
                ConfigureDirectionalTexture((TextureImporter)assetImporter);
                return;
            }
            if (assetPath != AtlasPath) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = width / 8f;
            byte[] alpha = ReadAlphaBytes(assetPath);
            var slices = new SpriteMetaData[40];
            for (int row = 0; row < 5; row++)
                for (int frame = 0; frame < 8; frame++)
                {
                    int left = Mathf.RoundToInt(frame * width / 8f);
                    int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                    int top = Mathf.RoundToInt(row * height / 5f);
                    int bottom = Mathf.RoundToInt((row + 1) * height / 5f);
                    // Every spell uses the actor as its origin. The spear row
                    // grows forward from its left edge; all other rows stay
                    // centered directly over the actor/grid origin. The pivot is
                    // anchored to THIS frame's own alpha content bbox (not the
                    // cell's fixed geometric center) - see
                    // VfxFramePivotCalculator's class doc for why (2026-09-15
                    // skill-cast jitter fix).
                    (float pivotX, float pivotY) = VfxFramePivotCalculator.ComputeContentPivot(
                        alpha, width, left, height - bottom, right - left, bottom - top, directional: row == 4);
                    slices[row * 8 + frame] = new SpriteMetaData
                    {
                        name = "MageVfx_" + Rows[row] + "_" + frame.ToString("00"),
                        rect = new Rect(left, height - bottom, right - left, bottom - top),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(pivotX, pivotY)
                    };
                }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        // Raw per-pixel alpha for the whole texture, bottom-to-top (matches
        // Texture2D.GetPixels32/the rect flip above) - read directly from the PNG
        // bytes via a throwaway Texture2D since OnPreprocessTexture runs before the
        // asset's own Texture2D is importable/readable.
        private static byte[] ReadAlphaBytes(string path)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            raw.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = raw.GetPixels32();
            var alpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a;
            UnityEngine.Object.DestroyImmediate(raw);
            return alpha;
        }

        private static void ConfigureStripTexture(TextureImporter importer, string path, string effectName)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = width / 8f;
            byte[] alpha = ReadAlphaBytes(path);
            var slices = new SpriteMetaData[8];
            for (int frame = 0; frame < 8; frame++)
            {
                int left = Mathf.RoundToInt(frame * width / 8f);
                int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                // Content-bbox pivot, same reasoning as the main atlas above -
                // ManaShieldPadded/ThunderFieldPadded are single-row strips of the
                // same AI-generated-per-frame kind.
                (float pivotX, float pivotY) = VfxFramePivotCalculator.ComputeContentPivot(
                    alpha, width, left, 0, right - left, height, directional: false);
                slices[frame] = new SpriteMetaData
                {
                    name = "MageVfx_" + effectName + "_" + frame.ToString("00"),
                    rect = new Rect(left, 0, right - left, height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(pivotX, pivotY)
                };
            }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void ConfigureDirectionalTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = width / 8f;
            byte[] alpha = ReadAlphaBytes(DirectionalAtlasPath);
            var slices = new SpriteMetaData[24];
            string[] names = { "Teleport", "IceSpike", "LightningSpear" };
            for (int row = 0; row < 3; row++)
                for (int frame = 0; frame < 8; frame++)
                {
                    int left = Mathf.RoundToInt(frame * width / 8f);
                    int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                    int top = Mathf.RoundToInt(row * height / 3f);
                    int bottom = Mathf.RoundToInt((row + 1) * height / 3f);
                    (float pivotX, float pivotY) = VfxFramePivotCalculator.ComputeContentPivot(alpha, width, left, height - bottom, right - left, bottom - top, directional: row == 2);
                    slices[row * 8 + frame] = new SpriteMetaData
                    {
                        name = "MageVfx_Directional_" + names[row] + "_" + frame.ToString("00"),
                        rect = new Rect(left, height - bottom, right - left, bottom - top),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(pivotX, pivotY)
                    };
                }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (imported.Contains(AtlasPath) || imported.Contains(ThunderAtlasPath) || imported.Contains(ShieldAtlasPath) || imported.Contains(DirectionalAtlasPath)) EditorApplication.delayCall += ConfigureLibrary;
        }
        [InitializeOnLoadMethod]
        private static void ScheduleLibrary() { EditorApplication.delayCall += ConfigureLibrary; }

        // Scene builder may call this explicitly after ConfigureArtImportSettings.
        public static void ConfigureLibrary()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath) == null) return;
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            if (sprites.Length != 40)
            {
                AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
                sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
                if (sprites.Length != 40) throw new InvalidOperationException("Mage VFX atlas must contain 40 slices.");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Sapphire/Resources")) AssetDatabase.CreateFolder("Assets/Sapphire", "Resources");
            var library = AssetDatabase.LoadAssetAtPath<SkillVfxLibrary>(LibraryPath);
            bool created = library == null;
            if (created) library = ScriptableObject.CreateInstance<SkillVfxLibrary>();
            Sprite[] frames = new Sprite[40];
            for (int row = 0; row < 5; row++)
                for (int frame = 0; frame < 8; frame++)
                    frames[row * 8 + frame] = sprites.Single(s => s.name == "MageVfx_" + Rows[row] + "_" + frame.ToString("00"));
            var thunderSprites = AssetDatabase.LoadAllAssetsAtPath(ThunderAtlasPath).OfType<Sprite>().ToArray();
            if (thunderSprites.Length == 8)
                for (int frame = 0; frame < 8; frame++)
                    frames[16 + frame] = thunderSprites.Single(s => s.name == "MageVfx_Thunder_" + frame.ToString("00"));
            var shieldSprites = AssetDatabase.LoadAllAssetsAtPath(ShieldAtlasPath).OfType<Sprite>().ToArray();
            if (shieldSprites.Length == 8)
                for (int frame = 0; frame < 8; frame++)
                    frames[frame] = shieldSprites.Single(s => s.name == "MageVfx_Shield_" + frame.ToString("00"));
            var directionalSprites = AssetDatabase.LoadAllAssetsAtPath(DirectionalAtlasPath).OfType<Sprite>().ToArray();
            if (directionalSprites.Length == 24)
            {
                for (int frame = 0; frame < 8; frame++)
                {
                    frames[8 + frame] = directionalSprites.Single(s => s.name == "MageVfx_Directional_Teleport_" + frame.ToString("00"));
                    frames[24 + frame] = directionalSprites.Single(s => s.name == "MageVfx_Directional_IceSpike_" + frame.ToString("00"));
                    frames[32 + frame] = directionalSprites.Single(s => s.name == "MageVfx_Directional_LightningSpear_" + frame.ToString("00"));
                }
            }
            if (!created && library.Frames != null && library.Frames.SequenceEqual(frames)) return;
            library.Frames = frames;
            if (created) AssetDatabase.CreateAsset(library, LibraryPath);
            else EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
    }
}
