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
            ConfigureWarriorAttackSheet();
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
        // 2026-09-16 (character-floating-above-tile bug, docs/HANDOFF.md):
        // replaced the hand-picked per-row pivot dictionary (and the
        // Mage-specific duplicate grid-slicing logic this method used to
        // keep separate "to guarantee zero behavior change for Mage") with
        // the shared CharacterGridSheetImporter.BuildGridSlices, now that
        // both classes need the exact same per-frame-measured-pivot
        // algorithm - see CharacterFootPivotCalculator's doc comment for why
        // the old "Right" row pivot (0.48, 0.00) was wrong (corrupted by a
        // cross-cell bleed artifact from the "Up" row's own art).
        private static void ConfigureWarriorCharacterSheet()
        {
            const int textureWidth = 1086;
            const int textureHeight = 1448;
            const int cellSize = 362;
            string path = SapphireSceneBuilder.RootArtDir + "/WarriorTopdownGridSheet.png";

            ArtImportConfigurator.ConfigureMultiSprite(
                path,
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: CharacterGridSheetImporter.BuildGridSlices(path, "Warrior", textureWidth, textureHeight, cellSize));
        }

        // 2026-09-16 (attack-motion slice): WarriorAttackGridSheet.png, same
        // 1086x1448 / 3-column x 4-row / 362px-cell layout as
        // WarriorTopdownGridSheet.png above, but the columns are a
        // Windup/Apex/Recovery sword-swing pose sequence instead of
        // Idle/WalkA/WalkB - see ArtImportConfigurator.ConfigureMageAttackSheet's
        // doc comment (mirrors it exactly, mage vs warrior) and
        // SkillMotionPlayer, which plays these frames back on cast. mipmaps
        // false, matching this class's own ConfigureWarriorCharacterSheet
        // (not Mage's mipmaps=true) so the warrior swing renders at the exact
        // same fidelity as the warrior's own walk cycle.
        private static void ConfigureWarriorAttackSheet()
        {
            const int textureWidth = 1086;
            const int textureHeight = 1448;
            const int cellSize = 362;
            string path = SapphireSceneBuilder.RootArtDir + "/WarriorAttackGridSheet.png";

            ArtImportConfigurator.ConfigureMultiSprite(
                path,
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: CharacterGridSheetImporter.BuildGridSlices(
                    path, "Warrior", textureWidth, textureHeight, cellSize,
                    colNames: new[] { "Windup", "Apex", "Recovery" }));
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
