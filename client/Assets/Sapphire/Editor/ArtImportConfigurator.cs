using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Configures texture importer settings (slicing, PPU, filtering, borders)
    /// for every source art asset the VillageHub scene depends on. Split out of
    /// <see cref="SapphireSceneBuilder"/> so scene-construction and art-import
    /// concerns don't live in the same 500+ line file. Safe to re-run: it only
    /// (re)writes importer settings, it does not create/delete assets.
    /// </summary>
    internal static class ArtImportConfigurator
    {
        // 2026-09-15 (gemless MapleStory-M-style UI kit): every asset built by
        // tools/ui_kit/build_ui_kit.py uses SCALE=3 (native saved px = target
        // on-screen px * 3, see that script's module doc comment), so
        // pixelsPerUnit = 100 * native/target = 300 for ALL of them regardless
        // of each asset's own native pixel size - a flat constant instead of
        // the old per-call-site "100*nativeWidth/targetWidth" formula every
        // pre-existing hand-measured asset in this file still uses (those
        // varied because their native/target ratios weren't a fixed factor).
        internal const float UiKitV3PixelsPerUnit = 300f;

        internal static void ConfigureArtImportSettings()
        {
            ConfigureGroundAtlas();
            ConfigureVillagePropsAtlas();
            ConfigureSlimeKingdomAtlas();
            ConfigureSlimeKingdomExpansionAtlases();
            ConfigureSlimeKingdomGroundTiles();
            ConfigureSlimeKingdomSeamlessTiles();
            ConfigureCharacterSheets();
            ConfigureMageAttackSheet();
            ConfigureUiFrames();
            ConfigureSkillButtonFrame();
            ConfigureHealthBarFrame();
            ConfigureSkillIconsSet();
            ConfigureSingleSprite(SapphireSceneBuilder.UiArtDir + "/HitSpark.png", Vector4.zero, FilterMode.Bilinear, false, 100);
            HudArtImportConfigurator.ConfigureAll();
            WarriorArtImportConfigurator.ConfigureAll();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureSlimeKingdomAtlas()
        {
            const float cell = 362f;
            string[] names =
            {
                "SlimeGround_Grass", "SlimeGround_Road", "SlimeGround_Pool", "SlimeGround_Stone",
                "SlimeProp_Tree", "SlimeProp_Crystal", "SlimeProp_Mushroom", "SlimeProp_Statue",
                "SlimeProp_Slime", "SlimeProp_Chest", "SlimeProp_Gate", "SlimeProp_Throne",
            };
            var slices = new (string, Rect, Vector2)[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                slices[i] = (names[i], new Rect((i % 4) * cell, (2 - i / 4) * cell, cell, cell), new Vector2(0.5f, 0.5f));
            }

            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/SlimeKingdomAtlas.png",
                cell,
                FilterMode.Bilinear,
                false,
                null,
                slices);
        }

        private static void ConfigureSlimeKingdomGroundTiles()
        {
            foreach (string name in new[] { "Grass", "RoyalRoad", "JellyPool", "CastleStone" })
            {
                string path = SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/Ground/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("Texture not found or not a TextureImporter: " + path);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 1250f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureSlimeKingdomExpansionAtlases()
        {
            string[] propNames =
            {
                "Slime2_House", "Slime2_Shop", "Slime2_Fountain", "Slime2_Lamp",
                "Slime2_BridgeH", "Slime2_BridgeV", "Slime2_Cliff", "Slime2_Hedge",
                "Slime2_Cave", "Slime2_Sign", "Slime2_Flowers", "Slime2_PalaceArch",
            };
            var propSlices = new (string, Rect, Vector2)[propNames.Length];
            for (int i = 0; i < propNames.Length; i++)
                propSlices[i] = (propNames[i], new Rect((i % 4) * 362f, (2 - i / 4) * 362f, 362f, 362f), new Vector2(0.5f, 0.5f));
            ConfigureMultiSprite(SapphireSceneBuilder.WorldArtDir + "/SlimeKingdomProps2.png", 362f, FilterMode.Bilinear, false, null, propSlices);

        }

        private static void ConfigureSlimeKingdomSeamlessTiles()
        {
            string root = SapphireSceneBuilder.WorldArtDir + "/SlimeKingdom/SeamlessV4/";
            foreach (string family in new[] { "Grass", "Dirt", "Water", "Stone", "Shore" })
            {
                int count = family == "Shore" ? 32 : 4;
                for (int i = 0; i < count; i++)
                    ConfigureGroundTileSprite(root + family + i + ".png", 512, 512f);
            }
        }

        // 2026-09-15: one shared PPU for all 6 individually-imported ground
        // tiles (see ConfigureGroundAtlas below for why this isn't 512).
        private const float GroundTilePpu = 508f;

        private static void ConfigureGroundAtlas()
        {
            // Ground tiles: 2026-09-15 split from one shared 1536x1024 3x2
            // atlas (GroundTiles.png, kept in git history) into 6 standalone
            // 512x512 textures under Art/World/Ground/. Root cause was two
            // compounding artifacts that both showed up as a 1px dark seam at
            // tile boundaries in the orchestrator's diagnostic screenshot
            // (final2_1280_default.png, e.g. x~=207/527/447/687/1167/1247):
            //  (a) atlas bleed - all 6 cells shared one texture with
            //      bilinear filtering + Max Size 256 downscale, so a sprite's
            //      edge texel sampled a neighboring cell's edge texel across
            //      the shared atlas seam;
            //  (b) sub-pixel gaps between adjacent tile quads letting the
            //      camera background color show through at the seam.
            // Splitting into 6 separate textures with wrapMode=Clamp kills
            // (a) outright - there is no neighboring cell in the same texture
            // to bleed from. (b) is closed by importing at PPU=508 instead of
            // 512: each 512px-wide tile then renders as a 512/508 ~= 1.008
            // unit quad, ~0.4% larger than the 1x1 grid cell it's placed in,
            // so adjacent tiles overlap by that same ~0.4% and paper over any
            // sub-pixel placement gap instead of leaving the background
            // visible through it.
            foreach (string tileName in new[] { "Grass_0", "Grass_1", "Grass_2", "Dirt_0", "Dirt_1", "Dirt_2" })
            {
                ConfigureGroundTileSprite(SapphireSceneBuilder.WorldArtDir + "/Ground/" + tileName + ".png");
            }
        }

        // Standalone (non-atlas) sprite import for one ground tile texture:
        // Sprite/Single, center pivot, Clamp wrap (no neighboring cell exists
        // in the texture to bleed from), bilinear filtering, mipmaps off (a
        // ground-plane tile is always viewed at ~1:1 scale, never minified
        // enough to need mip levels - and mips would reintroduce the same
        // edge-bleed artifact this split is meant to remove), Max Size 256,
        // uncompressed, FullRect mesh (a plain rectangular tile doesn't need
        // Tight's alpha-hull trim), PPU 508 (see ConfigureGroundAtlas above).
        private static void ConfigureGroundTileSprite(string path, int maxSize = 256, float pixelsPerUnit = GroundTilePpu)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;

            // spriteAlignment/spriteMeshType/spritePivot are not direct
            // TextureImporter properties (unlike spriteImportMode/
            // spritePixelsPerUnit/spriteBorder) - they live on
            // TextureImporterSettings and must be round-tripped via
            // Read/SetTextureSettings.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            importer.SaveAndReimport();
        }

        private static void ConfigureVillagePropsAtlas()
        {
            // Village props atlas: 1536x1024, 2x2 grid, each cell 768x512.
            // PPU = cell height so each prop is ~1 grid cell tall (props are wider
            // than 1 cell by design - fence rails span slightly more than one tile).
            var bottomCenterPivot = new Vector2(0.5f, 0f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.WorldArtDir + "/VillageProps.png",
                ppu: 512,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("VillageProps_FenceStraight", new Rect(0, 512, 768, 512), bottomCenterPivot),
                    ("VillageProps_FenceCornerA", new Rect(768, 512, 768, 512), bottomCenterPivot),
                    ("VillageProps_FenceCornerB", new Rect(0, 0, 768, 512), bottomCenterPivot),
                    ("VillageProps_Signpost", new Rect(768, 0, 768, 512), bottomCenterPivot),
                });
        }

        private static void ConfigureCharacterSheets()
        {
            // 2026-09-14: replaced the two separate idle (MageIdleDirectional.png)
            // and walk (MageWalk4x3-v2.png) sheets with a single unified sheet,
            // MageTopdownGridSheet.png (1086x1448, 3 columns x 4 rows, 362x362
            // cells). Columns are idle/walkA/walkB; rows are Down/Left/Right/Up
            // (see BuildMageGridSlices). PPU=302 is carried over unchanged from
            // the old walk sheet (same 362px cell size -> 362/302 ~= 1.2 world
            // units tall, within the previously-verified 1.0-1.5 unit target band).
            //
            // 2026-09-14 (later same day): swapped in the v4 body-stable
            // walk-cycle artwork (same 1086x1448 / 362px-cell layout, no grid
            // change needed).
            //
            // 2026-09-16 (character-floating-above-tile bug, docs/HANDOFF.md):
            // replaced the hand-picked per-row pivot dictionary with per-frame
            // pivots computed straight from each cell's own (cleaned) alpha
            // content via CharacterGridSheetImporter/CharacterFootPivotCalculator
            // - see those classes' doc comments for why the old dictionary's
            // "Right" row pivot was wrong (corrupted by a cross-cell bleed
            // artifact from the "Up" row) and why per-frame beats
            // per-row-average.
            string mageSheetPath = SapphireSceneBuilder.RootArtDir + "/MageTopdownGridSheet.png";
            ConfigureMultiSprite(
                mageSheetPath,
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: CharacterGridSheetImporter.BuildGridSlices(mageSheetPath, "Mage", 1086, 1448, cellSize: 362));
        }

        // 2026-09-16 (attack-motion slice): MageAttackGridSheet.png, same
        // 1086x1448 / 3-column x 4-row / 362px-cell layout as
        // MageTopdownGridSheet.png above, but the 3 columns are a
        // Windup/Apex/Recovery swing pose sequence instead of
        // Idle/WalkA/WalkB - see CharacterGridSheetImporter.BuildGridSlices'
        // colNames parameter and SkillMotionPlayer, which plays these three
        // frames back on a basic attack / skill cast. Same PPU (302) and
        // mipmaps setting as the walk sheet so the swing renders at the exact
        // same in-world scale, and reuses the identical per-frame
        // alpha-measured foot pivot (CharacterFootPivotCalculator) - foot
        // placement was independently re-measured (see the task spec's 0-9px
        // deviation note) and this calculator re-derives it per frame anyway,
        // so no separate pivot table is needed.
        private static void ConfigureMageAttackSheet()
        {
            string mageAttackSheetPath = SapphireSceneBuilder.RootArtDir + "/MageAttackGridSheet.png";
            ConfigureMultiSprite(
                mageAttackSheetPath,
                ppu: 302,
                filterMode: FilterMode.Bilinear,
                mipmaps: true,
                maxSize: null,
                slices: CharacterGridSheetImporter.BuildGridSlices(
                    mageAttackSheetPath, "Mage", 1086, 1448, cellSize: 362,
                    colNames: new[] { "Windup", "Apex", "Recovery" }));
        }

        private static void ConfigureUiFrames()
        {
            // UI: message panel frame, 1680x960 (2026-09-15 gemless MapleStory-M
            // rebuild - replaces the previous ornate 1937x812 gold-trim asset,
            // same role: message panel background + CharacterSelect's
            // ConfirmDialog). Beige parchment panel, generated by
            // tools/ui_kit/build_ui_kit.py's build_beige_panel (double-line
            // border, vertical gradient fill, no gems/gold). Border re-measured
            // with PIL (inward scan from each edge to the first row/column that
            // stably matches the flat interior fill color, not the naive
            // "distance to any near-match" scan - a plain nearest-color scan
            // false-positives on this asset's anti-aliasing spike right at the
            // outer/inner line boundary) - all four edges settle at 32px
            // (confirmed via direct pixel dump: decoration spans px 17-32 from
            // every edge, flat fill from 32 onward on all 4 sides, symmetric
            // top/bottom/left/right since build_beige_panel draws the same
            // radius/line geometry on every side regardless of canvas aspect).
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png",
                border: new Vector4(32, 32, 32, 32),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: UiKitV3PixelsPerUnit);

            // UI: wide pill button, 993x251 (2026-09-15, replaces the old flat
            // WideButton.png at every call site - message panel close button,
            // main menu open button, and the 7 menu list item buttons all now
            // share this one gold asset; see VillageHubUiBuilder). Reported as
            // "20px padding crop" but that claim was not trusted - re-measured
            // the actual gold-frame-to-navy-fill color transition directly (same
            // method as MessagePanelFrameGold above, narrow 40%-60% window):
            // left 114px, right 116px, top 77px, bottom 71px.
            //
            // pixelsPerUnit calibrated to nativeWidth/280 (~3.546), scaled by
            // referencePixelsPerUnit (100) - see the *100 note on
            // MessagePanelFrameGold above; without it this exact asset/border
            // combo is what produced the "메뉴 버튼 배경????보인?? bug (main
            // menu open button, MenuButtonGold border sums (114+116)/280 x
            // 100 no longer fits the ~28x-inflated math, collapsing the
            // Sliced mesh to 0 vertices). 280 is the close button's width, the
            // middle of this sprite's three call-site widths (150 main menu
            // button, 280 close button, 310 menu items). Border comfortably
            // under the smallest call site (MainMenuButton, 150x72): sums to
            // ~65/150 = 43% of width, ~42/72 = 58% of height, no overlap.
            ConfigureSingleSprite(
                SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png",
                border: new Vector4(30, 30, 30, 30),
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                pixelsPerUnit: 100f * 993f / 280f);

            // 2026-09-16 (Phase 3, REMEDIATION_PLAN.md D2(b)): MenuPanelFrameGold.png
            // (the old vertical text-list panel background, 7 divider lines for
            // the retired 7-item MainMenuPanel) is retired - see
            // HudArtImportConfigurator.ConfigureMenuPanelOdin for its
            // replacement, MenuPanelOdin.png. The old ConfigureSingleSprite call
            // for MenuPanelFrameGold.png that used to live here was removed
            // along with the source PNG (git rm, confirmed no remaining
            // references via grep).
        }

        private static void ConfigureSkillButtonFrame()
        {
            // UI: circular skill button frame, 756x396, 2 cells side by side -
            // left cell is the plain skill-slot ring, right cell is the larger
            // basic-attack ring (2026-09-15 gemless MapleStory-M rebuild,
            // replaces the previous 1536x1024 gold-gem asset - same 2-cell
            // layout, generated by build_ui_kit.py's build_skill_button_frame:
            // skill_d=80/attack_d=132/gutter=40 TARGET px * SCALE(3) native).
            // Re-measured each circle's own alpha bounding box (PIL) rather than
            // trusting the formula alone: skill circle x[0,241] y[77,319]
            // (241x242, ~1:1.00 aspect), basic-attack circle x[359,756] y[0,396]
            // (397x396, ~1:1.00 aspect) - both within 1-2px of the formula's
            // predicted center-derived box, keeping this file's "measure, don't
            // assume" convention.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png",
                ppu: UiKitV3PixelsPerUnit,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillButtonFrame_Skill", new Rect(0, 77, 241, 242), centerPivot),
                    ("SkillButtonFrame_BasicAttack", new Rect(359, 0, 397, 396), centerPivot),
                });
        }

        private static void ConfigureHealthBarFrame()
        {
            // UI: HP bar frame, 780x120, 2 cells stacked vertically - top cell
            // (native rows 0-48) is the translucent-dark capsule track, bottom
            // cell (native rows 72-120) is the crimson fill capsule (2026-09-15
            // gemless MapleStory-M rebuild - replaces the previous 1774x887
            // ornate-gold asset with a flat slim HUD bar, generated by
            // build_ui_kit.py's build_health_bar_frame: target_w=260/
            // target_h=16/gutter=8 * SCALE(3) native). Both cells fill their
            // entire 780x48 cell edge-to-edge (draw_dark_capsule's box exactly
            // matches the cell box, no internal margin) - confirmed via PIL
            // alpha bbox: Track bbox y=[0,49] of the 0-48 cell, Fill bbox
            // y=[71,120] of the 72-120 cell, both within 1px of the full cell
            // (the 1px slack is antialiasing, not a real margin). Sprite names
            // unchanged from the old asset (HealthBarFrame_Track/_Fill) so
            // VillageHubUiBuilder.BuildGauges needs no rename, only its
            // barHeight/fill-anchor constants (see that method - the new
            // capsule's 780:48=16.25 native aspect exactly matches its own
            // 260:16 target size, so barHeight=16 needs no artificial
            // recalibration the way the old ornate asset did).
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/HealthBarFrameGold.png",
                ppu: UiKitV3PixelsPerUnit,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("HealthBarFrame_Track", new Rect(0, 72, 780, 48), centerPivot),
                    ("HealthBarFrame_Fill", new Rect(0, 0, 780, 48), centerPivot),
                });
        }

        private static void ConfigureSkillIconsSet()
        {
            // Skill icons: 1536x1024, 3x2 grid, 6 cells (2026-09-15 gold-tier
            // replacement of the 2026-09-14 SkillIconsSet.png, same 3x2 layout
            // and reading order - visually re-confirmed against each icon's
            // artwork: staff+starburst/flying shard/snowflake top row,
            // chevron/shield/winged bolt bottom row, matching
            // 기본공격/비전???�리?�동 then ?�멸/보호�?질주). Sprite names unchanged
            // so SkillCatalog.cs and RadialSkillMenu need no changes.
            //
            // Re-measured rather than reused: column gaps at 520-527 and
            // 1003-1022 (px, top-left origin) - NOT clean zero-alpha bands like
            // the old asset, both have a 1-13px noise blip inside the gap from
            // thin gold connector linework between the hex icon frames, so the
            // boundary is the midpoint of the full noisy gap span (not just a
            // "first/last exact zero" average): 524 and 1013. Row split is also
            // not a clean zero-alpha gap - the minimum-density row in the
            // 400-620 window is row 502 with 83 nonzero-alpha pixels (not 0),
            // meaning the top/bottom icon rows' decorative elements touch
            // slightly; used that density-minimum row directly as the split -
            // same "measure, don't assume equal cells" principle as before,
            // applied to a noisier image.
            var centerPivot = new Vector2(0.5f, 0.5f);
            ConfigureMultiSprite(
                SapphireSceneBuilder.UiArtDir + "/SkillIconsSetGold.png",
                ppu: 100,
                filterMode: FilterMode.Bilinear,
                mipmaps: false,
                maxSize: null,
                slices: new[]
                {
                    ("SkillIcons_BasicAttack", new Rect(0, 522, 524, 502), centerPivot),
                    ("SkillIcons_ArcaneBolt", new Rect(524, 522, 489, 502), centerPivot),
                    ("SkillIcons_FrostWave", new Rect(1013, 522, 523, 502), centerPivot),
                    ("SkillIcons_Blink", new Rect(0, 0, 524, 522), centerPivot),
                    ("SkillIcons_Shield", new Rect(524, 0, 489, 522), centerPivot),
                    ("SkillIcons_Haste", new Rect(1013, 0, 523, 522), centerPivot),
                });
        }

        // internal (not private): HudArtImportConfigurator (split out of this
        // file for the same "keep files under ~500 lines" reason
        // VillageHubUiBuilder was split into VillageHubSkillMenuBuilder/
        // VillageHubMenuBuilder) reuses this instead of duplicating it.
        internal static void ConfigureMultiSprite(
            string path, float ppu, FilterMode filterMode, bool mipmaps, int? maxSize,
            IEnumerable<(string name, Rect rect, Vector2 pivot)> slices)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            if (maxSize.HasValue)
            {
                importer.maxTextureSize = maxSize.Value;
            }

            var metas = new List<SpriteMetaData>();
            foreach (var (name, rect, pivot) in slices)
            {
                metas.Add(new SpriteMetaData
                {
                    name = name,
                    rect = rect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                });
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
        }

        internal static void ConfigureSingleSprite(string path, Vector4 border, FilterMode filterMode, bool mipmaps, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new Exception("Texture not found or not a TextureImporter: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            importer.SaveAndReimport();
        }

        // internal (not private): moved here from HudArtImportConfigurator
        // (2026-09-16) so CharacterFlowArtImportConfigurator's new
        // ButtonSelectV2/ButtonDeleteV2/ButtonCreateV2 sheets can share it
        // instead of a second private copy - same "single shared helper,
        // multiple Configure* call sites" convention ConfigureMultiSprite/
        // ConfigureSingleSprite themselves already follow. ConfigureMultiSprite's
        // SpriteMetaData tuples don't carry a per-slice border (most
        // Multiple-mode sheets in this codebase are used as Image.Type.Simple,
        // not Sliced) - sheets that DO need 9-slice border data apply it as
        // this small follow-up pass instead of widening that shared helper's
        // signature for a minority of callers.
        internal static void ApplySingleSliceBorder(string path, string spriteName, Vector4 border)
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
    }
}
