using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sapphire.Presentation.Skills;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Imports the warrior's 8-frame VFX atlas (WarriorSkillVfxAtlas.png,
    /// 1586x992, 8 cols x 5 rows) and ground-slam strip
    /// (WarriorGroundSlamPadded.png, 2079x756, 8 cols x 1 row) and persists
    /// WarriorSkillVfxLibrary - mirrors SkillVfxImporter's mage convention
    /// (per-frame SpriteMetaData slicing + a ScriptableObject asset built
    /// from the resulting named sprites), but the atlas's own row order
    /// (BasicAttackSlash/WhirlwindSlash/WarCryShockwave/ShieldBlockBarrier/
    /// DashStreak top-to-bottom) does not match WarriorSkillVfxLibrary's
    /// Frames row order, so sprites are looked up by NAME when building the
    /// library, never by raw row index (see AssignRow below).
    /// </summary>
    public sealed class WarriorSkillVfxImporter : AssetPostprocessor
    {
        public const string AtlasPath = "Assets/Sapphire/Art/VFX/Warrior/WarriorSkillVfxAtlas.png";
        public const string GroundSlamStripPath = "Assets/Sapphire/Art/VFX/Warrior/WarriorGroundSlamPadded.png";
        private const string LibraryPath = "Assets/Sapphire/Resources/WarriorSkillVfxLibrary.asset";

        // Atlas row order, top-to-bottom, per the commissioned art spec.
        private static readonly string[] AtlasRowNames =
        {
            "BasicAttackSlash", "WhirlwindSlash", "WarCryShockwave", "ShieldBlockBarrier", "DashStreak",
        };

        // BasicAttackSlash/DashStreak are directional (drawn facing right)
        // and grow from their left edge, same convention as mage's
        // LightningSpear row (SkillVfxImporter) - the other three are
        // direction-less area effects centered on the caster.
        private static readonly Vector2 LeftEdgePivot = new Vector2(0f, .5f);
        private static readonly Vector2 CenterPivot = new Vector2(.5f, .5f);

        private void OnPreprocessTexture()
        {
            if (assetPath == GroundSlamStripPath)
            {
                ConfigureGroundSlamStrip((TextureImporter)assetImporter);
                return;
            }

            if (assetPath != AtlasPath)
            {
                return;
            }

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
            {
                bool directional = AtlasRowNames[row] == "BasicAttackSlash" || AtlasRowNames[row] == "DashStreak";
                Vector2 pivot = directional ? LeftEdgePivot : CenterPivot;
                for (int frame = 0; frame < 8; frame++)
                {
                    int left = Mathf.RoundToInt(frame * width / 8f);
                    int right = Mathf.RoundToInt((frame + 1) * width / 8f);
                    int top = Mathf.RoundToInt(row * height / 5f);
                    int bottom = Mathf.RoundToInt((row + 1) * height / 5f);
                    slices[row * 8 + frame] = new SpriteMetaData
                    {
                        name = "WarriorVfx_" + AtlasRowNames[row] + "_" + frame.ToString("00"),
                        rect = new Rect(left, height - bottom, right - left, bottom - top),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = pivot,
                    };
                }
            }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        // GroundSlam is a single row, 8 frames, center-pivoted - the actual
        // world footprint (3 tiles wide x 1 tile deep, rotated to face the
        // caster's facing direction) is entirely a WarriorSkillVfxPlayer
        // runtime concern (scale/rotation), not an import-time slice choice.
        private static void ConfigureGroundSlamStrip(TextureImporter importer)
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
                    name = "WarriorVfx_GroundSlam_" + frame.ToString("00"),
                    rect = new Rect(left, 0, right - left, height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = CenterPivot,
                };
            }
#pragma warning disable 618
            importer.spritesheet = slices;
#pragma warning restore 618
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (imported.Contains(AtlasPath) || imported.Contains(GroundSlamStripPath))
            {
                EditorApplication.delayCall += ConfigureLibrary;
            }
        }

        [InitializeOnLoadMethod]
        private static void ScheduleLibrary()
        {
            EditorApplication.delayCall += ConfigureLibrary;
        }

        // Scene builder may call this explicitly after ConfigureArtImportSettings
        // (see SapphireSceneBuilder.BuildAll), same convention SkillVfxImporter
        // uses for the mage library.
        public static void ConfigureLibrary()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath) == null)
            {
                return;
            }

            var atlasSprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            if (atlasSprites.Length != 40)
            {
                AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
                atlasSprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
                if (atlasSprites.Length != 40)
                {
                    throw new InvalidOperationException("Warrior VFX atlas must contain 40 slices.");
                }
            }

            var stripSprites = AssetDatabase.LoadAllAssetsAtPath(GroundSlamStripPath).OfType<Sprite>().ToArray();
            if (stripSprites.Length != 8)
            {
                AssetDatabase.ImportAsset(GroundSlamStripPath, ImportAssetOptions.ForceUpdate);
                stripSprites = AssetDatabase.LoadAllAssetsAtPath(GroundSlamStripPath).OfType<Sprite>().ToArray();
                if (stripSprites.Length != 8)
                {
                    throw new InvalidOperationException("Warrior ground slam strip must contain 8 slices.");
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Sapphire/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/Sapphire", "Resources");
            }

            var library = AssetDatabase.LoadAssetAtPath<WarriorSkillVfxLibrary>(LibraryPath);
            bool created = library == null;
            if (created)
            {
                library = ScriptableObject.CreateInstance<WarriorSkillVfxLibrary>();
            }

            var frames = new Sprite[48];
            AssignRow(frames, WarriorSkillVfxLibrary.BasicAttackRow, atlasSprites, "BasicAttackSlash");
            AssignRow(frames, WarriorSkillVfxLibrary.DashRow, atlasSprites, "DashStreak");
            AssignRow(frames, WarriorSkillVfxLibrary.WhirlwindRow, atlasSprites, "WhirlwindSlash");
            AssignRow(frames, WarriorSkillVfxLibrary.ShieldBlockRow, atlasSprites, "ShieldBlockBarrier");
            AssignRow(frames, WarriorSkillVfxLibrary.WarCryRow, atlasSprites, "WarCryShockwave");
            for (int frame = 0; frame < 8; frame++)
            {
                frames[WarriorSkillVfxLibrary.GroundSlamRow * 8 + frame] =
                    stripSprites.Single(s => s.name == "WarriorVfx_GroundSlam_" + frame.ToString("00"));
            }

            if (!created && library.Frames != null && library.Frames.SequenceEqual(frames))
            {
                return;
            }

            library.Frames = frames;
            if (created)
            {
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            else
            {
                EditorUtility.SetDirty(library);
            }

            AssetDatabase.SaveAssets();
        }

        private static void AssignRow(Sprite[] frames, int rowIndex, Sprite[] atlasSprites, string atlasRowName)
        {
            for (int frame = 0; frame < 8; frame++)
            {
                frames[rowIndex * 8 + frame] = atlasSprites.Single(s => s.name == "WarriorVfx_" + atlasRowName + "_" + frame.ToString("00"));
            }
        }
    }
}
