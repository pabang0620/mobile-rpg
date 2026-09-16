using System;
using System.Collections.Generic;
using System.IO;
using Sapphire.Domain.Character;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Shared per-frame foot-pivot slicing for the mage/warrior topdown grid
    /// sheets (MageTopdownGridSheet.png, WarriorTopdownGridSheet.png) - both
    /// use the identical 3-column (idle/walkA/walkB) x 4-row
    /// (Down/Left/Right/Up), 362px-cell layout (see
    /// ArtImportConfigurator.ConfigureCharacterSheets /
    /// WarriorArtImportConfigurator.ConfigureWarriorCharacterSheet's own doc
    /// comments), so this is the one shared place that computes and applies
    /// per-frame pivots - replacing the two classes' former separate
    /// hardcoded pivotByRow dictionaries (one hand-picked pivot per
    /// direction, shared across that direction's 3 poses) with a value
    /// measured straight from each individual frame's own alpha content via
    /// <see cref="CharacterFootPivotCalculator"/>.
    ///
    /// 2026-09-16 (character-floating-above-tile bug, docs/HANDOFF.md): see
    /// CharacterFootPivotCalculator's class doc for why the previous
    /// hand-picked pivots were wrong specifically for the "Right" direction
    /// on both sheets, and why per-frame (not per-row-average) pivots also
    /// close the "walk cycle jitter" ask - each of idle/walkA/walkB now gets
    /// its own measured pivot instead of sharing one row-level average, so
    /// any future art regeneration that reintroduces a real per-pose foot
    /// offset renders correctly instead of visibly bobbing.
    /// </summary>
    internal static class CharacterGridSheetImporter
    {
        private static readonly string[] RowNames = { "Down", "Left", "Right", "Up" };
        private static readonly string[] ColNames = { "Idle", "WalkA", "WalkB" };

        internal static IEnumerable<(string name, Rect rect, Vector2 pivot)> BuildGridSlices(
            string assetPath, string namePrefix, int textureWidth, int textureHeight, int cellSize, string[] colNames = null)
        {
            // colNames defaults to the walk-sheet's Idle/WalkA/WalkB columns so
            // every pre-existing call site (Mage/Warrior *TopdownGridSheet.png)
            // is unaffected. The 2026-09-16 *AttackGridSheet.png sheets pass
            // { "Windup", "Apex", "Recovery" } explicitly - same 3-column x
            // 4-row / 362px-cell layout and the identical per-frame foot-pivot
            // measurement, just a different column meaning (see
            // ArtImportConfigurator.ConfigureMageAttackSheet /
            // WarriorArtImportConfigurator.ConfigureWarriorAttackSheet).
            colNames ??= ColNames;

            if (textureWidth != cellSize * colNames.Length || textureHeight != cellSize * RowNames.Length)
            {
                throw new Exception($"{namePrefix} grid size mismatch: expected {cellSize * colNames.Length}x{cellSize * RowNames.Length}, got {textureWidth}x{textureHeight}");
            }

            byte[] alpha = ReadAlphaBytes(assetPath);
            var result = new List<(string, Rect, Vector2)>();
            for (int r = 0; r < RowNames.Length; r++)
            {
                // Unity rects are bottom-up; row 0 (Down) is the topmost row in the image.
                int yBottom = textureHeight - (r + 1) * cellSize;

                for (int c = 0; c < colNames.Length; c++)
                {
                    int xLeft = c * cellSize;
                    (float pivotX, float pivotY) = CharacterFootPivotCalculator.ComputeFootPivot(
                        alpha, textureWidth, xLeft, yBottom, cellSize, cellSize);
                    string name = $"{namePrefix}_{RowNames[r]}_{colNames[c]}";
                    result.Add((name, new Rect(xLeft, yBottom, cellSize, cellSize), new Vector2(pivotX, pivotY)));
                }
            }

            return result;
        }

        // Same technique as SkillVfxImporter.ReadAlphaBytes: read raw PNG
        // bytes via a throwaway Texture2D rather than
        // AssetDatabase.LoadAssetAtPath, since this runs from
        // ArtImportConfigurator (called directly by SapphireSceneBuilder,
        // not from an AssetPostprocessor hook) where the asset's own
        // Texture2D is not guaranteed to be marked readable at this point.
        private static byte[] ReadAlphaBytes(string path)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            raw.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = raw.GetPixels32();
            var alphaBytes = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) alphaBytes[i] = pixels[i].a;
            UnityEngine.Object.DestroyImmediate(raw);
            return alphaBytes;
        }
    }
}
