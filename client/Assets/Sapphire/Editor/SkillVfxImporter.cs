using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sapphire.Presentation.Skills;

namespace Sapphire.EditorTools
{
    /// <summary>Imports the generated 5-row, 8-frame atlas and persists its runtime library.</summary>
    public sealed class SkillVfxImporter : AssetPostprocessor
    {
        public const string AtlasPath = "Assets/Sapphire/Art/VFX/MageSkillVfxAtlas.png";
        public const string ThunderAtlasPath = "Assets/Sapphire/Art/VFX/ThunderFieldPadded.png";
        public const string ShieldAtlasPath = "Assets/Sapphire/Art/VFX/ManaShieldPadded.png";
        private const string LibraryPath = "Assets/Sapphire/Resources/MageSkillVfxLibrary.asset";
        private static readonly string[] Rows = { "Shield", "Teleport", "Thunder", "IceSpike", "LightningSpear" };
        private void OnPreprocessTexture()
        {
            if (assetPath == ThunderAtlasPath)
            {
                ConfigureStripTexture((TextureImporter)assetImporter, "Thunder");
                return;
            }
            if (assetPath == ShieldAtlasPath)
            {
                ConfigureStripTexture((TextureImporter)assetImporter, "Shield");
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
            var slices = new SpriteMetaData[40];
            for (int row = 0; row < 5; row++)
                for (int frame = 0; frame < 8; frame++)
                {
                    int left = Mathf.RoundToInt(frame * width / 8f);
                    int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                    int top = Mathf.RoundToInt(row * height / 5f);
                    int bottom = Mathf.RoundToInt((row + 1) * height / 5f);
                    slices[row * 8 + frame] = new SpriteMetaData
                    {
                        name = "MageVfx_" + Rows[row] + "_" + frame.ToString("00"),
                        rect = new Rect(left, height - bottom, right - left, bottom - top),
                        // Every spell uses the actor as its origin. The spear row
                        // grows forward from its left edge; all other rows stay
                        // centered directly over the actor/grid origin.
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = row == 4 ? new Vector2(0f, .5f) : new Vector2(.5f, .5f)
                    };
                }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void ConfigureStripTexture(TextureImporter importer, string effectName)
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
            var slices = new SpriteMetaData[8];
            for (int frame = 0; frame < 8; frame++)
            {
                int left = Mathf.RoundToInt(frame * width / 8f);
                int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                slices[frame] = new SpriteMetaData
                {
                    name = "MageVfx_" + effectName + "_" + frame.ToString("00"),
                    rect = new Rect(left, 0, right - left, height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f)
                };
            }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (imported.Contains(AtlasPath) || imported.Contains(ThunderAtlasPath) || imported.Contains(ShieldAtlasPath)) EditorApplication.delayCall += ConfigureLibrary;
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
            if (!created && library.Frames != null && library.Frames.SequenceEqual(frames)) return;
            library.Frames = frames;
            if (created) AssetDatabase.CreateAsset(library, LibraryPath);
            else EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
    }
}
