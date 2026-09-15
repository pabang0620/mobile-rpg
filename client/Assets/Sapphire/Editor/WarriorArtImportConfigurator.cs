using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Import settings for the warrior class's two art files
    /// (WarriorTopdownGridSheet.png, WarriorSkillIconsSetGold.png). Split out
    /// of <see cref="ArtImportConfigurator"/> (same "keep files under ~500
    /// lines" convention that file's own doc comment already follows) rather
    /// than added to Mage's ConfigureCharacterSheets/ConfigureSkillIconsSet -
    /// keeps every Mage-specific method there byte-for-byte untouched.
    ///
    /// 2026-09-15: both warrior art files now exist and have been measured
    /// with PIL. The body sheet's grid is a real equal grid by construction
    /// (the art spec explicitly commissions it as "동일한 격자/셀 크기/행열
    /// 순서" as Mage's sheet), so the 362px cell grid itself is unchanged -
    /// only the per-row pivots were re-measured against the actual art (see
    /// ConfigureWarriorCharacterSheet). The icon sheet's per-cell slices were
    /// re-measured to each icon's own alpha content bbox, the same way
    /// ArtImportConfigurator.ConfigureSkillIconsSet did for Mage's icons (see
    /// ConfigureWarriorSkillIconsSet) - no longer a naive equal-cell split.
    /// </summary>
    internal static class WarriorArtImportConfigurator
    {
        internal static void ConfigureAll()
        {
            ConfigureWarriorCharacterSheet();
            ConfigureWarriorSkillIconsSet();
        }

        // Same 1086x1448, 3-column (idle/walkA/walkB) x 4-row
        // (Down/Left/Right/Up) grid, cellSize=362, as Mage's
        // MageTopdownGridSheet.png (ArtImportConfigurator.ConfigureCharacterSheets)
        // - the task spec explicitly commissions the warrior sheet at
        // "동일한 격자/셀 크기/행열 순서". Reuses the exact same PPU (302) so
        // the warrior renders at the same in-world scale as the mage (both
        // read as "about the same size" character, no per-class size tuning
        // requested).
        //
        // Per-row pivots (feet position) - 2026-09-15 re-measured with PIL
        // against the actual WarriorTopdownGridSheet.png art (same method as
        // Mage's BuildMageGridSlices: average alpha-bounding-box center-x/
        // bottom-y across idle/walkA/walkB, one pivot per direction). Unlike
        // Mage's art, warrior Left/Right are NOT forced to share one pivot -
        // the source art itself is asymmetric (Left content bbox h=351-354px
        // reaching the cell's top edge; Right content bbox h=328px with a
        // 33px top margin), so using each row's own measured foot-y keeps
        // both rows' feet planted on the ground tile instead of forcing an
        // artificial match. Measured per-cell bbox (cellSize=362):
        // Down (Idle/WalkA/WalkB) foot_norm=0.0028/0.0028/0.0028, cx_norm=
        // 0.554/0.553/0.570 -> avg (0.559, 0.003); Left foot_norm=0.022/0.030/
        // 0.022, cx_norm=0.547/0.559/0.547 -> avg (0.551, 0.025); Right
        // foot_norm=0.0028 (all 3), cx_norm=0.483/0.471/0.471 -> avg (0.475,
        // 0.003); Up foot_norm=0.204/0.188/0.185, cx_norm=0.500/0.506/0.511 ->
        // avg (0.506, 0.192). Rounded to 2 decimals below, same precision
        // Mage's own pivots use.
        private static void ConfigureWarriorCharacterSheet()
        {
            const int textureWidth = 1086;
            const int textureHeight = 1448;
            const int cellSize = 362;

            ArtImportConfigurator.ConfigureMultiSprite(
                SapphireSceneBuilder.RootArtDir + "/WarriorTopdownGridSheet.png",
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: BuildTopdownGridSlices("Warrior", textureWidth, textureHeight, cellSize));
        }

        // Shared by BuildWarriorCharacterSheet; the equivalent Mage-only
        // logic in ArtImportConfigurator.ConfigureCharacterSheets keeps its
        // own private copy (unchanged, to guarantee zero behavior change for
        // Mage) rather than being generalized to call this - see this
        // method's own doc for why the two independently reaching the same
        // pivot values is intentional, not risky duplication.
        internal static IEnumerable<(string name, Rect rect, Vector2 pivot)> BuildTopdownGridSlices(
            string classNamePrefix, int textureWidth, int textureHeight, int cellSize)
        {
            string[] rowNames = { "Down", "Left", "Right", "Up" };
            string[] colNames = { "Idle", "WalkA", "WalkB" };

            var pivotByRow = new Dictionary<string, Vector2>
            {
                ["Down"] = new Vector2(0.56f, 0.00f),
                ["Left"] = new Vector2(0.55f, 0.02f),
                ["Right"] = new Vector2(0.48f, 0.00f),
                ["Up"] = new Vector2(0.51f, 0.19f),
            };

            if (textureWidth != cellSize * colNames.Length || textureHeight != cellSize * rowNames.Length)
            {
                throw new Exception($"{classNamePrefix}TopdownGridSheet grid size mismatch: expected {cellSize * colNames.Length}x{cellSize * rowNames.Length}, got {textureWidth}x{textureHeight}");
            }

            var result = new List<(string, Rect, Vector2)>();
            for (int r = 0; r < rowNames.Length; r++)
            {
                Vector2 pivot = pivotByRow[rowNames[r]];
                float yBottom = textureHeight - (r + 1) * cellSize;

                for (int c = 0; c < colNames.Length; c++)
                {
                    float xLeft = c * cellSize;
                    string name = $"{classNamePrefix}_{rowNames[r]}_{colNames[c]}";
                    result.Add((name, new Rect(xLeft, yBottom, cellSize, cellSize), pivot));
                }
            }

            return result;
        }

        // 1536x1024, 3x2 equal-grid CELLS (512x512, matches the commissioned
        // layout) but each slice's Rect is now the cell's own alpha content
        // bounding box (PIL, threshold alpha>8), not the naive full cell -
        // 2026-09-15 re-measurement, same "measure, don't assume" convention
        // ArtImportConfigurator.ConfigureSkillIconsSet uses for Mage's icons.
        // Reading order (left-to-right, top-to-bottom, matching the task's
        // stated order): row1 = 대검베기(기본공격, BasicAttack)/돌진(Dash)/
        // 회오리베기(Whirlwind), row2 = 방패막기(ShieldBlock)/전쟁함성(WarCry)/
        // 대지강타(GroundSlam). Per-cell bbox (cell-local, top-left origin):
        // BasicAttack x[43,504] y[44,511]; Dash x[537,998](cell-local
        // x[25,486]) y[93,456]; Whirlwind x[14,474] y[34,511]; ShieldBlock
        // x[44,487] y[0,452]; WarCry x[22,497] y[13,442]; GroundSlam x[9,485]
        // y[0,459] - all comfortably inside their own 512x512 cell, no
        // cross-cell bleed.
        private static void ConfigureWarriorSkillIconsSet()
        {
            var centerPivot = new Vector2(0.5f, 0.5f);

            ArtImportConfigurator.ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/WarriorSkillIconsSetGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_BasicAttack", new Rect(43f, 513f, 461f, 467f), centerPivot),
                    ("SkillIcons_Dash", new Rect(537f, 568f, 461f, 363f), centerPivot),
                    ("SkillIcons_Whirlwind", new Rect(1038f, 513f, 460f, 477f), centerPivot),
                    ("SkillIcons_ShieldBlock", new Rect(44f, 60f, 443f, 452f), centerPivot),
                    ("SkillIcons_WarCry", new Rect(534f, 70f, 475f, 429f), centerPivot),
                    ("SkillIcons_GroundSlam", new Rect(1033f, 53f, 476f, 459f), centerPivot),
                });
        }
    }
}
